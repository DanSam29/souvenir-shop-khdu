using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Repositories;
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
    private static PromotionService CreateService(AppDbContext context) =>
        new PromotionService(
            new PromotionRepository(context),
            new UserRepository(context),
            new Repository<UserPromotion>(context),
            new Repository<OrderItem>(context));
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
        var service = CreateService(context);
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
        var service = CreateService(context);
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
    [Fact]
    public void GetPriceAfterPromotions_StacksPromotionsByPriorityTiers()
    {
        var context = CreateInMemoryContext("promos3");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "PERCENTAGE", TargetType = "CART", TargetId = null, Value = 10, AudienceType = "CUSTOM" }, // рівень 1: персональна
            new Promotion { PromotionId = 2, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 20, AudienceType = "REGULAR" }, // рівень 2: студентська
            new Promotion { PromotionId = 3, Type = "PERCENTAGE", TargetType = "CATEGORY", TargetId = 10, Value = 10, AudienceType = "ALL" }, // рівень 3: категорійна
            new Promotion { PromotionId = 4, Type = "PERCENTAGE", TargetType = "CART", TargetId = null, Value = 10, AudienceType = "ALL" }, // рівень 4: загальна
        };
        var priceAfter = service.GetPriceAfterPromotions(product, promotions);
        // 200.00 -> 180.00 (10%) -> 144.00 (20%) -> 129.60 (10%) -> 116.64 (10%)
        Assert.Equal(116.64m, priceAfter);
    }
    [Fact]
    public void GetPriceAfterPromotions_HigherPriorityWinsOverBiggerDiscount()
    {
        var context = CreateInMemoryContext("promos4");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 5, Priority = 90 }, // менша знижка, вищий пріоритет
            new Promotion { PromotionId = 2, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 50, Priority = 10 }, // більша знижка, нижчий пріоритет
        };
        var priceAfter = service.GetPriceAfterPromotions(product, promotions);
        Assert.Equal(190.00m, priceAfter);
    }
    [Fact]
    public void GetPriceAfterPromotions_SamePriority_PicksBiggestDiscount()
    {
        var context = CreateInMemoryContext("promos5");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 5, Priority = 50 },
            new Promotion { PromotionId = 2, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 30, Priority = 50 },
        };
        var priceAfter = service.GetPriceAfterPromotions(product, promotions);
        Assert.Equal(140.00m, priceAfter);
    }
    [Fact]
    public void GetPriceAfterPromotions_ClampsFinalPriceToZero()
    {
        var context = CreateInMemoryContext("promos6");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "FIXED_AMOUNT", TargetType = "PRODUCT", TargetId = 100, Value = 1000, Priority = 50 },
        };
        var priceAfter = service.GetPriceAfterPromotions(product, promotions);
        Assert.Equal(0m, priceAfter);
    }
    [Fact]
    public void GetPriceAfterPromotionsWithPromotion_ReturnsAppliedPromotionId()
    {
        var context = CreateInMemoryContext("promos7");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var promotions = new List<Promotion>
        {
            new Promotion { PromotionId = 1, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 10 },
            new Promotion { PromotionId = 2, Type = "PERCENTAGE", TargetType = "PRODUCT", TargetId = 100, Value = 30 },
        };
        var (price, appliedPromotionId) = service.GetPriceAfterPromotionsWithPromotion(product, promotions);
        // Обидві акції на одну ціль — перемагає більша знижка (30%): 200.00 -> 140.00, id = 2
        Assert.Equal(140.00m, price);
        Assert.Equal(2, appliedPromotionId);
    }
    [Fact]
    public void GetPriceAfterPromotionsWithPromotion_NoPromotions_ReturnsNullId()
    {
        var context = CreateInMemoryContext("promos8");
        var service = CreateService(context);
        var product = new Product { ProductId = 100, CategoryId = 10, Price = 200m };
        var (price, appliedPromotionId) = service.GetPriceAfterPromotionsWithPromotion(product, new List<Promotion>());
        Assert.Equal(200m, price);
        Assert.Null(appliedPromotionId);
    }
    private static List<CartItem> CreateCartItems(decimal unitPrice = 100m, int quantity = 1) =>
        new List<CartItem>
        {
            new CartItem
            {
                ProductId = 100,
                Quantity = quantity,
                Product = new Product { ProductId = 100, CategoryId = 10, Price = unitPrice }
            }
        };
    [Fact]
    public async Task ValidatePromoCodeAsync_ValidCode_ReturnsPromotion()
    {
        var context = CreateInMemoryContext("promocode1");
        context.Promotions.Add(new Promotion
        {
            PromotionId = 1, Name = "Test", Type = "PERCENTAGE", Value = 10,
            PromoCode = "SAVE10", IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("SAVE10", 1, CreateCartItems());
        Assert.True(result.Valid);
        Assert.Null(result.Error);
        Assert.Equal(1, result.Promotion!.PromotionId);
    }
    [Fact]
    public async Task ValidatePromoCodeAsync_UnknownCode_ReturnsNotFoundMessage()
    {
        var context = CreateInMemoryContext("promocode2");
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("NOPE", 1, CreateCartItems());
        Assert.False(result.Valid);
        Assert.Equal("Промокод не знайдено", result.Error);
    }
    [Fact]
    public async Task ValidatePromoCodeAsync_Expired_ReturnsExpiredMessage()
    {
        var context = CreateInMemoryContext("promocode3");
        context.Promotions.Add(new Promotion
        {
            PromotionId = 1, Name = "Test", Type = "PERCENTAGE", Value = 10,
            PromoCode = "OLD", IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("OLD", 1, CreateCartItems());
        Assert.False(result.Valid);
        Assert.Equal("Термін дії закінчився", result.Error);
    }
    [Fact]
    public async Task ValidatePromoCodeAsync_UsageLimitReached_ReturnsExhaustedMessage()
    {
        var context = CreateInMemoryContext("promocode4");
        context.Promotions.Add(new Promotion
        {
            PromotionId = 1, Name = "Test", Type = "PERCENTAGE", Value = 10,
            PromoCode = "LIMIT", IsActive = true, UsageLimit = 5, CurrentUsage = 5
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("LIMIT", 1, CreateCartItems());
        Assert.False(result.Valid);
        Assert.Equal("Промокод вичерпано", result.Error);
    }
    [Fact]
    public async Task ValidatePromoCodeAsync_BelowMinOrderAmount_ReturnsMinAmountMessage()
    {
        var context = CreateInMemoryContext("promocode5");
        context.Promotions.Add(new Promotion
        {
            PromotionId = 1, Name = "Test", Type = "PERCENTAGE", Value = 10,
            PromoCode = "MIN500", IsActive = true, MinOrderAmount = 500m
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("MIN500", 1, CreateCartItems(unitPrice: 100m, quantity: 1));
        Assert.False(result.Valid);
        Assert.Equal("Мінімальна сума: 500 грн", result.Error);
    }
    [Fact]
    public async Task ValidatePromoCodeAsync_BelowMinQuantity_ReturnsMinQuantityMessage()
    {
        var context = CreateInMemoryContext("promocode6");
        context.Promotions.Add(new Promotion
        {
            PromotionId = 1, Name = "Test", Type = "PERCENTAGE", Value = 10,
            PromoCode = "MINQTY3", IsActive = true, MinQuantity = 3
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var result = await service.ValidatePromoCodeAsync("MINQTY3", 1, CreateCartItems(unitPrice: 100m, quantity: 2));
        Assert.False(result.Valid);
        Assert.Equal("Мінімальна кількість: 3", result.Error);
    }
    [Fact]
    public async Task AssignPersonalPromotionAsync_AssignsOnceAndRejectsDuplicate()
    {
        var context = CreateInMemoryContext("assign1");
        context.Promotions.Add(new Promotion { PromotionId = 1, Name = "Personal", IsActive = true });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var first = await service.AssignPersonalPromotionAsync(5, 1);
        var second = await service.AssignPersonalPromotionAsync(5, 1);
        Assert.True(first);
        Assert.False(second);
        Assert.Equal(1, context.UserPromotions.Count(up => up.UserId == 5 && up.PromotionId == 1));
    }
    [Fact]
    public async Task AssignPersonalPromotionAsync_UnknownPromotion_ReturnsFalse()
    {
        var context = CreateInMemoryContext("assign2");
        var service = CreateService(context);
        var result = await service.AssignPersonalPromotionAsync(5, 999);
        Assert.False(result);
    }
    [Fact]
    public async Task GetPromotionStatisticsAsync_CalculatesRoiMetrics()
    {
        var context = CreateInMemoryContext("stats1");
        context.Promotions.Add(new Promotion { PromotionId = 1, Name = "Sale", IsActive = true, CurrentUsage = 2, UsageLimit = 10 });
        context.UserPromotions.AddRange(
            new UserPromotion { UserPromotionId = 1, UserId = 1, PromotionId = 1 },
            new UserPromotion { UserPromotionId = 2, UserId = 2, PromotionId = 1 });
        context.OrderItems.AddRange(
            new OrderItem { OrderItemId = 1, OrderId = 100, ProductId = 1, Quantity = 2, OriginalPrice = 100, DiscountAmount = 20, FinalPrice = 80, AppliedPromotionId = 1 },
            new OrderItem { OrderItemId = 2, OrderId = 100, ProductId = 2, Quantity = 1, OriginalPrice = 50, DiscountAmount = 5, FinalPrice = 45, AppliedPromotionId = 1 },
            new OrderItem { OrderItemId = 3, OrderId = 101, ProductId = 3, Quantity = 3, OriginalPrice = 10, DiscountAmount = 0, FinalPrice = 10, AppliedPromotionId = null });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var stats = await service.GetPromotionStatisticsAsync(1);
        Assert.NotNull(stats);
        Assert.Equal(1, stats!.TotalOrders); // лише замовлення з AppliedPromotionId = 1
        Assert.Equal(3, stats.TotalQuantitySold);
        Assert.Equal(25m, stats.TotalDiscountGiven); // 20 + 5
        Assert.Equal(205m, stats.TotalRevenue); // 80*2 + 45*1
        Assert.Equal(2, stats.AssignedUsers);
        Assert.Equal(2, stats.CurrentUsage);
    }
    [Fact]
    public async Task GetPromotionStatisticsAsync_UnknownPromotion_ReturnsNull()
    {
        var context = CreateInMemoryContext("stats2");
        var service = CreateService(context);
        var stats = await service.GetPromotionStatisticsAsync(42);
        Assert.Null(stats);
    }
}