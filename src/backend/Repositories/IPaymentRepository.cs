using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByOrderIdAsync(int orderId);
        Task<Payment?> GetByStripeSessionIdAsync(string sessionId);
        Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId);
    }
}
