using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context) { }

        public async Task<Payment?> GetByOrderIdAsync(int orderId) =>
            await DbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);

        public async Task<Payment?> GetByStripeSessionIdAsync(string sessionId) =>
            await DbSet.FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);

        public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId) =>
            await DbSet.FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
    }
}
