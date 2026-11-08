using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartRepository _cartRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShippingRepository _shippingRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<OrderHistory> _orderHistoryRepository;
        private readonly IRepository<UserPromotion> _userPromotionRepository;
        private readonly ILogger<OrdersController> _logger;
        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService;
        private readonly KhduSouvenirShop.API.Services.IPaymentService _paymentService;
        private readonly KhduSouvenirShop.API.Services.INovaPoshtaService _novaPoshtaService;
        private readonly KhduSouvenirShop.API.Services.IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        public OrdersController(
            IUnitOfWork unitOfWork,
            ICartRepository cartRepository,
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            IShippingRepository shippingRepository,
            IPaymentRepository paymentRepository,
            IPromotionRepository promotionRepository,
            IWarehouseRepository warehouseRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<CartItem> cartItemRepository,
            IRepository<OrderHistory> orderHistoryRepository,
            IRepository<UserPromotion> userPromotionRepository,
            ILogger<OrdersController> logger, 
            KhduSouvenirShop.API.Services.PromotionService promotionService,
            KhduSouvenirShop.API.Services.IPaymentService paymentService,
            KhduSouvenirShop.API.Services.INovaPoshtaService novaPoshtaService,
            KhduSouvenirShop.API.Services.IEmailService emailService,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cartRepository = cartRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _shippingRepository = shippingRepository;
            _paymentRepository = paymentRepository;
            _promotionRepository = promotionRepository;
            _warehouseRepository = warehouseRepository;
            _orderItemRepository = orderItemRepository;
            _cartItemRepository = cartItemRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _userPromotionRepository = userPromotionRepository;
            _logger = logger;
            _promotionService = promotionService;
            _paymentService = paymentService;
            _novaPoshtaService = novaPoshtaService;
            _emailService = emailService;
            _configuration = configuration;
            _cache = cache;
        }
        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }
        // Оформлення замовлення (POST: api/Orders)
        [HttpPost]
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
            var cart = await _cartRepository.Query()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кошик порожній", "BadRequest"));
            }
            var user = await _userRepository.GetByIdAsync(userId);
            string studentStatus = user?.StudentStatus ?? "NONE";
            var userPromotions = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
            // Крок 1. Перевірка наявності товару на складі
            foreach (var item in cart.CartItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару '{item.Product.Name}' на складі. Доступно: {item.Product.Stock}", "BadRequest"));
                }
            }
            // Крок 1. Перевірка промокоду до початку транзакції (6 валідацій у PromotionService)
            decimal originalSubtotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);
            Promotion? promoCodePromo = null;
            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                var validation = await _promotionService.ValidatePromoCodeAsync(dto.PromoCode, userId, cart.CartItems.ToList());
                if (!validation.Valid)
                {
                    return BadRequest(ApiResponse<object>.FailureResult(validation.Error ?? "Промокод недійсний", "BadRequest"));
                }
                promoCodePromo = validation.Promotion;
            }
            // Крок 1. Перевірка існування відділення Nova Poshta (м'яка деградація: за помилок API перевірку пропускаємо)
            // Адреса відділення береться з наданого DTO, а якщо її не передано — з довідника Nova Poshta.
            string? warehouseAddress = dto.WarehouseAddress;
            if (_configuration.GetValue<bool>("Features:NovaPoshtaEnabled")
                && !string.IsNullOrWhiteSpace(dto.CityRef)
                && !string.IsNullOrWhiteSpace(dto.WarehouseRef))
            {
                try
                {
                    var warehouses = await _novaPoshtaService.GetWarehousesAsync(dto.CityRef);
                    if (warehouses != null)
                    {
                        var warehouseList = warehouses.ToList();
                        if (warehouseList.Count > 0 && !warehouseList.Any(w => w.Ref == dto.WarehouseRef))
                        {
                            return BadRequest(ApiResponse<object>.FailureResult("Вказане відділення не знайдено для обраного міста", "BadRequest"));
                        }
                        if (string.IsNullOrWhiteSpace(warehouseAddress))
                        {
                            warehouseAddress = warehouseList.FirstOrDefault(w => w.Ref == dto.WarehouseRef)?.Description;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не вдалося перевірити відділення Nova Poshta для CityRef={CityRef}; перевірку пропущено", dto.CityRef);
                }
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var totalAfterUserPromosForShipping = cart.CartItems.Sum(ci => _promotionService.GetPriceAfterPromotions(ci.Product, userPromotions) * ci.Quantity);
                var totalWeight = cart.CartItems.Sum(ci => ci.Product.Weight * ci.Quantity);
                decimal shippingCost = 0;
                if (!string.IsNullOrEmpty(dto.CityRef) && _configuration.GetValue<bool>("Features:NovaPoshtaEnabled"))
                {
                    shippingCost = await _novaPoshtaService.CalculateDeliveryCostAsync(dto.CityRef, dto.WarehouseRef, totalWeight, totalAfterUserPromosForShipping);
                }
                // Валідація: вартість доставки не може бути від'ємною
                if (shippingCost < 0)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Некоректна вартість доставки", "BadRequest"));
                }
                // Крок 5. Номер замовлення у форматі KSU-{yyyyMMdd}-{000000}
                var orderNumberPrefix = $"KSU-{DateTime.UtcNow:yyyyMMdd}-";
                var todayOrderCount = await _orderRepository.Query()
                    .CountAsync(o => o.OrderNumber.StartsWith(orderNumberPrefix));
                var orderNumber = $"{orderNumberPrefix}{(todayOrderCount + 1):D6}";
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = "Processing",
                    TotalAmount = 0,
                    ShippingCost = shippingCost,
                    CreatedAt = DateTime.UtcNow
                };
                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                // Крок 8. Запис історії створення замовлення
                await _orderHistoryRepository.AddAsync(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = null,
                    NewStatus = "Processing",
                    ChangedByUserId = userId,
                    Comment = "Замовлення створено",
                    Timestamp = DateTime.UtcNow
                });
                var shipping = new Shipping
                {
                    OrderId = order.OrderId,
                    City = dto.City,
                    WarehouseNumber = dto.WarehouseNumber,
                    WarehouseAddress = warehouseAddress,
                    CityRef = dto.CityRef,
                    WarehouseRef = dto.WarehouseRef,
                    RecipientName = dto.RecipientName,
                    RecipientPhone = dto.RecipientPhone,
                    TrackingNumber = null,
                    DeliveryMethod = "NovaPoshta",
                    DeliveryCost = shippingCost,
                    EstimatedDays = _novaPoshtaService.EstimateDeliveryDays(totalWeight)
                };
                await _shippingRepository.AddAsync(shipping);
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = 0,
                    Method = dto.PaymentMethod ?? PaymentMethod.CashOnDelivery.ToString(),
                    Status = PaymentStatus.Pending.ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                await _paymentRepository.AddAsync(payment);
                var orderItems = new List<OrderItem>();
                decimal totalAfterUserPromos = 0;
                foreach (var item in cart.CartItems)
                {
                    item.Product.Stock -= item.Quantity;
                    var (priceAfterUserPromos, appliedPromotionId) = _promotionService.GetPriceAfterPromotionsWithPromotion(item.Product, userPromotions);
                    var userDiscountPerUnit = item.Product.Price - priceAfterUserPromos;
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OriginalPrice = item.Product.Price,
                        AppliedPromotionId = appliedPromotionId,
                        DiscountAmount = userDiscountPerUnit * item.Quantity,
                        FinalPrice = priceAfterUserPromos
                    };
                    orderItems.Add(orderItem);
                    totalAfterUserPromos += priceAfterUserPromos * item.Quantity;
                }
                decimal totalDiscount = originalSubtotal - totalAfterUserPromos;
                decimal finalTotal = totalAfterUserPromos;
                if (promoCodePromo != null)
                {
                    if (promoCodePromo.Type == "PERCENTAGE")
                    {
                        var percent = Math.Clamp((double)promoCodePromo.Value, 0, 100);
                        foreach (var oi in orderItems)
                        {
                            var perUnitDiscount = Math.Round(oi.FinalPrice * (decimal)(percent / 100.0), 2);
                            var totalItemDiscount = perUnitDiscount * oi.Quantity;
                            oi.DiscountAmount += totalItemDiscount;
                            oi.FinalPrice = Math.Max(0, oi.FinalPrice - perUnitDiscount);
                            oi.AppliedPromotionId = promoCodePromo.PromotionId;
                            totalDiscount += totalItemDiscount;
                        }
                    }
                    else if (promoCodePromo.Type == "FIXED_AMOUNT")
                    {
                        var fixedAmount = Math.Max(0, promoCodePromo.Value);
                        var appliedTotal = Math.Min(fixedAmount, finalTotal);
                        foreach (var oi in orderItems)
                        {
                            var itemSubtotal = oi.FinalPrice * oi.Quantity;
                            var share = finalTotal > 0 ? itemSubtotal / finalTotal : 0;
                            var itemDiscountTotal = Math.Round(appliedTotal * share, 2);
                            var perUnitDiscount = Math.Round(itemDiscountTotal / oi.Quantity, 2);
                            oi.DiscountAmount += itemDiscountTotal;
                            oi.FinalPrice = Math.Max(0, oi.FinalPrice - perUnitDiscount);
                            oi.AppliedPromotionId = promoCodePromo.PromotionId;
                            totalDiscount += itemDiscountTotal;
                        }
                    }
                    finalTotal = Math.Max(0, finalTotal - (totalDiscount - (originalSubtotal - totalAfterUserPromos)));
                    // Крок 11. Збільшення лічильників використання промокоду
                    promoCodePromo.CurrentUsage += 1;
                    var userPromotion = await _userPromotionRepository.Query()
                        .FirstOrDefaultAsync(up => up.UserId == userId && up.PromotionId == promoCodePromo.PromotionId);
                    if (userPromotion != null)
                    {
                        userPromotion.UsedCount += 1;
                    }
                }
                finalTotal = Math.Max(0, orderItems.Sum(oi => oi.FinalPrice * oi.Quantity));
                order.TotalAmount = finalTotal + shippingCost;
                payment.Amount = order.TotalAmount;
                foreach (var oi in orderItems)
                {
                    await _orderItemRepository.AddAsync(oi);
                    var outgoingDoc = new OutgoingDocument
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        OrderId = order.OrderId,
                        Reason = "Order",
                        OriginalPrice = oi.OriginalPrice,
                        AppliedPromotionId = oi.AppliedPromotionId,
                        DiscountAmount = oi.DiscountAmount,
                        FinalPrice = oi.FinalPrice,
                        DocumentDate = DateTime.UtcNow,
                        CreatedBy = userId,
                        Notes = $"Автоматично створено для замовлення {order.OrderNumber}"
                    };
                    await _warehouseRepository.AddAsync(outgoingDoc);
                }
                _cartItemRepository.RemoveRange(cart.CartItems);
                await _unitOfWork.SaveChangesAsync();
                InvalidateCache();
                // Відправка листа-підтвердження для накладеного платежу
                if (PaymentStates.IsCashOnDelivery(payment.Method))
                {
                    await _emailService.SendOrderConfirmationAsync(user!.Email, order.OrderNumber, "ua");
                }
                if (payment.Method == "Card")
                {
                    var successUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:3000/checkout/success";
                    var cancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:3000/checkout/cancel";
                    var session = await _paymentService.CreateCheckoutSessionAsync(order, successUrl, cancelUrl);
                    paymentUrl = session.Url;
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
            var cart = await _cartRepository.Query()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кошик порожній", "BadRequest"));
            }
            var user = await _userRepository.GetByIdAsync(userId);
            string studentStatus = user?.StudentStatus ?? "NONE";
            var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
            var items = cart.CartItems.Select(ci => new
            {
                productId = ci.ProductId,
                name = ci.Product.Name,
                nameEn = ci.Product.NameEn,
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
            string? promoCodeError = null;
            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                var validation = await _promotionService.ValidatePromoCodeAsync(dto.PromoCode, userId, cart.CartItems.ToList());
                if (!validation.Valid)
                {
                    promoCodeError = validation.Error;
                }
                else
                {
                    var promo = validation.Promotion!;
                    if (promo.Type == "PERCENTAGE")
                    {
                        var percent = Math.Clamp((double)promo.Value, 0, 100);
                        // Оновлення ціни кожного товару
                        items = items.Select(i => new {
                            i.productId,
                            i.name,
                            i.nameEn,
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
                        // Розподіл фіксованої знижки пропорційно
                        items = items.Select(i => {
                            var itemSubtotal = i.finalPrice * i.quantity;
                            var share = totalAmount > 0 ? itemSubtotal / totalAmount : 0;
                            var itemDiscountTotal = Math.Round(appliedTotal * share, 2);
                            var perUnitDiscount = Math.Round(itemDiscountTotal / i.quantity, 2);
                            return new {
                                i.productId,
                                i.name,
                                i.nameEn,
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
                promoCodeError = promoCodeError,
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
            var orders = await _orderRepository.Query()
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
                    warehouseAddress = o.Shipping.WarehouseAddress,
                    recipientName = o.Shipping.RecipientName,
                    recipientPhone = o.Shipping.RecipientPhone,
                    deliveryMethod = o.Shipping.DeliveryMethod,
                    deliveryCost = o.Shipping.DeliveryCost,
                    estimatedDays = o.Shipping.EstimatedDays
                } : null,
                items = o.OrderItems.Select(oi => new
                {
                    productId = oi.ProductId,
                    name = oi.Product.Name,
                    nameEn = oi.Product.NameEn,
                    quantity = oi.Quantity,
                    price = oi.FinalPrice
                })
            });
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(result));
        }
        [HttpGet("verify-payment")]
        public async Task<ActionResult> VerifyPayment([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<object>.FailureResult("Session ID is required"));

            try
            {
                var result = await _paymentService.HandleWebhookAsync(null, sessionId); // We'll modify HandleWebhookAsync to handle manual verify
                if (result)
                {
                    var payment = await _paymentRepository.Query()
                        .Include(p => p.Order)
                        .FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
                    if (payment?.Order != null)
                    {
                        return Ok(ApiResponse<object>.SuccessResult(new
                        {
                            orderId = payment.Order.OrderId,
                            orderNumber = payment.Order.OrderNumber
                        }, "Payment verified"));
                    }
                    return Ok(ApiResponse<object>.SuccessResult(null, "Payment verified"));
                }
                else
                    return BadRequest(ApiResponse<object>.FailureResult("Payment verification failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
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
            var order = await _orderRepository.Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }
            // Перевірка прав: або власник замовлення, або адмін/менеджер
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isPrivileged = userRole == "Administrator" || userRole == "Manager";
            if (order.UserId != userId && !isPrivileged)
            {
                return Forbid();
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
                    warehouseAddress = order.Shipping.WarehouseAddress,
                    recipientName = order.Shipping.RecipientName,
                    recipientPhone = order.Shipping.RecipientPhone,
                    trackingNumber = order.Shipping.TrackingNumber,
                    deliveryMethod = order.Shipping.DeliveryMethod,
                    deliveryCost = order.Shipping.DeliveryCost,
                    estimatedDays = order.Shipping.EstimatedDays
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
                    nameEn = oi.Product.NameEn,
                    quantity = oi.Quantity,
                    price = oi.FinalPrice
                })
            };
            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        // Скасування замовлення (POST: api/Orders/{id}/cancel)
        // Доступ: Customer - лише власне замовлення у статусі Processing;
        //         Manager/Administrator/SuperAdmin - будь-яке замовлення до статусу Delivered.
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CancelOrder(int id, [FromBody] CancelOrderDto? dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Administrator" || userRole == "Manager" || userRole == "SuperAdmin";
            var order = await _orderRepository.Query()
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }
            // Перевірка прав доступу: адміністратор або власник замовлення
            bool isOwner = order.UserId == userId;
            if (!isAdmin && !isOwner)
            {
                return Forbid();
            }
            // Крок 1. Перевірка поточного статусу
            if (order.Status == "Cancelled")
            {
                return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення вже скасоване"));
            }
            // Скасування дозволене лише з не термінальних статусів (Processing/Paid/Shipped)
            if (!OrderStateMachine.CanTransition(order.Status, OrderStatus.Cancelled.ToString()))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Доставлене замовлення не можна скасувати", "BadRequest"));
            }
            // Customer може скасувати лише власне замовлення у статусі Processing
            if (!isAdmin && order.Status != "Processing")
            {
                return BadRequest(ApiResponse<object>.FailureResult("Скасувати замовлення можна лише у статусі 'Processing'. Зверніться до менеджера", "BadRequest"));
            }
            var cancelReason = string.IsNullOrWhiteSpace(dto?.Reason)
                ? (isAdmin ? "Скасовано адміністратором" : "Скасовано клієнтом")
                : dto!.Reason!;
            // Крок 2. Якщо оплата завершена (Completed) - спроба повернути кошти через Stripe,
            // інакше (або якщо refund недоступний) - скасування з поверненням товару на склад
            bool cancelled;
            if (order.Payment != null && order.Payment.Status == "Completed")
            {
                cancelled = await _paymentService.RefundPaymentAsync(id, cancelReason);
                if (!cancelled)
                {
                    cancelled = await _paymentService.CancelOrderAndRestoreStock(id, cancelReason);
                }
            }
            else
            {
                cancelled = await _paymentService.CancelOrderAndRestoreStock(id, cancelReason);
            }
            if (!cancelled)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.FailureResult("Не вдалося скасувати замовлення", "InternalServerError"));
            }
            // Крок 8. Відправка листа про скасування замовлення
            var user = await _userRepository.GetByIdAsync(order.UserId);
            if (user != null)
            {
                await _emailService.SendOrderCancellationAsync(user.Email, order.OrderNumber, cancelReason, "ua");
            }
            InvalidateCache();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення скасовано"));
        }
    }
    public class CheckoutDto
    {
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string WarehouseNumber { get; set; } = string.Empty;
        public string? WarehouseAddress { get; set; }
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
    public class CancelOrderDto
    {
        public string? Reason { get; set; }
    }
}