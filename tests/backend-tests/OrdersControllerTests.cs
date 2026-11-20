using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using KhduSouvenirShop.API.Controllers;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Tests;

public class OrdersControllerTests
{
    private static OrdersController CreateController(
        AppDbContext context,
        Mock<IPaymentService> paymentMock,
        Mock<IEmailService> emailMock)
    {
        var promotionService = new PromotionService(
            new PromotionRepository(context),
            new UserRepository(context),
            new Repository<UserPromotion>(context),
            new Repository<OrderItem>(context));

        return new OrdersController(
            new FakeUnitOfWork(context),
            new CartRepository(context),
            new UserRepository(context),
            new OrderRepository(context),
            new ShippingRepository(context),
            new PaymentRepository(context),
            new PromotionRepository(context),
            new WarehouseRepository(context),
            new Repository<OrderItem>(context),
            new Repository<CartItem>(context),
            new Repository<OrderHistory>(context),
            new Repository<UserPromotion>(context),
            NullLogger<OrdersController>.Instance,
            promotionService,
            paymentMock.Object,
            new Mock<INovaPoshtaService>().Object,
            emailMock.Object,
            TestDb.Config(),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static void SetUser(ControllerBase controller, int userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private static CheckoutDto ValidCheckout() => new()
    {
        RecipientName = "Ivan Petrenko",
        RecipientPhone = "+380501112233",
        City = "Kherson",
        WarehouseNumber = "1",
        PaymentMethod = "CashOnDelivery"
    };

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        var context = TestDb.Create();
        context.Users.Add(new User { UserId = 1, FirstName = "I", LastName = "P", Email = "u@example.com" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new Mock<IPaymentService>(), new Mock<IEmailService>());
        SetUser(controller, 1);

        var result = await controller.Checkout(ValidCheckout());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Checkout_InsufficientStock_ReturnsBadRequest()
    {
        var context = TestDb.Create();
        context.Products.Add(new Product { ProductId = 1, Name = "Cup", Price = 100, Weight = 0.3m, CategoryId = 1, Stock = 1 });
        context.Users.Add(new User { UserId = 1, FirstName = "I", LastName = "P", Email = "u@example.com" });
        context.Carts.Add(new Cart { CartId = 1, UserId = 1 });
        context.CartItems.Add(new CartItem { CartItemId = 1, CartId = 1, ProductId = 1, Quantity = 5 });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new Mock<IPaymentService>(), new Mock<IEmailService>());
        SetUser(controller, 1);

        var result = await controller.Checkout(ValidCheckout());

        result.Should().BeOfType<BadRequestObjectResult>();
        (await context.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Checkout_CashOnDelivery_CreatesOrderAndDecrementsStock()
    {
        var context = TestDb.Create();
        var product = new Product { ProductId = 1, Name = "Cup", Price = 100, Weight = 0.3m, CategoryId = 1, Stock = 10 };
        context.Products.Add(product);
        context.Users.Add(new User { UserId = 1, FirstName = "I", LastName = "P", Email = "u@example.com" });
        context.Carts.Add(new Cart { CartId = 1, UserId = 1 });
        context.CartItems.Add(new CartItem { CartItemId = 1, CartId = 1, ProductId = 1, Quantity = 2 });
        await context.SaveChangesAsync();
        var paymentMock = new Mock<IPaymentService>();
        var emailMock = new Mock<IEmailService>();
        var controller = CreateController(context, paymentMock, emailMock);
        SetUser(controller, 1);

        var result = await controller.Checkout(ValidCheckout());

        result.Should().BeOfType<OkObjectResult>();
        (await context.Orders.CountAsync()).Should().Be(1);
        var order = await context.Orders.Include(o => o.Payment).FirstAsync();
        order.TotalAmount.Should().Be(200); // 2 x 100
        order.Payment!.Method.Should().Be("CashOnDelivery");
        product.Stock.Should().Be(8); // 10 - 2
        (await context.CartItems.CountAsync()).Should().Be(0);
        emailMock.Verify(e => e.SendOrderConfirmationAsync("u@example.com", order.OrderNumber, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_NotFound_ReturnsNotFound()
    {
        var context = TestDb.Create();
        var controller = CreateController(context, new Mock<IPaymentService>(), new Mock<IEmailService>());
        SetUser(controller, 1);

        var result = await controller.CancelOrder(999, null);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelOrder_NotOwner_ReturnsForbid()
    {
        var context = TestDb.Create();
        context.Orders.Add(new Order { OrderId = 1, UserId = 2, OrderNumber = "KSU-1", Status = "Processing", TotalAmount = 100 });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new Mock<IPaymentService>(), new Mock<IEmailService>());
        SetUser(controller, 1); // інший користувач, роль Customer

        var result = await controller.CancelOrder(1, null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CancelOrder_DeliveredOrder_ReturnsBadRequest()
    {
        var context = TestDb.Create();
        context.Orders.Add(new Order { OrderId = 1, UserId = 1, OrderNumber = "KSU-1", Status = "Delivered", TotalAmount = 100 });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new Mock<IPaymentService>(), new Mock<IEmailService>());
        SetUser(controller, 1, "Manager");

        var result = await controller.CancelOrder(1, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CancelOrder_OwnerProcessing_Succeeds()
    {
        var context = TestDb.Create();
        context.Users.Add(new User { UserId = 1, FirstName = "I", LastName = "P", Email = "u@example.com" });
        context.Orders.Add(new Order { OrderId = 1, UserId = 1, OrderNumber = "KSU-1", Status = "Processing", TotalAmount = 100 });
        await context.SaveChangesAsync();
        var paymentMock = new Mock<IPaymentService>();
        paymentMock.Setup(p => p.CancelOrderAndRestoreStock(1, It.IsAny<string>())).ReturnsAsync(true);
        var emailMock = new Mock<IEmailService>();
        var controller = CreateController(context, paymentMock, emailMock);
        SetUser(controller, 1);

        var result = await controller.CancelOrder(1, null);

        result.Should().BeOfType<OkObjectResult>();
        paymentMock.Verify(p => p.CancelOrderAndRestoreStock(1, It.IsAny<string>()), Times.Once);
        emailMock.Verify(e => e.SendOrderCancellationAsync("u@example.com", "KSU-1", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
