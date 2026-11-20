using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Наскрізний (E2E) сценарій життєвого циклу замовлення проти реального host + тимчасової MySQL:
/// реєстрація → логін → кошик → оформлення (Stripe mock) → webhook оплати → Shipped → Delivered.
/// </summary>
[Collection("integration")]
public sealed class E2EOrderFlowTests
{
    private readonly IntegrationTestFixture _fixture;

    public E2EOrderFlowTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullOrderLifecycle_FromRegistrationToDelivered()
    {
        // 1. Реєстрація нового покупця (домен поза університетом, щоб не викликати студентську верифікацію)
        var email = $"e2e_{Guid.NewGuid():N}@example.com";
        using var client = _fixture.Factory.CreateApiClient();
        var register = await client.PostAsync("api/Users/register", ApiTestHelpers.Json(new
        {
            firstName = "Тест",
            lastName = "Покупець",
            email,
            password = ApiTestFactory.TestPassword,
            confirmPassword = ApiTestFactory.TestPassword
        }));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Логін та отримання access-токена
        var loginToken = await ApiTestHelpers.LoginAsync(_fixture.Factory, email);
        loginToken.Should().NotBeNullOrEmpty();
        using var customer = _fixture.Factory.CreateApiClient(loginToken);

        // 3. Додавання товару до кошика (ProductId 1 існує завдяки HasData-міграції)
        var addToCart = await customer.PostAsync("api/Cart/items",
            ApiTestHelpers.Json(new { productId = 1, quantity = 2 }));
        addToCart.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Оформлення замовлення з оплатою карткою (Stripe-сесія підмінюється локальною заглушкою)
        var checkout = await customer.PostAsync("api/Orders", ApiTestHelpers.Json(new
        {
            recipientName = "Тест Покупець",
            recipientPhone = "+380501112233",
            city = "Київ",
            warehouseNumber = "1",
            warehouseAddress = "Відділення №1: вул. Хрещатик, 1",
            cityRef = ApiTestFactory.CityRef,
            warehouseRef = ApiTestFactory.WarehouseRef,
            paymentMethod = "Card"
        }));
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkoutData = await ApiTestHelpers.ReadDataAsync(checkout);
        var orderId = checkoutData.GetProperty("orderId").GetInt32();
        var totalAmount = checkoutData.GetProperty("totalAmount").GetDecimal();
        checkoutData.GetProperty("status").GetString().Should().Be("Processing");
        checkoutData.GetProperty("paymentUrl").GetString().Should().NotBeNullOrEmpty();
        totalAmount.Should().BeGreaterThan(0);

        // 5. Надсилання підписаної події Stripe про успішну оплату
        var webhookPayload = ApiTestHelpers.BuildCheckoutSessionCompletedPayload(orderId, totalAmount);
        var webhook = await ApiTestHelpers.PostStripeWebhookAsync(_fixture.Factory, webhookPayload);
        webhook.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Після webhook замовлення оплачене
        var afterPayment = await ApiTestHelpers.ReadDataAsync(await customer.GetAsync($"api/Orders/{orderId}"));
        afterPayment.GetProperty("status").GetString().Should().Be("Paid");
        afterPayment.GetProperty("payment").GetProperty("status").GetString().Should().Be("Completed");

        // 7. Адміністратор відправляє замовлення (Paid → Shipped, потрібен ТТН)
        var adminToken = await ApiTestHelpers.LoginAsync(_fixture.Factory, ApiTestFactory.AdminEmail);
        using var admin = _fixture.Factory.CreateApiClient(adminToken);

        var shipped = await admin.PutAsync($"api/admin/orders/{orderId}/status",
            ApiTestHelpers.Json(new { status = "Shipped", trackingNumber = "20450000000001" }));
        shipped.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Доставлено (Shipped → Delivered)
        var delivered = await admin.PutAsync($"api/admin/orders/{orderId}/status",
            ApiTestHelpers.Json(new { status = "Delivered" }));
        delivered.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalOrder = await ApiTestHelpers.ReadDataAsync(await customer.GetAsync($"api/Orders/{orderId}"));
        finalOrder.GetProperty("status").GetString().Should().Be("Delivered");
        finalOrder.GetProperty("shipping").GetProperty("trackingNumber").GetString().Should().Be("20450000000001");
    }

    [Fact]
    public async Task Checkout_WithoutAuthorization_IsRejected()
    {
        using var anonymous = _fixture.Factory.CreateApiClient();
        var response = await anonymous.PostAsync("api/Orders", ApiTestHelpers.Json(new
        {
            recipientName = "Аноним",
            recipientPhone = "+380501112233",
            city = "Київ",
            warehouseNumber = "1",
            cityRef = ApiTestFactory.CityRef,
            warehouseRef = ApiTestFactory.WarehouseRef,
            paymentMethod = "Card"
        }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
