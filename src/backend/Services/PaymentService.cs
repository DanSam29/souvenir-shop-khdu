using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Stripe;
using Stripe.Checkout;

namespace KhduSouvenirShop.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly string _webhookSecret;

        public PaymentService(AppDbContext context, ILogger<PaymentService> logger, IConfiguration configuration, IEmailService emailService, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
        }

        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }

        public async Task<Session> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
        {
            var orderWithItems = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

            if (orderWithItems == null)
                throw new Exception("Замовлення не знайдено");

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.OrderId);
            var idempotencyKey = payment?.IdempotencyKey ?? Guid.NewGuid().ToString();

            if (payment != null && string.IsNullOrEmpty(payment.IdempotencyKey))
            {
                payment.IdempotencyKey = idempotencyKey;
                await _context.SaveChangesAsync();
            }

            var lineItems = orderWithItems.OrderItems.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmountDecimal = item.FinalPrice * 100, // Stripe expects amounts in cents
                    Currency = "uah",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Name,
                        Description = item.Product.Description,
                    },
                },
                Quantity = item.Quantity,
            }).ToList();

            // Додаємо вартість доставки як окремий line item
            if (orderWithItems.ShippingCost > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = orderWithItems.ShippingCost * 100,
                        Currency = "uah",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Доставка Nova Poshta",
                            Description = "Вартість доставки за замовлення",
                        },
                    },
                    Quantity = 1,
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = order.OrderId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "orderNumber", order.OrderNumber }
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, requestOptions);

            // Оновлення інформації про платіж
            if (payment != null)
            {
                payment.StripeSessionId = session.Id;
                payment.StripePaymentIntentId = session.PaymentIntentId;
                await _context.SaveChangesAsync();
            }

            return session;
        }

        public async Task<Session> CreateCheckoutSessionForCartAsync(User user, Cart cart, List<Promotion> promos, string? promoCode, string successUrl, string cancelUrl, Dictionary<string, string> metadata)
        {
            var promotionService = new PromotionService(_context);
            var lineItems = new List<SessionLineItemOptions>();
            decimal totalAmount = 0;

            foreach (var item in cart.CartItems)
            {
                var priceAfterUserPromos = promotionService.GetPriceAfterPromotions(item.Product, promos);
                
                // Якщо є промокод, застосовуємо його до ціни (спрощена логіка для Stripe)
                if (!string.IsNullOrEmpty(promoCode))
                {
                    var now = DateTime.UtcNow;
                    var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive);
                    if (promo != null)
                    {
                        if (promo.Type == "PERCENTAGE")
                        {
                            var percent = Math.Clamp((double)promo.Value, 0, 100);
                            priceAfterUserPromos = Math.Round(priceAfterUserPromos * (decimal)(1 - percent / 100.0), 2);
                        }
                        else if (promo.Type == "FIXED_AMOUNT")
                        {
                            // Для фіксованої суми на весь кошик - пропорційно розподіляємо (спрощено)
                            var cartTotal = cart.CartItems.Sum(ci => promotionService.GetPriceAfterPromotions(ci.Product, promos) * ci.Quantity);
                            if (cartTotal > 0)
                            {
                                var share = (priceAfterUserPromos * item.Quantity) / cartTotal;
                                var discountForItem = (promo.Value * share) / item.Quantity;
                                priceAfterUserPromos = Math.Max(0, priceAfterUserPromos - discountForItem);
                            }
                        }
                    }
                }

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = priceAfterUserPromos * 100,
                        Currency = "uah",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                        },
                    },
                    Quantity = item.Quantity,
                });
                totalAmount += priceAfterUserPromos * item.Quantity;
            }

            // Додаємо доставку, якщо вона є в метаданих
            if (metadata.TryGetValue("shippingCost", out var costStr) && decimal.TryParse(costStr, out var cost) && cost > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = cost * 100,
                        Currency = "uah",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Доставка Nova Poshta",
                        },
                    },
                    Quantity = 1,
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = $"CART_{user.UserId}",
                Metadata = metadata
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<bool> HandleWebhookAsync(string? json, string? stripeSignature, string? sessionId = null)
        {
            try
            {
                Session session;

                if (!string.IsNullOrEmpty(stripeSignature) && !string.IsNullOrEmpty(json))
                {
                    var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _webhookSecret);

                    if (stripeEvent.Type == "checkout.session.completed")
                    {
                        session = (stripeEvent.Data.Object as Session)!;
                        return await ProcessSuccessfulPayment(session);
                    }
                    else if (stripeEvent.Type == "payment_intent.payment_failed")
                    {
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        if (paymentIntent == null) return false;

                        return await ProcessFailedPayment(paymentIntent);
                    }
                    else if (stripeEvent.Type == "checkout.session.expired")
                    {
                        session = (stripeEvent.Data.Object as Session)!;
                        return await ProcessExpiredSession(session);
                    }
                }
                else if (!string.IsNullOrEmpty(sessionId))
                {
                    var service = new SessionService();
                    session = await service.GetAsync(sessionId);
                    
                    if (session.PaymentStatus == "paid")
                    {
                        return await ProcessSuccessfulPayment(session);
                    }
                }

                return true;
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe Webhook/Verify Error");
                return false;
            }
        }

        private async Task<bool> ProcessSuccessfulPayment(Session session)
        {
            var reference = session.ClientReferenceId;
            
            if (reference != null && reference.StartsWith("CART_"))
            {
                return await CreateOrderFromSuccessfulSession(session);
            }

            var orderIdStr = reference;
            if (!int.TryParse(orderIdStr, out var orderId)) return false;

            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.Payment == null) return false;

            // Ідемпотентність: якщо вже завершено, нічого не робимо
            if (order.Payment.Status == "Completed") return true;

            var oldStatus = order.Status;
            order.Payment.Status = "Completed";
            order.Payment.TransactionId = session.PaymentIntentId;
            order.Payment.StripePaymentIntentId = session.PaymentIntentId;

            order.Status = "Paid";

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Paid",
                Comment = "Оплата отримана через Stripe",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} successfully paid via Stripe", orderId);

            // Відправка листа про успішну оплату
            await _emailService.SendPaymentConfirmationAsync(order.User.Email, order.OrderNumber, order.Payment.Amount, "ua");

            return true;
        }

        private async Task<bool> CreateOrderFromSuccessfulSession(Session session)
        {
            var userIdStr = session.Metadata["userId"];
            if (!int.TryParse(userIdStr, out var userId)) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems.Count == 0)
            {
                _logger.LogWarning("Cart is empty for user {UserId} during webhook processing", userId);
                return true; // Вже опрацьовано або кошик очищено іншим шляхом
            }

            var promoCode = session.Metadata.TryGetValue("promoCode", out var pc) ? pc : null;
            decimal shippingCost = 0;
            if (session.Metadata.TryGetValue("shippingCost", out var scStr)) decimal.TryParse(scStr, out shippingCost);

            var promotionService = new PromotionService(_context);
            var userPromotions = await promotionService.GetActivePromotionsForUserAsync(user.StudentStatus);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = "Paid", // Оскільки це після успішної оплати
                    ShippingCost = shippingCost,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var shipping = new KhduSouvenirShop.API.Models.Shipping
                {
                    OrderId = order.OrderId,
                    City = session.Metadata["city"],
                    WarehouseNumber = session.Metadata["warehouseNumber"],
                    CityRef = session.Metadata["cityRef"],
                    WarehouseRef = session.Metadata["warehouseRef"],
                    RecipientName = session.Metadata["recipientName"],
                    RecipientPhone = session.Metadata["recipientPhone"]
                };
                _context.Shippings.Add(shipping);

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                    Method = "Card",
                    Status = "Completed",
                    TransactionId = session.PaymentIntentId,
                    StripeSessionId = session.Id,
                    StripePaymentIntentId = session.PaymentIntentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();
                foreach (var item in cart.CartItems)
                {
                    item.Product.Stock -= item.Quantity;
                    var priceAfterUserPromos = promotionService.GetPriceAfterPromotions(item.Product, userPromotions);
                    
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OriginalPrice = item.Product.Price,
                        DiscountAmount = (item.Product.Price - priceAfterUserPromos) * item.Quantity,
                        FinalPrice = priceAfterUserPromos
                    };
                    orderItems.Add(orderItem);
                    subtotal += priceAfterUserPromos * item.Quantity;
                }

                // Застосування промокоду, якщо він був
                if (!string.IsNullOrWhiteSpace(promoCode))
                {
                    var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive);
                    if (promo != null)
                    {
                        if (promo.Type == "PERCENTAGE")
                        {
                            var percent = Math.Clamp((double)promo.Value, 0, 100);
                            foreach (var oi in orderItems)
                            {
                                var disc = Math.Round(oi.FinalPrice * (decimal)(percent / 100.0), 2);
                                oi.DiscountAmount += disc * oi.Quantity;
                                oi.FinalPrice -= disc;
                                oi.AppliedPromotionId = promo.PromotionId;
                            }
                        }
                        else if (promo.Type == "FIXED_AMOUNT")
                        {
                            var appliedTotal = Math.Min(promo.Value, subtotal);
                            foreach (var oi in orderItems)
                            {
                                var share = (oi.FinalPrice * oi.Quantity) / subtotal;
                                var itemDisc = Math.Round(appliedTotal * share, 2);
                                oi.DiscountAmount += itemDisc;
                                oi.FinalPrice -= Math.Round(itemDisc / oi.Quantity, 2);
                                oi.AppliedPromotionId = promo.PromotionId;
                            }
                        }
                        promo.CurrentUsage += 1;
                    }
                }

                order.TotalAmount = orderItems.Sum(oi => oi.FinalPrice * oi.Quantity) + shippingCost;

                foreach (var oi in orderItems)
                {
                    _context.OrderItems.Add(oi);
                    _context.OutgoingDocuments.Add(new OutgoingDocument
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
                        Notes = $"Створено автоматично після оплати Stripe {order.OrderNumber}"
                    });
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    NewStatus = "Paid",
                    Comment = "Замовлення створено та оплачено через Stripe Checkout",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                InvalidateCache();

                _logger.LogInformation("Order {OrderNumber} created from Stripe Session {SessionId}", order.OrderNumber, session.Id);
                await _emailService.SendOrderConfirmationAsync(user.Email, order.OrderNumber, "ua");
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order from Stripe Session {SessionId}", session.Id);
                return false;
            }
        }

        private async Task<bool> ProcessFailedPayment(PaymentIntent paymentIntent)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id);

            if (payment == null) return false;

            return await CancelOrderAndRestoreStock(payment.OrderId, "Помилка оплати через Stripe (PaymentIntent Failed)");
        }

        private async Task<bool> ProcessExpiredSession(Session session)
        {
            var orderIdStr = session.ClientReferenceId;
            if (!int.TryParse(orderIdStr, out var orderId)) return false;

            return await CancelOrderAndRestoreStock(orderId, "Сесія оплати Stripe вичерпана (Expired)");
        }

        public async Task<bool> CancelOrderAndRestoreStock(int orderId, string comment)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;

            // Якщо замовлення вже скасоване, не робимо нічого
            if (order.Status == "Cancelled") return true;

            var oldStatus = order.Status;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Повернення товару на склад
                foreach (var item in order.OrderItems)
                {
                    item.Product.Stock += item.Quantity;
                }

                // Видалення видаткових накладних (OutgoingDocument), пов'язаних з цим замовленням
                var docs = await _context.OutgoingDocuments.Where(d => d.OrderId == orderId && d.Reason == "ORDER").ToListAsync();
                _context.OutgoingDocuments.RemoveRange(docs);

                if (order.Payment != null)
                {
                    order.Payment.Status = "Failed";
                }

                order.Status = "Cancelled";

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    Comment = comment,
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                InvalidateCache();

                _logger.LogInformation("Order {OrderId} cancelled and stock restored. Reason: {Comment}", orderId, comment);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling order {OrderId} and restoring stock", orderId);
                return false;
            }
        }

        public async Task<bool> RefundPaymentAsync(int orderId, string? reason = null)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.Payment == null || string.IsNullOrEmpty(order.Payment.StripePaymentIntentId))
                return false;

            if (order.Payment.Status != "Completed")
                return false;

            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = order.Payment.StripePaymentIntentId,
                    Reason = RefundReasons.RequestedByCustomer,
                    Metadata = new Dictionary<string, string>
                    {
                        { "orderId", orderId.ToString() },
                        { "reason", reason ?? "No reason provided" }
                    }
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                var oldStatus = order.Status;

                // Повернення товару на склад при Refund
                foreach (var item in order.OrderItems)
                {
                    item.Product.Stock += item.Quantity;
                }

                // Видалення видаткових накладних (OutgoingDocument), пов'язаних з цим замовленням
                var docs = await _context.OutgoingDocuments.Where(d => d.OrderId == orderId && d.Reason == "ORDER").ToListAsync();
                _context.OutgoingDocuments.RemoveRange(docs);

                order.Status = "Cancelled";
                if (order.Payment != null)
                {
                    order.Payment.Status = "Refunded";
                }

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    Comment = $"Повернення коштів через Stripe. Причина: {reason ?? "не вказана"}",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                InvalidateCache();
                _logger.LogInformation("Successfully refunded payment for Order {OrderId} and restored stock", orderId);
                return true;
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe Refund Error for Order {OrderId}", orderId);
                return false;
            }
        }
    }
}
