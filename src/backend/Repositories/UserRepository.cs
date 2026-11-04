using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email) =>
            await DbSet.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<bool> EmailExistsAsync(string email) =>
            await DbSet.AnyAsync(u => u.Email == email);

        public async Task<IReadOnlyList<User>> GetByRoleAsync(string role) =>
            await DbSet.AsNoTracking().Where(u => u.Role == role).ToListAsync();

        public async Task<User?> GetWithCartAsync(int userId) =>
            await DbSet
                .Include(u => u.Cart)
                    .ThenInclude(c => c!.CartItems)
                        .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(u => u.UserId == userId);
    }
}
