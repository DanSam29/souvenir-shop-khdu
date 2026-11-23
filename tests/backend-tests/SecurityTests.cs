using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Безпекові тести (пункт 13.5): час життя JWT, RBAC (401/403), SQL-ін'єкції, XSS,
/// зберігання паролів лише у вигляді BCrypt-хешів.
/// </summary>
[Collection("integration")]
public sealed class SecurityTests
{
    private readonly IntegrationTestFixture _fixture;

    public SecurityTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ---------- JWT ----------

    [Fact]
    public async Task AccessToken_ExpiresInOneHour()
    {
        // Час життя access-токена має дорівнювати 1 годині (пункт 13.5 / 14.1)
        var config = _fixture.Factory.Services.GetRequiredService<IConfiguration>();
        config["Jwt:ExpireMinutes"].Should().Be("60");

        using var client = _fixture.Factory.CreateApiClient();
        var response = await client.PostAsync("api/Users/login",
            ApiTestHelpers.Json(new { email = ApiTestFactory.CustomerEmail, password = ApiTestFactory.TestPassword }));
        response.EnsureSuccessStatusCode();

        var data = await ApiTestHelpers.ReadDataAsync(response);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(data.GetProperty("token").GetString()!);

        (jwt.ValidTo - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExpiredToken_IsRejected_With401()
    {
        // Навіть коректно підписаний, але прострочений токен не має давати доступ
        using var client = _fixture.Factory.CreateApiClient(CreateExpiredToken());

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- RBAC ----------

    [Fact]
    public async Task AdminEndpoint_WithoutToken_Returns401()
    {
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(ApiTestFactory.CustomerEmail)]
    [InlineData("manager@ksu.edu.ua")]
    public async Task AdminEndpoint_WithNonAdminRole_Returns403(string email)
    {
        // Customer і Manager не мають прав на адмін-ендпоінти (доступ лише Administrator/SuperAdmin)
        var token = await ApiTestHelpers.LoginAsync(_fixture.Factory, email);
        using var client = _fixture.Factory.CreateApiClient(token);

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_Returns200()
    {
        var token = await ApiTestHelpers.LoginAsync(_fixture.Factory, ApiTestFactory.AdminEmail);
        using var client = _fixture.Factory.CreateApiClient(token);

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- SQL Injection ----------

    [Fact]
    public async Task Search_WithSqlInjectionPayload_IsTreatedAsLiteral()
    {
        // EF Core параметризує запити, тож '" OR 1=1 --' не обходить фільтр і не спричиняє помилки сервера
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync("api/Products?search=" + Uri.EscapeDataString("\" OR 1=1 --"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.GetProperty("totalItems").GetInt32().Should().Be(0);
    }

    // ---------- XSS ----------

    [Fact]
    public async Task XssPayload_InStoredField_IsConfinedToJsonValue()
    {
        // Payload водночас намагається вирватися з рядка JSON і підмінити роль.
        // Серіалізатор має екранувати лапки, тож значення лишається звичайним текстом,
        // а не виконуваним HTML і не новою властивістю об'єкта.
        const string payload = "<script>alert('xss')</script>\",\"role\":\"SuperAdmin\"";
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.PostAsync("api/Users/register", ApiTestHelpers.Json(new
        {
            firstName = payload,
            lastName = "Тест",
            email = $"xss_{Guid.NewGuid():N}@example.com",
            password = ApiTestFactory.TestPassword,
            confirmPassword = ApiTestFactory.TestPassword
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.GetProperty("firstName").GetString().Should().Be(payload); // значення збережено як звичайний текст
        data.GetProperty("role").GetString().Should().Be("Customer");   // роль НЕ підмінена ін'єкцією в JSON
    }

    [Fact]
    public async Task XssPayload_InSearch_ReturnsInertJsonData()
    {
        // Маліційний ввід у параметрі пошуку не виконується й не ламає відповідь:
        // сервер повертає коректний JSON (не HTML-документ) без жодного збігу.
        const string payload = "<script>alert('xss')</script>";
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync("api/Products?search=" + Uri.EscapeDataString(payload));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.GetProperty("totalItems").GetInt32().Should().Be(0);
    }

    // ---------- BCrypt ----------

    [Fact]
    public async Task Passwords_AreStoredAsBcryptHashes()
    {
        // У БД мають зберігатися лише BCrypt-хеші (workFactor=12), а не відкритий текст
        await using var db = _fixture.Factory.CreateDbContext();

        var user = await db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Email == ApiTestFactory.CustomerEmail);

        user.Password.Should().NotBe(ApiTestFactory.TestPassword);
        user.Password.Should().NotContain(ApiTestFactory.TestPassword);
        Regex.IsMatch(user.Password, @"^\$2[aby]\$12\$").Should().BeTrue(
            "пароль має зберігатися як BCrypt-хеш з workFactor=12");
        BCrypt.Net.BCrypt.Verify(ApiTestFactory.TestPassword, user.Password).Should().BeTrue();
    }

    // Створює коректно підписаний, але вже прострочений JWT (для перевірки ValidateLifetime)
    private string CreateExpiredToken()
    {
        var config = _fixture.Factory.Services.GetRequiredService<IConfiguration>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Administrator")
            },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Перевірка обмеження швидкості входу (rate limiting) з РЕАЛЬНО активними лімітами:
/// ті самі налаштування, що й у production, але на окремій фабриці, щоб не впливати на решту тестів.
/// </summary>
[Collection("integration")]
public sealed class RateLimitSecurityTests
{
    private readonly IntegrationTestFixture _fixture;

    public RateLimitSecurityTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_AfterTooManyAttempts_Returns429()
    {
        // AuthPolicy: 5 спроб входу на 5 хвилин з однієї IP-адреси
        using var factory = new ApiTestFactory(_fixture.ConnectionString, enableRateLimiting: true);
        using var client = factory.CreateApiClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsync("api/Users/login",
                ApiTestHelpers.Json(new { email = "nobody@example.com", password = "WrongPass1!" }));
            statuses.Add(response.StatusCode);
        }

        // Перші 5 спроб обробляються (401 — невірні дані), шоста відхиляється лімітом
        statuses.Take(5).Should().OnlyContain(status => status == HttpStatusCode.Unauthorized);
        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }
}
