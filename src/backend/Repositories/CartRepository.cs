using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }

        public async Task<Cart?> GetByUserIdAsync(int userId) =>
            await DbSet
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Cart?> GetWithItemsAsync(int cartId) =>
            await DbSet
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CartId == cartId);
    }
}
