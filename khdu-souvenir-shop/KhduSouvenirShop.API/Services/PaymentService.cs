using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace KhduSouvenirShop.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _webhookSecret;

        public PaymentService(AppDbContext context, ILogger<PaymentService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
        }

        public async Task<Session> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.OrderId);
            var idempotencyKey = payment?.IdempotencyKey ?? Guid.NewGuid().ToString();

            if (payment != null && string.IsNullOrEmpty(payment.IdempotencyKey))
            {
                payment.IdempotencyKey = idempotencyKey;
                await _context.SaveChangesAsync();
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = order.OrderItems.Select(item => new SessionLineItemOptions
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
                }).ToList(),
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

        public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _webhookSecret);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null) return false;

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
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null) return false;

                    return await ProcessExpiredSession(session);
                }

                return true;
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe Webhook Error");
                return false;
            }
        }

        private async Task<bool> ProcessSuccessfulPayment(Session session)
        {
            var orderIdStr = session.ClientReferenceId;
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
            order.Payment.UpdatedAt = DateTime.UtcNow;

            order.Status = "Paid";
            order.UpdatedAt = DateTime.UtcNow;

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
            return true;
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

        private async Task<bool> CancelOrderAndRestoreStock(int orderId, string comment)
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

                if (order.Payment != null)
                {
                    order.Payment.Status = "Failed";
                    order.Payment.UpdatedAt = DateTime.UtcNow;
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

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

                order.Payment.Status = "Refunded";
                order.Payment.UpdatedAt = DateTime.UtcNow;
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    Comment = $"Повернення коштів через Stripe. Причина: {reason ?? "не вказана"}",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
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
