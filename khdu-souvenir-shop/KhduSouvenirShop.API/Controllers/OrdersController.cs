using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService;
        private readonly KhduSouvenirShop.API.Services.IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public OrdersController(
            AppDbContext context, 
            ILogger<OrdersController> logger, 
            KhduSouvenirShop.API.Services.PromotionService promotionService,
            KhduSouvenirShop.API.Services.IPaymentService paymentService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _promotionService = promotionService;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кошик порожній", "BadRequest"));
            }

            foreach (var item in cart.CartItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару '{item.Product.Name}' на складі. Доступно: {item.Product.Stock}", "BadRequest"));
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

                string? paymentUrl = null;
                if (payment.Method == "Card")
                {
                    var successUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:3000/checkout/success";
                    var cancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:3000/checkout/cancel";
                    
                    var session = await _paymentService.CreateCheckoutSessionAsync(order, successUrl, cancelUrl);
                    paymentUrl = session.Url;
                }

                var result = new
                {
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    status = order.Status,
                    totalAmount = order.TotalAmount,
                    discountTotal = totalDiscount,
                    createdAt = order.CreatedAt,
                    paymentUrl = paymentUrl
                };

                return Ok(ApiResponse<object>.SuccessResult(result, "Замовлення оформлено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка під час оформлення замовлення користувача {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.FailureResult("Не вдалося оформити замовлення", "InternalServerError"));
            }
        }

        [HttpPost("calculate")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Calculate([FromBody] CheckoutDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кошик порожній", "BadRequest"));
            }

            var user = await _context.Users.FindAsync(userId);
            string studentStatus = user?.StudentStatus ?? "NONE";
            var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

            var items = cart.CartItems.Select(ci => new
            {
                productId = ci.ProductId,
                name = ci.Product.Name,
                quantity = ci.Quantity,
                originalPrice = ci.Product.Price,
                priceAfterUserPromos = _promotionService.GetPriceAfterPromotions(ci.Product, promos)
            }).ToList();

            decimal subtotal = items.Sum(i => i.originalPrice * i.quantity);
            decimal totalAfterUserPromos = items.Sum(i => i.priceAfterUserPromos * i.quantity);

            decimal totalDiscount = subtotal - totalAfterUserPromos;
            decimal totalAmount = totalAfterUserPromos;

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
                    if (promo.Type == "PERCENTAGE")
                    {
                        var percent = Math.Clamp((double)promo.Value, 0, 100);
                        var promoDiscount = Math.Round(totalAmount * (decimal)(percent / 100.0), 2);
                        totalDiscount += promoDiscount;
                        totalAmount -= promoDiscount;
                    }
                    else if (promo.Type == "FIXED_AMOUNT")
                    {
                        var promoDiscount = Math.Min(promo.Value, totalAmount);
                        totalDiscount += promoDiscount;
                        totalAmount -= promoDiscount;
                    }
                }
            }

            var result = new
            {
                subtotal = subtotal,
                totalDiscount = totalDiscount,
                totalAmount = totalAmount,
                items = items
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        [HttpGet("my")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => new
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
            });

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(result));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetOrderById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }

            var result = new
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
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        // --- Admin Methods ---

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }

            var oldStatus = order.Status;
            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // Логіка для COD (Накладений платіж)
            if (order.Status == "Delivered" && order.Payment != null && order.Payment.Method == "CashOnDelivery")
            {
                order.Payment.Status = "Completed";
                order.Payment.UpdatedAt = DateTime.UtcNow;
            }

            if (dto.Status == "Shipped" && !string.IsNullOrEmpty(dto.TrackingNumber))
            {
                var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.OrderId == id);
                if (shipping != null)
                {
                    shipping.TrackingNumber = dto.TrackingNumber;
                }
            }

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                Comment = dto.Comment ?? $"Статус змінено адміністратором. { (dto.TrackingNumber != null ? "ТТН: " + dto.TrackingNumber : "") }",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { orderId = order.OrderId, status = order.Status }, "Статус замовлення оновлено"));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> CancelOrder(int id, [FromBody] string? reason)
        {
            var result = await _paymentService.RefundPaymentAsync(id, reason);
            
            // Якщо це не Stripe платіж, або Stripe повернув false, пробуємо просто скасувати (наприклад для COD)
            if (!result)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null) return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
                if (order.Status == "Cancelled") return BadRequest(ApiResponse<object>.FailureResult("Замовлення вже скасоване", "BadRequest"));

                var oldStatus = order.Status;
                
                // Повернення на склад
                foreach (var item in order.OrderItems)
                {
                    item.Product.Stock += item.Quantity;
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;
                if (order.Payment != null && order.Payment.Status != "Completed")
                {
                    order.Payment.Status = "Failed";
                }

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    Comment = reason ?? "Скасовано адміністратором",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Замовлення скасовано"));
        }
    }

    public class CheckoutDto
    {
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string WarehouseNumber { get; set; } = string.Empty;
        public string? CityRef { get; set; }
        public string? WarehouseRef { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PromoCode { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? Comment { get; set; }
    }
}
