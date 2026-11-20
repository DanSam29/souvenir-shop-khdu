using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Інтеграційні тести обробки Stripe webhook: перевіряється коректний підпис, зміна стану замовлення
/// та ідемпотентність (повторна доставка тієї самої події не обробляється вдруге).
/// </summary>
[Collection("integration")]
public sealed class StripeWebhookReplayTests
{
    private readonly IntegrationTestFixture _fixture;

    public StripeWebhookReplayTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DuplicateWebhookEvent_IsProcessedOnlyOnce()
    {
        var (customer, orderId, totalAmount) = await CreatePaidReadyOrderAsync();

        var eventId = $"evt_replay_{Guid.NewGuid():N}";
        var payload = ApiTestHelpers.BuildCheckoutSessionCompletedPayload(orderId, totalAmount, eventId);

        // Перша доставка події - замовлення переходить у стан Paid
        var first = await ApiTestHelpers.PostStripeWebhookAsync(_fixture.Factory, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Повторна доставка тієї самої події (replay) - відхиляється як вже оброблена
        var second = await ApiTestHelpers.PostStripeWebhookAsync(_fixture.Factory, payload);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        // У БД зафіксовано рівно один запис про цю подію (ідемпотентність)
        await using (var db = _fixture.Factory.CreateDbContext())
        {
            var processed = await db.ProcessedStripeEvents.CountAsync(e => e.EventId == eventId);
            processed.Should().Be(1);
        }

        var order = await ApiTestHelpers.ReadDataAsync(await customer.GetAsync($"api/Orders/{orderId}"));
        order.GetProperty("status").GetString().Should().Be("Paid");
        order.GetProperty("payment").GetProperty("status").GetString().Should().Be("Completed");
    }

    [Fact]
    public async Task Webhook_WithoutSignature_IsAcceptedButIgnored()
    {
        using var client = _fixture.Factory.CreateApiClient();
        var payload = ApiTestHelpers.BuildCheckoutSessionCompletedPayload(1, 100m);

        var response = await client.PostAsync("api/Webhooks/stripe",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        // Контролер завжди повертає 200, щоб Stripe не робив повторних спроб
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Реєстрація → логін → кошик → оформлення (Card) із локальною заглушкою Stripe-сесії
    private async Task<(HttpClient Customer, int OrderId, decimal TotalAmount)> CreatePaidReadyOrderAsync()
    {
        var email = $"replay_{Guid.NewGuid():N}@example.com";
        using (var anon = _fixture.Factory.CreateApiClient())
        {
            var register = await anon.PostAsync("api/Users/register", ApiTestHelpers.Json(new
            {
                firstName = "Реплей",
                lastName = "Тестовий",
                email,
                password = ApiTestFactory.TestPassword,
                confirmPassword = ApiTestFactory.TestPassword
            }));
            register.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var token = await ApiTestHelpers.LoginAsync(_fixture.Factory, email);
        var customer = _fixture.Factory.CreateApiClient(token);

        var addToCart = await customer.PostAsync("api/Cart/items",
            ApiTestHelpers.Json(new { productId = 2, quantity = 1 }));
        addToCart.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkout = await customer.PostAsync("api/Orders", ApiTestHelpers.Json(new
        {
            recipientName = "Реплей Тестовий",
            recipientPhone = "+380501112244",
            city = "Київ",
            warehouseNumber = "1",
            cityRef = ApiTestFactory.CityRef,
            warehouseRef = ApiTestFactory.WarehouseRef,
            paymentMethod = "Card"
        }));
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ApiTestHelpers.ReadDataAsync(checkout);
        return (customer, data.GetProperty("orderId").GetInt32(), data.GetProperty("totalAmount").GetDecimal());
    }
}
