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

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Оновлення інформації про платіж
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.OrderId);
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

            order.Payment.Status = "Completed";
            order.Payment.TransactionId = session.PaymentIntentId;
            order.Payment.StripePaymentIntentId = session.PaymentIntentId;
            order.Payment.UpdatedAt = DateTime.UtcNow;

            order.Status = "Paid"; // Або Processing, залежно від логіки
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = order.OrderId,
                OldStatus = "Processing",
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

            payment.Status = "Failed";
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Order != null)
            {
                payment.Order.Status = "PaymentFailed";
                payment.Order.UpdatedAt = DateTime.UtcNow;

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = payment.Order.OrderId,
                    OldStatus = payment.Order.Status,
                    NewStatus = "PaymentFailed",
                    Comment = "Помилка оплати через Stripe",
                    Timestamp = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Payment failed for PaymentIntent {PaymentIntentId}", paymentIntent.Id);
            return true;
        }

        public async Task<bool> RefundPaymentAsync(int orderId, string? reason = null)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
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

                order.Payment.Status = "Refunded";
                order.Payment.UpdatedAt = DateTime.UtcNow;
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = "Paid",
                    NewStatus = "Cancelled",
                    Comment = $"Повернення коштів через Stripe. Причина: {reason ?? "не вказана"}",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully refunded payment for Order {OrderId}", orderId);
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
