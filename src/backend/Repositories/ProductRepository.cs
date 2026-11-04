using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<Product?> GetWithDetailsAsync(int id) =>
            await DbSet
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

        public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId) =>
            await DbSet.AsNoTracking().Where(p => p.CategoryId == categoryId).ToListAsync();

        public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold) =>
            await DbSet.AsNoTracking().Where(p => p.Stock <= threshold).ToListAsync();
    }
}
