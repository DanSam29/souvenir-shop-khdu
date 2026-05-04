using KhduSouvenirShop.API.Models;
using Stripe.Checkout;

namespace KhduSouvenirShop.API.Services
{
    public interface IPaymentService
    {
        Task<Session> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);
        Task<bool> HandleWebhookAsync(string json, string stripeSignature);
        Task<bool> RefundPaymentAsync(int orderId, string? reason = null);
        Task<bool> CancelOrderAndRestoreStock(int orderId, string comment);
    }
}
