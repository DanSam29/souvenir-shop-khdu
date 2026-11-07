using KhduSouvenirShop.API.Models;
using Stripe;
using Stripe.Checkout;

namespace KhduSouvenirShop.API.Services
{
    public interface IPaymentService
    {
        Task<Session> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);
        Task<Session> CreateCheckoutSessionForCartAsync(User user, Cart cart, List<Promotion> promos, string? promoCode, string successUrl, string cancelUrl, Dictionary<string, string> metadata);
        Task<bool> HandleWebhookAsync(string? json, string? stripeSignature, string? sessionId = null);
        Event ConstructEventAsync(string payload, string signatureHeader);
        Task<Refund?> CreateRefundAsync(string paymentIntentId, decimal? amount = null);
        Task<bool> RefundPaymentAsync(int orderId, string? reason = null);
        Task<bool> CancelOrderAndRestoreStock(int orderId, string comment);
    }
}