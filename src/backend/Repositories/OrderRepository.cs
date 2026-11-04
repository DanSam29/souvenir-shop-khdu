using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }

        public async Task<Order?> GetByNumberAsync(string orderNumber) =>
            await DbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        public async Task<Order?> GetWithDetailsAsync(int orderId) =>
            await DbSet
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Include(o => o.Payment)
                .Include(o => o.OrderHistories)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

        public async Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId) =>
            await DbSet
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
    }
}
