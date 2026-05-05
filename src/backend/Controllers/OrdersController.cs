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
        private readonly KhduSouvenirShop.API.Services.INovaPoshtaService _novaPoshtaService;
        private readonly KhduSouvenirShop.API.Services.IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public OrdersController(
            AppDbContext context, 
            ILogger<OrdersController> logger, 
            KhduSouvenirShop.API.Services.PromotionService promotionService,
            KhduSouvenirShop.API.Services.IPaymentService paymentService,
            KhduSouvenirShop.API.Services.INovaPoshtaService novaPoshtaService,
            KhduSouvenirShop.API.Services.IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _promotionService = promotionService;
            _paymentService = paymentService;
            _novaPoshtaService = novaPoshtaService;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            string? paymentUrl = null;
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
            var userPromotions = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

            // --- НОВА ЛОГІКА ДЛЯ STRIPE (Етап 5 - Виправлення) ---
            if (dto.PaymentMethod == "Card")
            {
                // Валідація залишків перед переходом до оплати
                foreach (var item in cart.CartItems)
                {
                    if (item.Product.Stock < item.Quantity)
                    {
                        return BadRequest(ApiResponse<object>.FailureResult($"Товар '{item.Product.Name}' недостатньо на складі", "InsufficientStock"));
                    }
                }

                // Розрахунок вартості доставки
                var totalAfterUserPromos = cart.CartItems.Sum(ci => _promotionService.GetPriceAfterPromotions(ci.Product, userPromotions) * ci.Quantity);
                var totalWeight = cart.CartItems.Sum(ci => ci.Product.Weight * ci.Quantity);
                decimal shippingCost = 0;
                if (!string.IsNullOrEmpty(dto.CityRef) && _configuration.GetValue<bool>("Features:NovaPoshtaEnabled"))
                {
                    shippingCost = await _novaPoshtaService.CalculateDeliveryCostAsync(dto.CityRef, totalWeight, totalAfterUserPromos);
                }

                var successUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:3000/checkout/success";
                var cancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:3000/checkout/cancel";

                // Формуємо метадані для Stripe, щоб створити замовлення ПІСЛЯ оплати
                var metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "city", dto.City ?? "" },
                    { "cityRef", dto.CityRef ?? "" },
                    { "warehouseNumber", dto.WarehouseNumber ?? "" },
                    { "warehouseRef", dto.WarehouseRef ?? "" },
                    { "recipientName", dto.RecipientName ?? "" },
                    { "recipientPhone", dto.RecipientPhone ?? "" },
                    { "promoCode", dto.PromoCode ?? "" },
                    { "shippingCost", shippingCost.ToString("F2") }
                };

                var session = await _paymentService.CreateCheckoutSessionForCartAsync(user!, cart, userPromotions, dto.PromoCode, successUrl, cancelUrl, metadata);
                
                return Ok(ApiResponse<object>.SuccessResult(new { paymentUrl = session.Url }, "Перенаправлення на оплату"));
            }

            // --- ЛОГІКА ДЛЯ НАКЛАДЕНОГО ПЛАТЕЖУ (Залишається як була) ---
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
                var originalSubtotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);
                var totalAfterUserPromosForShipping = cart.CartItems.Sum(ci => _promotionService.GetPriceAfterPromotions(ci.Product, userPromotions) * ci.Quantity);
                var totalWeight = cart.CartItems.Sum(ci => ci.Product.Weight * ci.Quantity);
                
                decimal shippingCost = 0;
                if (!string.IsNullOrEmpty(dto.CityRef) && _configuration.GetValue<bool>("Features:NovaPoshtaEnabled"))
                {
                    shippingCost = await _novaPoshtaService.CalculateDeliveryCostAsync(dto.CityRef, totalWeight, totalAfterUserPromosForShipping);
                }

                var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = "Processing",
                    TotalAmount = 0,
                    ShippingCost = shippingCost,
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
                    Amount = 0,
                    Method = dto.PaymentMethod ?? "CashOnDelivery",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                var orderItems = new List<OrderItem>();
                decimal totalAfterUserPromos = 0;
                foreach (var item in cart.CartItems)
                {
                    item.Product.Stock -= item.Quantity;

                    var priceAfterUserPromos = _promotionService.GetPriceAfterPromotions(item.Product, userPromotions);
                    var userDiscountPerUnit = item.Product.Price - priceAfterUserPromos;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OriginalPrice = item.Product.Price,
                        DiscountAmount = userDiscountPerUnit * item.Quantity,
                        FinalPrice = priceAfterUserPromos
                    };

                    orderItems.Add(orderItem);
                    totalAfterUserPromos += priceAfterUserPromos * item.Quantity;
                }

                decimal totalDiscount = originalSubtotal - totalAfterUserPromos;
                decimal finalTotal = totalAfterUserPromos;

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
                            foreach (var oi in orderItems)
                            {
                                var perUnitDiscount = Math.Round(oi.FinalPrice * (decimal)(percent / 100.0), 2);
                                var totalItemDiscount = perUnitDiscount * oi.Quantity;
                                oi.DiscountAmount += totalItemDiscount;
                                oi.FinalPrice = Math.Max(0, oi.FinalPrice - perUnitDiscount);
                                oi.AppliedPromotionId = promo.PromotionId;
                                totalDiscount += totalItemDiscount;
                            }
                        }
                        else if (promo.Type == "FIXED_AMOUNT")
                        {
                            var fixedAmount = Math.Max(0, promo.Value);
                            var appliedTotal = Math.Min(fixedAmount, finalTotal);
                            foreach (var oi in orderItems)
                            {
                                var itemSubtotal = oi.FinalPrice * oi.Quantity;
                                var share = itemSubtotal / finalTotal;
                                var itemDiscountTotal = Math.Round(appliedTotal * share, 2);
                                var perUnitDiscount = Math.Round(itemDiscountTotal / oi.Quantity, 2);
                                oi.DiscountAmount += itemDiscountTotal;
                                oi.FinalPrice = Math.Max(0, oi.FinalPrice - perUnitDiscount);
                                oi.AppliedPromotionId = promo.PromotionId;
                                totalDiscount += itemDiscountTotal;
                            }
                        }

                        finalTotal = Math.Max(0, finalTotal - (totalDiscount - (originalSubtotal - totalAfterUserPromos)));
                        promo.CurrentUsage += 1;
                    }
                }

                finalTotal = Math.Max(0, orderItems.Sum(oi => oi.FinalPrice * oi.Quantity));
                order.TotalAmount = finalTotal + shippingCost;
                payment.Amount = order.TotalAmount;

                foreach (var oi in orderItems)
                {
                    _context.OrderItems.Add(oi);

                    var outgoingDoc = new OutgoingDocument
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        OrderId = order.OrderId,
                        Reason = "ORDER",
                        OriginalPrice = oi.OriginalPrice,
                        AppliedPromotionId = oi.AppliedPromotionId,
                        DiscountAmount = oi.DiscountAmount,
                        FinalPrice = oi.FinalPrice,
                        DocumentDate = DateTime.UtcNow,
                        CreatedByUserId = userId,
                        Notes = $"Автоматично створено для замовлення {order.OrderNumber}"
                    };
                    _context.OutgoingDocuments.Add(outgoingDoc);
                }

                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                if (payment.Method == "Card")
                {
                    // Ця частина більше не повинна виконуватися тут для "Card" за новою логікою,
                    // але я залишу її для сумісності, якщо метод оплати зміниться в процесі.
                }

                await transaction.CommitAsync();

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
        public async Task<ActionResult> Calculate([FromBody] CalculateDto dto)
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
                weight = ci.Product.Weight,
                originalPrice = ci.Product.Price,
                priceAfterUserPromos = _promotionService.GetPriceAfterPromotions(ci.Product, promos),
                finalPrice = _promotionService.GetPriceAfterPromotions(ci.Product, promos) // Початково до промокоду
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
                        
                        // Оновлюємо ціну кожного товару
                        items = items.Select(i => new {
                            i.productId,
                            i.name,
                            i.quantity,
                            i.weight,
                            i.originalPrice,
                            i.priceAfterUserPromos,
                            finalPrice = Math.Round(i.finalPrice * (decimal)(1 - percent / 100.0), 2)
                        }).ToList();

                        totalAmount = items.Sum(i => i.finalPrice * i.quantity);
                        totalDiscount = subtotal - totalAmount;
                    }
                    else if (promo.Type == "FIXED_AMOUNT")
                    {
                        var fixedAmount = Math.Max(0, promo.Value);
                        var appliedTotal = Math.Min(fixedAmount, totalAmount);
                        
                        // Розподіляємо фіксовану знижку пропорційно
                        items = items.Select(i => {
                            var itemSubtotal = i.finalPrice * i.quantity;
                            var share = totalAmount > 0 ? itemSubtotal / totalAmount : 0;
                            var itemDiscountTotal = Math.Round(appliedTotal * share, 2);
                            var perUnitDiscount = Math.Round(itemDiscountTotal / i.quantity, 2);
                            return new {
                                i.productId,
                                i.name,
                                i.quantity,
                                i.weight,
                                i.originalPrice,
                                i.priceAfterUserPromos,
                                finalPrice = Math.Max(0, i.finalPrice - perUnitDiscount)
                            };
                        }).ToList();

                        totalAmount = items.Sum(i => i.finalPrice * i.quantity);
                        totalDiscount = subtotal - totalAmount;
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

        [HttpGet("admin")]
        [Authorize(Roles = "Administrator,Manager")] // Змінено Admin → Administrator
        public async Task<ActionResult> GetAllOrders([FromQuery] string? status)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            
            var result = orders.Select(o => new {
                o.OrderId,
                o.OrderNumber,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                userName = $"{o.User.FirstName} {o.User.LastName}",
                userEmail = o.User.Email
            });

            return Ok(ApiResponse<object>.SuccessResult(result));
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
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var adminUserId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            // Якщо змінюємо статус на Cancelled — використовуємо спеціальну логіку з поверненням товару
            if (dto.Status == "Cancelled")
            {
                var cancelResult = await _paymentService.RefundPaymentAsync(id, dto.Comment);
                if (!cancelResult)
                {
                    cancelResult = await _paymentService.CancelOrderAndRestoreStock(id, dto.Comment ?? "Скасовано адміністратором");
                }
                if (!cancelResult)
                {
                    return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося скасувати замовлення", "InternalServerError"));
                }
                return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення скасовано"));
            }

            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }

            var oldStatus = order.Status;
            order.Status = dto.Status;

            // Логіка для COD (Накладений платіж)
            if (order.Status == "Delivered" && order.Payment != null && order.Payment.Method == "CashOnDelivery")
            {
                order.Payment.Status = "Completed";
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
                ChangedByUserId = adminUserId,
                Comment = dto.Comment ?? $"Статус змінено адміністратором. { (dto.TrackingNumber != null ? "ТТН: " + dto.TrackingNumber : "") }",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { orderId = order.OrderId, status = order.Status }, "Статус замовлення оновлено"));
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> CancelOrder(int id, [FromBody] string? reason)
        {
            // Спочатку перевіримо, чи замовлення взагалі існує
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            if (order.Status == "Cancelled") return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення вже скасоване"));

            // Спочатку спробуємо зробити refund через Stripe, якщо оплата Completed
            var refundResult = await _paymentService.RefundPaymentAsync(id, reason);
            if (refundResult)
            {
                return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення скасовано, кошти повернуто"));
            }
            
            // Інакше — просто скасовуємо замовлення та повертаємо товар (використовуємо готовий метод з PaymentService)
            var result = await _paymentService.CancelOrderAndRestoreStock(id, reason ?? "Скасовано адміністратором");
            if (!result)
            {
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося скасувати замовлення", "InternalServerError"));
            }

            return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення скасовано"));
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

    public class CalculateDto
    {
        public string? PromoCode { get; set; }
        public string? CityRef { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? Comment { get; set; }
    }
}
