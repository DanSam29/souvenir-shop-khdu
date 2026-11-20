using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;
using Xunit;

namespace KhduSouvenirShop.API.Tests;

public class WarehouseServiceTests
{
    private static WarehouseService CreateService(AppDbContext context, int threshold = 5) =>
        new WarehouseService(
            new ProductRepository(context),
            new Repository<IncomingDocument>(context),
            new Repository<OutgoingDocument>(context),
            TestDb.Config(("Warehouse:LowStockThreshold", threshold.ToString())));

    [Fact]
    public async Task RecalculateStockAsync_ComputesIncomingMinusOutgoing()
    {
        var context = TestDb.Create();
        context.Products.Add(new Product { ProductId = 1, Name = "P", Stock = 999 });
        context.IncomingDocuments.AddRange(
            new IncomingDocument { DocumentId = 1, ProductId = 1, Quantity = 30 },
            new IncomingDocument { DocumentId = 2, ProductId = 1, Quantity = 20 });
        context.OutgoingDocuments.Add(
            new OutgoingDocument { DocumentId = 1, ProductId = 1, Quantity = 15 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var stock = await service.RecalculateStockAsync(1);

        stock.Should().Be(35); // 30 + 20 - 15
        (await context.Products.FindAsync(1))!.Stock.Should().Be(35);
    }

    [Fact]
    public async Task RecalculateStockAsync_UnknownProduct_ReturnsZero()
    {
        var context = TestDb.Create();
        var service = CreateService(context);

        (await service.RecalculateStockAsync(404)).Should().Be(0);
    }

    [Fact]
    public async Task RecalculateStockAsync_NoDocuments_ResetsStockToZero()
    {
        var context = TestDb.Create();
        context.Products.Add(new Product { ProductId = 1, Name = "P", Stock = 42 });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        (await service.RecalculateStockAsync(1)).Should().Be(0);
        (await context.Products.FindAsync(1))!.Stock.Should().Be(0);
    }

    [Fact]
    public void IsLowStock_UsesConfiguredThreshold()
    {
        var service = CreateService(TestDb.Create(), threshold: 5);

        service.LowStockThreshold.Should().Be(5);
        service.IsLowStock(0).Should().BeTrue();
        service.IsLowStock(5).Should().BeTrue();
        service.IsLowStock(6).Should().BeFalse();
    }

    [Fact]
    public void LowStockThreshold_DefaultsToFive_WhenNotConfigured()
    {
        var context = TestDb.Create();
        var service = new WarehouseService(
            new ProductRepository(context),
            new Repository<IncomingDocument>(context),
            new Repository<OutgoingDocument>(context),
            TestDb.Config());

        service.LowStockThreshold.Should().Be(5);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsOnlyAtOrBelowThreshold()
    {
        var context = TestDb.Create();
        context.Products.AddRange(
            new Product { ProductId = 1, Name = "Low", Stock = 2 },
            new Product { ProductId = 2, Name = "Borderline", Stock = 5 },
            new Product { ProductId = 3, Name = "Enough", Stock = 50 });
        await context.SaveChangesAsync();

        var service = CreateService(context, threshold: 5);
        var low = await service.GetLowStockProductsAsync();

        low.Select(p => p.ProductId).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetStockSummaryAsync_ComputesTotalsAndLowStockFlag()
    {
        var context = TestDb.Create();
        context.Products.AddRange(
            new Product { ProductId = 1, Name = "A", Stock = 3 },
            new Product { ProductId = 2, Name = "B", Stock = 40 });
        context.IncomingDocuments.AddRange(
            new IncomingDocument { DocumentId = 1, ProductId = 1, Quantity = 10 },
            new IncomingDocument { DocumentId = 2, ProductId = 2, Quantity = 50 });
        context.OutgoingDocuments.AddRange(
            new OutgoingDocument { DocumentId = 1, ProductId = 1, Quantity = 7 },
            new OutgoingDocument { DocumentId = 2, ProductId = 2, Quantity = 10 });
        await context.SaveChangesAsync();

        var service = CreateService(context, threshold: 5);
        var summary = await service.GetStockSummaryAsync();

        summary.Should().HaveCount(2);
        var a = summary.Single(s => s.ProductId == 1);
        a.TotalIncoming.Should().Be(10);
        a.TotalOutgoing.Should().Be(7);
        a.CurrentStock.Should().Be(3);
        a.LowStock.Should().BeTrue();
        var b = summary.Single(s => s.ProductId == 2);
        b.LowStock.Should().BeFalse();
    }
}
