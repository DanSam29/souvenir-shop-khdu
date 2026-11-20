using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Інтеграційні тести інтеграції з Nova Poshta: усі зовнішні виклики переспрямовуються на WireMock-заглушку,
/// тому перевіряється повний шлях запиту (контролер → сервіс → HTTP) без реального API.
/// </summary>
[Collection("integration")]
public sealed class NovaPoshtaMockTests
{
    private readonly IntegrationTestFixture _fixture;

    public NovaPoshtaMockTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Cities_ReturnsCityFromMockedNovaPoshta()
    {
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync("api/np/cities?search=Київ");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        var city = data.EnumerateArray().First();
        city.GetProperty("description").GetString().Should().Be("Київ");
        city.GetProperty("ref").GetString().Should().Be(ApiTestFactory.CityRef);
    }

    [Fact]
    public async Task Warehouses_ReturnsWarehouseFromMockedNovaPoshta()
    {
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync($"api/np/warehouses?cityRef={ApiTestFactory.CityRef}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ApiTestHelpers.ReadDataAsync(response);
        var warehouse = data.EnumerateArray().First();
        warehouse.GetProperty("ref").GetString().Should().Be(ApiTestFactory.WarehouseRef);
        warehouse.GetProperty("number").GetString().Should().Be("1");
    }

    [Fact]
    public async Task Calculate_ReturnsCostFromMockedNovaPoshta()
    {
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.PostAsync("api/np/calculate", ApiTestHelpers.Json(new
        {
            cityRef = ApiTestFactory.CityRef,
            warehouseRef = ApiTestFactory.WarehouseRef,
            weight = 2.5m,
            totalAmount = 500m
        }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.GetProperty("cost").GetDecimal().Should().Be(ApiTestFactory.DeliveryCost);
        data.GetProperty("estimatedDays").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Tracking_ReturnsStatusFromMockedNovaPoshta()
    {
        using var client = _fixture.Factory.CreateApiClient();

        var response = await client.GetAsync("api/np/tracking/20450000000001");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await ApiTestHelpers.ReadDataAsync(response);
        data.GetProperty("trackingNumber").GetString().Should().Be("20450000000001");
        data.GetProperty("status").GetString().Should().Be("Отримано");
        data.GetProperty("statusCode").GetString().Should().Be("9");
        data.GetProperty("available").GetBoolean().Should().BeTrue();
    }
}
