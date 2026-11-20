using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Інтеграційні тести складу: створення прибуткових/видаткових накладних, перерахунок залишків
/// (Product.Stock має дорівнювати SUM(прибуткові) - SUM(видаткові)) та відхилення нестачі товару.
/// </summary>
[Collection("integration")]
public sealed class WarehouseIntegrationTests
{
    // Товар, який не використовується в DbSeeder, тому його залишок визначається лише документами тесту
    private const int IsolatedProductId = 8;

    private readonly IntegrationTestFixture _fixture;

    public WarehouseIntegrationTests(IntegrationTestFixture fixture) => _fixture = fixture;

    private async Task<HttpClient> CreateManagerClientAsync()
    {
        var token = await ApiTestHelpers.LoginAsync(_fixture.Factory, "manager@ksu.edu.ua");
        return _fixture.Factory.CreateApiClient(token);
    }

    [Fact]
    public async Task IncomingAndOutgoingDocuments_KeepStockConsistentWithDocuments()
    {
        using var manager = await CreateManagerClientAsync();

        // Створення двох прибуткових накладних (+10, +5)
        var incoming10 = await manager.PostAsync("api/admin/incoming", ApiTestHelpers.Json(new
        {
            productId = IsolatedProductId,
            quantity = 10,
            purchasePrice = 100m,
            reason = "INCOMING",
            companyId = 1
        }));
        incoming10.StatusCode.Should().Be(HttpStatusCode.OK);
        var incoming10Id = (await ApiTestHelpers.ReadDataAsync(incoming10)).GetProperty("documentId").GetInt32();

        var incoming5 = await manager.PostAsync("api/admin/incoming", ApiTestHelpers.Json(new
        {
            productId = IsolatedProductId,
            quantity = 5,
            purchasePrice = 95m,
            reason = "INCOMING",
            companyId = 1
        }));
        incoming5.StatusCode.Should().Be(HttpStatusCode.OK);

        // Списання (видаткова накладна, -3)
        var outgoing3 = await manager.PostAsync("api/admin/outgoing", ApiTestHelpers.Json(new
        {
            productId = IsolatedProductId,
            quantity = 3,
            reason = "Inventory"
        }));
        outgoing3.StatusCode.Should().Be(HttpStatusCode.OK);

        // Залишок = 10 + 5 - 3 = 12
        var stockItem = await GetStockItemAsync(manager, IsolatedProductId);
        stockItem.GetProperty("totalIncoming").GetInt32().Should().Be(15);
        stockItem.GetProperty("totalOutgoing").GetInt32().Should().Be(3);
        stockItem.GetProperty("currentStock").GetInt32().Should().Be(12);

        // Пряма перевірка в БД: Product.Stock синхронізовано з документами
        await using (var db = _fixture.Factory.CreateDbContext())
        {
            var product = await db.Products.AsNoTracking().FirstAsync(p => p.ProductId == IsolatedProductId);
            product.Stock.Should().Be(12);
        }

        // Видалення прибуткової накладної повертає залишок: 12 - 10 = 2
        var delete = await manager.DeleteAsync($"api/admin/incoming/{incoming10Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var stockAfterDelete = await GetStockItemAsync(manager, IsolatedProductId);
        stockAfterDelete.GetProperty("totalIncoming").GetInt32().Should().Be(5);
        stockAfterDelete.GetProperty("currentStock").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task OutgoingDocument_WithQuantityOverStock_IsRejected()
    {
        using var manager = await CreateManagerClientAsync();

        var response = await manager.PostAsync("api/admin/outgoing", ApiTestHelpers.Json(new
        {
            productId = IsolatedProductId,
            quantity = 10_000_000,
            reason = "Inventory"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WarehouseEndpoints_WithoutManagerRole_ReturnForbidden()
    {
        var customerToken = await ApiTestHelpers.LoginAsync(_fixture.Factory, "customer@ksu.edu.ua");
        using var customer = _fixture.Factory.CreateApiClient(customerToken);

        var response = await customer.GetAsync("api/admin/stock");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<System.Text.Json.JsonElement> GetStockItemAsync(HttpClient manager, int productId)
    {
        var stock = await ApiTestHelpers.ReadDataAsync(await manager.GetAsync("api/admin/stock"));
        return stock.EnumerateArray()
            .Single(e => e.GetProperty("productId").GetInt32() == productId)
            .Clone();
    }
}
