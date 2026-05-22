using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Services;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Tests;

public class PromotionServiceTests
{
    private AppDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }
    [Fact]
    public async Task GetActivePromotionsForUserAsync_FiltersByAudienceAndDates()
    {
        var context = CreateInMemoryContext("promos1");
        var now = DateTime.UtcNow;
        context.Promotions.AddRange(new[] {
            new Promotion { PromotionId = 1, IsActive = true, StartDate = now.AddDays(-1), EndDate = now.AddDays(1), AudienceType = "ALL", Priority = 1 },
            new Promotion { PromotionId = 2, IsActive = true, StartDate = now.AddDays(-5), EndDate = now.AddDays(-1), AudienceType = "ALL", Priority = 5 }, // термін дії закінчився
            new Promotion { PromotionId = 3, IsActive = true, StartDate = null, EndDate = null, AudienceType = "STUDENT", Priority = 10 },
            new Promotion { PromotionId = 4, IsActive = false, StartDate = null, EndDate = null, AudienceType = "ALL", Priority = 2 }, // не активна
        });
        await context.SaveChangesAsync();
        var service = new PromotionService(context);
        var forAnon = await service.GetActivePromotionsForUserAsync(null);
        Assert.Single(forAnon);
        Assert.Equal(1, forAnon[0].PromotionId);
        var forStudent = await service.GetActivePromotionsForUserAsync("STUDENT");
        Assert.Equal(2, forStudent.Count);
        Assert.Equal(3, forStudent[0].PromotionId); // Перевірка сортування за пріоритетом у порядку спадання
    }
    [Fact]
    public void GetPriceAfterPromotions_AppliesPercentagePromotionsCorrectly()
    {
        var context = CreateInMemoryContext("promos2");
        var service = new PromotionService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 10 }, // 10% знижки на товар
            new Promotion { PromotionId = 2, Type = "PERCENTAGE", TargetType = "CATEGORY", TargetId = 10, Value = 5 }, // 5% знижки на категорію
            new Promotion { PromotionId = 3, Type = "PERCENTAGE", TargetType = "CART", TargetId = null, Value = 20 }, // 20% знижки на кошик
            new Promotion { PromotionId = 4, Type = "FIXED", TargetType = "PRODUCT", TargetId = 100, Value = 50 }, // ігнорується
        };
        var priceAfter = service.GetPriceAfterPromotions(product, promotions);
        // Розрахунок кумулятивної знижки:
        // 1. Базова ціна: 200.00
        // 2. Знижка на товар (10%): 200.00 - 20.00 = 180.00
        // 3. Знижка на категорію (5%): 180.00 - 9.00 = 171.00
        // 4. Знижка кошика (20%): 171.00 - 34.20 = 136.80
        Assert.Equal(136.80m, priceAfter);
    }
}