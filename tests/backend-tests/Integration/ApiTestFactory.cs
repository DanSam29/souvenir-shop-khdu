using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Фабрика тестового застосунку для інтеграційних тестів: піднімає реальний ASP.NET Core host
/// (WebApplicationFactory) поверх тимчасової MySQL (TestContainers), мокає Nova Poshta (WireMock)
/// та підмінює створення Stripe-сесії, зберігаючи справжню обробку webhook-подій.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string WebhookSecret = "whsec_integration_test_secret";
    public const string TestPassword = "Password123!";
    public const string AdminEmail = "admin@ksu.edu.ua";
    public const string CustomerEmail = "customer@ksu.edu.ua";
    public const string CityRef = "city-ref-1";
    public const string WarehouseRef = "warehouse-ref-1";
    public const decimal DeliveryCost = 85m;

    private readonly string _connectionString;
    private readonly bool _enableRateLimiting;
    private readonly WireMockServer _novaPoshta;

    /// <param name="connectionString">Рядок підключення до тимчасової MySQL-контейнера.</param>
    /// <param name="enableRateLimiting">
    /// Якщо <c>true</c>, реальні ліміти швидкості залишаються активними (потрібно для безпекових тестів на 429).
    /// За замовчуванням вони знімаються, щоб серія звичайних тестів не отримувала помилок 429.
    /// </param>
    public ApiTestFactory(string connectionString, bool enableRateLimiting = false)
    {
        _connectionString = connectionString;
        _enableRateLimiting = enableRateLimiting;
        _novaPoshta = WireMockServer.Start();
        StubNovaPoshta(_novaPoshta);
    }

    // Мок відповідей Nova Poshta API (реальний сервіс звертається на https://api.novaposhta.ua/v2.0/json/)
    private static void StubNovaPoshta(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/v2.0/json/").UsingPost()
                .WithBody(new WildcardMatcher("*getCities*")))
            .RespondWith(Response.Create().WithBodyAsJson(new
            {
                success = true,
                data = new[] { new { Description = "Київ", Ref = CityRef, AreaDescription = "Київська" } },
                errors = Array.Empty<string>(),
                warnings = Array.Empty<string>()
            }));

        server.Given(Request.Create().WithPath("/v2.0/json/").UsingPost()
                .WithBody(new WildcardMatcher("*getWarehouses*")))
            .RespondWith(Response.Create().WithBodyAsJson(new
            {
                success = true,
                data = new[] { new { Description = "Відділення №1: вул. Хрещатик, 1", Ref = WarehouseRef, Number = "1" } },
                errors = Array.Empty<string>(),
                warnings = Array.Empty<string>()
            }));

        server.Given(Request.Create().WithPath("/v2.0/json/").UsingPost()
                .WithBody(new WildcardMatcher("*getDocumentPrice*")))
            .RespondWith(Response.Create().WithBodyAsJson(new
            {
                success = true,
                data = new[] { new { Cost = DeliveryCost } },
                errors = Array.Empty<string>(),
                warnings = Array.Empty<string>()
            }));

        server.Given(Request.Create().WithPath("/v2.0/json/").UsingPost()
                .WithBody(new WildcardMatcher("*getStatusDocuments*")))
            .RespondWith(Response.Create().WithBodyAsJson(new
            {
                success = true,
                data = new[]
                {
                    new
                    {
                        Status = "Отримано",
                        StatusCode = "9",
                        WarehouseRecipient = "Відділення №1",
                        RecipientFullName = "Тест Тестовий",
                        ScheduledDeliveryDate = "2026-11-25"
                    }
                },
                errors = Array.Empty<string>(),
                warnings = Array.Empty<string>()
            }));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Stripe:SecretKey"] = "sk_test_integration",
                ["Stripe:WebhookSecret"] = WebhookSecret,
                ["Features:StripeEnabled"] = "true",
                ["Features:NovaPoshtaEnabled"] = "true",
                ["NovaPoshta:ApiKey"] = "np-integration-key"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // 0. Перевизначити AppDbContext на тимчасову БД контейнера. Program.cs читає рядок
            //    підключення з appsettings на етапі реєстрації (до застосування тестових налаштувань),
            //    тож без цього тести писали б у реальну БД з appsettings.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

            // 1. Вимкнути лише фонові сервіси застосунку (щоб уникнути реальних SMTP-запитів),
            //    зберігаючи GenericWebHostService, який запускає TestServer.
            var backgroundServices = new List<ServiceDescriptor>();
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType != typeof(IHostedService) || descriptor.ImplementationType is null)
                {
                    continue;
                }

                var name = descriptor.ImplementationType.Name;
                if (name is nameof(VerificationQueueBackgroundService)
                    or nameof(StudentStatusExpiryBackgroundService)
                    or nameof(EmailQueueBackgroundService))
                {
                    backgroundServices.Add(descriptor);
                }
            }
            foreach (var descriptor in backgroundServices)
            {
                services.Remove(descriptor);
            }

            // 2. Зняти обмеження швидкості, щоб серія тестів не отримувала 429.
            //    Для безпекових тестів (enableRateLimiting: true) ліміти лишаються реальними.
            if (!_enableRateLimiting)
            {
                services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
                services.Configure<RateLimiterOptions>(options =>
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                        _ => RateLimitPartition.GetNoLimiter("integration"));
                    options.AddPolicy("AuthPolicy", _ => RateLimitPartition.GetNoLimiter("integration"));
                    options.AddPolicy("NovaPoshtaPolicy", _ => RateLimitPartition.GetNoLimiter("integration"));
                });
            }

            // 3. Направити HttpClient сервісу Nova Poshta на WireMock замість реального API
            services.AddHttpClient<INovaPoshtaService, NovaPoshtaService>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new RewriteHostHandler(_novaPoshta.Url!, new HttpClientHandler()));

            // 4. Підмінити IPaymentService: створення Stripe-сесії не робить мережевих викликів,
            //    але обробка webhook виконується реальним PaymentService.
            services.RemoveAll<IPaymentService>();
            services.AddScoped<PaymentService>();
            services.AddScoped<IPaymentService>(sp => new TestPaymentService(sp.GetRequiredService<PaymentService>()));
        });
    }

    /// <summary>Створює HTTP-клієнт до тестового host (без автоперенаправлень).</summary>
    public HttpClient CreateApiClient(string? accessToken = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        });
        if (!string.IsNullOrEmpty(accessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }
        return client;
    }

    /// <summary>Прямий доступ до БД тестового контейнера (для перевірок після запитів).</summary>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        return new AppDbContext(options);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _novaPoshta.Stop();
        }
    }
}

