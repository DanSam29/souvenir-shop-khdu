using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Тести пункту 14.1: тимчасове блокування акаунта після серії невдалих спроб входу
/// (5 невдалих спроб → блокування на 5 хвилин, HTTP 423) та оновлення пари токенів.
/// </summary>
[Collection("integration")]
public sealed class AccountLockoutTests
{
    private const int MaxFailedAttempts = 5;
    private readonly IntegrationTestFixture _fixture;

    public AccountLockoutTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // Реєструє окремого користувача, щоб тести блокування не змінювали стан спільних акаунтів
    private async Task<string> RegisterFreshUserAsync()
    {
        var email = $"lockout_{Guid.NewGuid():N}@example.com";
        using var client = _fixture.Factory.CreateApiClient();
        var response = await client.PostAsync("api/Users/register", ApiTestHelpers.Json(new
        {
            firstName = "Тест",
            lastName = "Блокування",
            email,
            password = ApiTestFactory.TestPassword,
            confirmPassword = ApiTestFactory.TestPassword
        }));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return email;
    }

    private static Task<HttpResponseMessage> AttemptLoginAsync(HttpClient client, string email, string password) =>
        client.PostAsync("api/Users/login", ApiTestHelpers.Json(new { email, password }));

    [Fact]
    public async Task Login_AfterMaxFailedAttempts_LocksAccountWith423()
    {
        var email = await RegisterFreshUserAsync();
        using var client = _fixture.Factory.CreateApiClient();

        // Перші 5 спроб із хибним паролем обробляються як звичайні помилки входу (401)
        for (var i = 0; i < MaxFailedAttempts; i++)
        {
            var failed = await AttemptLoginAsync(client, email, "WrongPass1!");
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Наступна спроба навіть із правильним паролем відхиляється тимчасовим блокуванням (423)
        var locked = await AttemptLoginAsync(client, email, ApiTestFactory.TestPassword);
        locked.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body = await locked.Content.ReadAsStringAsync();
        body.Should().Contain("AccountLocked");
    }

    [Fact]
    public async Task Login_AfterMaxFailedAttempts_SetsLockoutEndInDatabase()
    {
        var email = await RegisterFreshUserAsync();
        using var client = _fixture.Factory.CreateApiClient();

        for (var i = 0; i < MaxFailedAttempts; i++)
        {
            (await AttemptLoginAsync(client, email, "WrongPass1!")).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized);
        }

        await using var db = _fixture.Factory.CreateDbContext();
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);

        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow);
        // У момент блокування лічильник скидається, щоб після завершення блокування відлік почався заново
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ResetsFailedCounter()
    {
        var email = await RegisterFreshUserAsync();
        using var client = _fixture.Factory.CreateApiClient();

        // Дві невдалі спроби — менше за встановлений ліміт
        for (var i = 0; i < 2; i++)
        {
            (await AttemptLoginAsync(client, email, "WrongPass1!")).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized);
        }

        // Успішний вхід має скинути лічильник невдалих спроб
        var ok = await AttemptLoginAsync(client, email, ApiTestFactory.TestPassword);
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = _fixture.Factory.CreateDbContext();
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);

        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokenPair()
    {
        var email = await RegisterFreshUserAsync();
        using var client = _fixture.Factory.CreateApiClient();

        var login = await AttemptLoginAsync(client, email, ApiTestFactory.TestPassword);
        login.EnsureSuccessStatusCode();
        var loginData = await ApiTestHelpers.ReadDataAsync(login);
        var refreshToken = loginData.GetProperty("refreshToken").GetString();
        refreshToken.Should().NotBeNullOrEmpty();

        var response = await client.PostAsync("api/Users/refresh", ApiTestHelpers.Json(new { refreshToken }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await ApiTestHelpers.ReadDataAsync(response);
        refreshed.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        // Ротація refresh-токена: новий токен відрізняється від використаного
        refreshed.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }
}
