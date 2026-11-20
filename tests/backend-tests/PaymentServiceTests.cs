using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Tests;

public class PaymentServiceTests
{
    private static PaymentService CreateService(AppDbContext context)
    {
        var promotionService = new PromotionService(
            new PromotionRepository(context),
            new UserRepository(context),
            new Repository<UserPromotion>(context),
            new Repository<OrderItem>(context));

        return new PaymentService(
            new OrderRepository(context),
            new PaymentRepository(context),
            new PromotionRepository(context),
            new UserRepository(context),
            promotionService,
            new CartRepository(context),
            new Repository<CartItem>(context),
            new Repository<OrderItem>(context),
            new Repository<OrderHistory>(context),
            new Repository<ProcessedStripeEvent>(context),
            new ShippingRepository(context),
            new WarehouseRepository(context),
            new FakeUnitOfWork(context),
            NullLogger<PaymentService>.Instance,
            TestDb.Config(),
            new Mock<IEmailService>().Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static (AppDbContext Context, Order Order, Product Product) SeedOrder(
        string status = "Processing",
        string paymentStatus = "Completed",
        int orderQuantity = 3,
        int initialStock = 10)
    {
        var context = TestDb.Create();
        var product = new Product { ProductId = 1, Name = "Cup", Price = 100, Weight = 0.3m, CategoryId = 1, Stock = initialStock };
        context.Products.Add(product);
        context.Users.Add(new User { UserId = 1, FirstName = "I", LastName = "P", Email = "u@example.com" });
        var order = new Order { OrderId = 1, UserId = 1, OrderNumber = "KSU-1", Status = status, TotalAmount = 300 };
        order.OrderItems.Add(new OrderItem
        {
            OrderItemId = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = orderQuantity,
            OriginalPrice = 100,
            FinalPrice = 100
        });
        order.Payment = new Payment
        {
            PaymentId = 1,
            OrderId = 1,
            Amount = 300,
            Method = "Card",
            Status = paymentStatus,
            StripePaymentIntentId = "pi_test"
        };
        context.Orders.Add(order);
        context.OutgoingDocuments.Add(new OutgoingDocument
        {
            DocumentId = 1,
            ProductId = 1,
            Quantity = orderQuantity,
            OrderId = 1,
            Reason = "Order",
            DocumentDate = System.DateTime.UtcNow
        });
        context.SaveChanges();
        // Прибираємо відслідковування, щоб сервіс працював із власними інстансами сутностей
        // (інакше виникає конфлікт ключів при видаленні накладних у WarehouseRepository).
        context.ChangeTracker.Clear();
        return (context, order, product);
    }

    [Fact]
    public async Task CancelOrderAndRestoreStock_UnknownOrder_ReturnsFalse()
    {
        var context = TestDb.Create();
        var service = CreateService(context);

        (await service.CancelOrderAndRestoreStock(999, "test")).Should().BeFalse();
    }

    [Fact]
    public async Task CancelOrderAndRestoreStock_AlreadyCancelled_ReturnsTrueWithoutChanges()
    {
        var (context, _, _) = SeedOrder(status: "Cancelled");
        var service = CreateService(context);

        (await service.CancelOrderAndRestoreStock(1, "test")).Should().BeTrue();
        (await context.Orders.FindAsync(1))!.Status.Should().Be("Cancelled");
        (await context.Products.FindAsync(1))!.Stock.Should().Be(10); // залишок не змінюється повторно
    }

    [Fact]
    public async Task CancelOrderAndRestoreStock_RestoresStock_CancelsOrder_RemovesDocuments()
    {
        var (context, _, _) = SeedOrder(orderQuantity: 3, initialStock: 10);
        var service = CreateService(context);

        var cancelled = await service.CancelOrderAndRestoreStock(1, "Скасовано клієнтом");

        cancelled.Should().BeTrue();
        var reloaded = await context.Orders.Include(o => o.Payment).FirstAsync(o => o.OrderId == 1);
        reloaded.Status.Should().Be("Cancelled");
        reloaded.Payment!.Status.Should().Be("Failed");
        (await context.Products.FindAsync(1))!.Stock.Should().Be(13); // 10 + 3
        (await context.OutgoingDocuments.CountAsync(d => d.OrderId == 1)).Should().Be(0);
        (await context.OrderHistories.CountAsync(h => h.OrderId == 1 && h.NewStatus == "Cancelled")).Should().Be(1);
    }

    [Fact]
    public async Task RefundPaymentAsync_OrderWithoutPayment_ReturnsFalse()
    {
        var context = TestDb.Create();
        context.Orders.Add(new Order { OrderId = 1, UserId = 1, OrderNumber = "KSU-1", Status = "Paid", TotalAmount = 100 });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        (await service.RefundPaymentAsync(1)).Should().BeFalse();
    }

    [Fact]
    public async Task RefundPaymentAsync_PaymentNotCompleted_ReturnsFalse()
    {
        var (context, _, _) = SeedOrder(paymentStatus: "Pending");
        var service = CreateService(context);

        (await service.RefundPaymentAsync(1)).Should().BeFalse();
    }

    [Fact]
    public async Task RefundPaymentAsync_MissingStripeIntent_ReturnsFalse()
    {
        var context = TestDb.Create();
        var order = new Order { OrderId = 1, UserId = 1, OrderNumber = "KSU-1", Status = "Paid", TotalAmount = 100 };
        order.Payment = new Payment { PaymentId = 1, OrderId = 1, Amount = 100, Status = "Completed", StripePaymentIntentId = null };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        (await service.RefundPaymentAsync(1)).Should().BeFalse();
    }

    [Fact]
    public async Task HandleWebhookAsync_NoSignatureAndNoSession_ReturnsTrue()
    {
        var service = CreateService(TestDb.Create());

        (await service.HandleWebhookAsync(null, null)).Should().BeTrue();
    }

    [Fact]
    public async Task HandleWebhookAsync_InvalidSignature_ReturnsFalse()
    {
        var service = CreateService(TestDb.Create());

        var handled = await service.HandleWebhookAsync("{\"id\":\"evt_1\"}", "invalid-signature");

        handled.Should().BeFalse();
    }
}