/// <summary>Переадресовує всі вихідні HTTP-запити на заданий хост (WireMock), зберігаючи шлях.</summary>
internal sealed class RewriteHostHandler : DelegatingHandler
{
    private readonly Uri _baseUri;

    public RewriteHostHandler(string baseUrl, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _baseUri = new Uri(baseUrl);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            request.RequestUri = new UriBuilder(request.RequestUri)
            {
                Scheme = _baseUri.Scheme,
                Host = _baseUri.Host,
                Port = _baseUri.Port
            }.Uri;
        }
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Обгортка над реальним <see cref="PaymentService"/>: лише створення Stripe-сесії підміняється
/// локальною заглушкою (щоб не робити реальних запитів до Stripe), решта логіки — справжня.
/// </summary>
internal sealed class TestPaymentService : IPaymentService
{
    private readonly IPaymentService _inner;

    public TestPaymentService(IPaymentService inner) => _inner = inner;

    public Task<Session> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
    {
        var session = new Session
        {
            Id = $"cs_test_{Guid.NewGuid():N}",
            Url = $"https://checkout.stripe.test/{Guid.NewGuid():N}",
            ClientReferenceId = order.OrderId.ToString(),
            AmountTotal = (long)Math.Round(order.TotalAmount * 100m),
            PaymentIntentId = $"pi_test_{Guid.NewGuid():N}"
        };
        return Task.FromResult(session);
    }

    public Task<Session> CreateCheckoutSessionForCartAsync(User user, Cart cart, List<Promotion> promos,
        string? promoCode, string successUrl, string cancelUrl, Dictionary<string, string> metadata)
        => _inner.CreateCheckoutSessionForCartAsync(user, cart, promos, promoCode, successUrl, cancelUrl, metadata);

    public Task<bool> HandleWebhookAsync(string? json, string? stripeSignature, string? sessionId = null)
        => _inner.HandleWebhookAsync(json, stripeSignature, sessionId);

    public Event ConstructEventAsync(string payload, string signatureHeader)
        => _inner.ConstructEventAsync(payload, signatureHeader);

    public Task<Refund?> CreateRefundAsync(string paymentIntentId, decimal? amount = null)
        => _inner.CreateRefundAsync(paymentIntentId, amount);

    public Task<bool> RefundPaymentAsync(int orderId, string? reason = null)
        => _inner.RefundPaymentAsync(orderId, reason);

    public Task<bool> CancelOrderAndRestoreStock(int orderId, string comment)
        => _inner.CancelOrderAndRestoreStock(orderId, comment);
}
