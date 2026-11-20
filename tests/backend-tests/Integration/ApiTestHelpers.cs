using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Stripe;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>Допоміжні методи для інтеграційних тестів: JSON, розбір відповідей, логін та підпис Stripe webhook.</summary>
internal static class ApiTestHelpers
{
    public static StringContent Json(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    /// <summary>Витягує поле <c>data</c> зі стандартної відповіді <c>ApiResponse&lt;T&gt;</c>.</summary>
    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").Clone();
    }

    /// <summary>Повертає access-токен користувача за email та стандартним тестовим паролем.</summary>
    public static async Task<string> LoginAsync(ApiTestFactory factory, string email)
    {
        using var client = factory.CreateApiClient();
        var response = await client.PostAsync("api/Users/login",
            Json(new { email, password = ApiTestFactory.TestPassword }));
        response.EnsureSuccessStatusCode();
        var data = await ReadDataAsync(response);
        return data.GetProperty("token").GetString()!;
    }

    /// <summary>Формує та надсилає коректно підписану подію Stripe на endpoint webhook.</summary>
    public static async Task<HttpResponseMessage> PostStripeWebhookAsync(ApiTestFactory factory, string payload)
    {
        using var client = factory.CreateApiClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(
            "Stripe-Signature",
            GenerateStripeSignature(payload, ApiTestFactory.WebhookSecret));
        return await client.SendAsync(request);
    }

    /// <summary>Корисне навантаження події <c>checkout.session.completed</c> для конкретного замовлення.</summary>
    public static string BuildCheckoutSessionCompletedPayload(int orderId, decimal amount, string? eventId = null)
    {
        var payload = new
        {
            id = eventId ?? $"evt_test_{Guid.NewGuid():N}",
            @object = "event",
            api_version = StripeConfiguration.ApiVersion,
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = $"cs_test_{Guid.NewGuid():N}",
                    @object = "checkout.session",
                    client_reference_id = orderId.ToString(),
                    payment_intent = $"pi_test_{Guid.NewGuid():N}",
                    amount_total = (long)Math.Round(amount * 100m),
                    currency = "uah",
                    payment_status = "paid"
                }
            }
        };
        return JsonSerializer.Serialize(payload);
    }

    // Підпис у форматі Stripe: t=<timestamp>,v1=<HMAC-SHA256(secret, "{timestamp}.{payload}")>
    private static string GenerateStripeSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
