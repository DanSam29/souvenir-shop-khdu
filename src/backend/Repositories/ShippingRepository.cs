using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class ShippingRepository : Repository<Shipping>, IShippingRepository
    {
        public ShippingRepository(AppDbContext context) : base(context) { }

        public async Task<Shipping?> GetByOrderIdAsync(int orderId) =>
            await DbSet.FirstOrDefaultAsync(s => s.OrderId == orderId);
    }
}
