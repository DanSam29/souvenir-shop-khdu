using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(AppDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("checkout")]
        public async Task<ActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(new { error = "Кошик порожній" });
            }

            foreach (var item in cart.CartItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    return BadRequest(new { error = $"Недостатньо товару '{item.Product.Name}' на складі. Доступно: {item.Product.Stock}" });
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var totalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);
                var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = "Processing",
                    TotalAmount = totalAmount,
                    ShippingCost = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var shipping = new Shipping
                {
                    OrderId = order.OrderId,
                    City = dto.City,
                    WarehouseNumber = dto.WarehouseNumber,
                    CityRef = dto.CityRef,
                    WarehouseRef = dto.WarehouseRef,
                    RecipientName = dto.RecipientName,
                    RecipientPhone = dto.RecipientPhone,
                    TrackingNumber = null
                };

                _context.Shippings.Add(shipping);

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = totalAmount,
                    Method = dto.PaymentMethod ?? "CashOnDelivery",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                // Створюємо OrderItems без знижок
                var orderItems = new List<OrderItem>();
                foreach (var item in cart.CartItems)
                {
                    item.Product.Stock -= item.Quantity;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OriginalPrice = item.Product.Price,
                        DiscountAmount = 0,
                        FinalPrice = item.Product.Price
                    };

                    orderItems.Add(orderItem);
                }

                // Застосування промокоду (за наявності)
                decimal totalDiscount = 0;
                if (!string.IsNullOrWhiteSpace(dto.PromoCode))
                {
                    var now = DateTime.UtcNow;
                    var promo = await _context.Promotions
                        .FirstOrDefaultAsync(p =>
                            p.PromoCode == dto.PromoCode &&
                            p.IsActive &&
                            (p.StartDate == null || p.StartDate <= now) &&
                            (p.EndDate == null || p.EndDate >= now) &&
                            (p.UsageLimit == null || p.CurrentUsage < p.UsageLimit));

                    if (promo != null)
                    {
                        var subtotal = orderItems.Sum(oi => oi.OriginalPrice * oi.Quantity);
                        if (subtotal > 0)
                        {
                            if (promo.Type == "PERCENTAGE")
                            {
                                var percent = Math.Clamp((double)promo.Value, 0, 100);
                                foreach (var oi in orderItems)
                                {
                                    var perUnitDiscount = Math.Round(oi.OriginalPrice * (decimal)(percent / 100.0), 2);
                                    var totalItemDiscount = perUnitDiscount * oi.Quantity;
                                    oi.DiscountAmount = totalItemDiscount;
                                    oi.FinalPrice = Math.Max(0, oi.OriginalPrice - perUnitDiscount);
                                    oi.AppliedPromotionId = promo.PromotionId;
                                    totalDiscount += totalItemDiscount;
                                }
                            }
                            else if (promo.Type == "FIXED_AMOUNT")
                            {
                                var fixedAmount = Math.Max(0, promo.Value);
                                var appliedTotal = Math.Min(fixedAmount, subtotal);
                                foreach (var oi in orderItems)
                                {
                                    var itemSubtotal = oi.OriginalPrice * oi.Quantity;
                                    var share = itemSubtotal / subtotal;
                                    var itemDiscountTotal = Math.Round(appliedTotal * share, 2);
                                    var perUnitDiscount = Math.Round(itemDiscountTotal / oi.Quantity, 2);
                                    oi.DiscountAmount = itemDiscountTotal;
                                    oi.FinalPrice = Math.Max(0, oi.OriginalPrice - perUnitDiscount);
                                    oi.AppliedPromotionId = promo.PromotionId;
                                    totalDiscount += itemDiscountTotal;
                                }
                            }

                            totalAmount = Math.Max(0, subtotal - totalDiscount);
                            payment.Amount = totalAmount;

                            promo.CurrentUsage += 1;
                        }
                    }
                }

                foreach (var oi in orderItems)
                {
                    _context.OrderItems.Add(oi);
                }

                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Замовлення {OrderNumber} створено користувачем {UserId}", order.OrderNumber, userId);

                return Ok(new
                {
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    status = order.Status,
                    totalAmount = order.TotalAmount,
                    discountTotal = totalDiscount,
                    createdAt = order.CreatedAt
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка під час оформлення замовлення користувача {UserId}", userId);
                return StatusCode(500, new { error = "Не вдалося оформити замовлення" });
            }
        }

        [HttpGet("my")]
        public async Task<ActionResult> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders.Select(o => new
            {
                orderId = o.OrderId,
                orderNumber = o.OrderNumber,
                status = o.Status,
                totalAmount = o.TotalAmount,
                createdAt = o.CreatedAt,
                shipping = o.Shipping != null ? new
                {
                    city = o.Shipping.City,
                    warehouseNumber = o.Shipping.WarehouseNumber,
                    recipientName = o.Shipping.RecipientName,
                    recipientPhone = o.Shipping.RecipientPhone
                } : null,
                items = o.OrderItems.Select(oi => new
                {
                    productId = oi.ProductId,
                    name = oi.Product.Name,
                    quantity = oi.Quantity,
                    price = oi.FinalPrice
                })
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrderById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { error = "Замовлення не знайдено" });
            }

            return Ok(new
            {
                orderId = order.OrderId,
                orderNumber = order.OrderNumber,
                status = order.Status,
                totalAmount = order.TotalAmount,
                createdAt = order.CreatedAt,
                shipping = order.Shipping != null ? new
                {
                    city = order.Shipping.City,
                    warehouseNumber = order.Shipping.WarehouseNumber,
                    recipientName = order.Shipping.RecipientName,
                    recipientPhone = order.Shipping.RecipientPhone,
                    trackingNumber = order.Shipping.TrackingNumber
                } : null,
                payment = order.Payment != null ? new
                {
                    method = order.Payment.Method,
                    status = order.Payment.Status,
                    amount = order.Payment.Amount,
                    transactionId = order.Payment.TransactionId
                } : null,
                items = order.OrderItems.Select(oi => new
                {
                    productId = oi.ProductId,
                    name = oi.Product.Name,
                    quantity = oi.Quantity,
                    price = oi.FinalPrice
                })
            });
        }
    }

    public class CheckoutDto
    {
        public string City { get; set; } = string.Empty;
        public string WarehouseNumber { get; set; } = string.Empty;
        public string? CityRef { get; set; }
        public string? WarehouseRef { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PromoCode { get; set; }
    }
}
