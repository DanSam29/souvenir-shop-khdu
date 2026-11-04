using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync() =>
            await DbSet
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

        public async Task<Category?> GetWithSubCategoriesAsync(int id) =>
            await DbSet
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

        public async Task<bool> HasProductsAsync(int categoryId) =>
            await Context.Products.AnyAsync(p => p.CategoryId == categoryId);
    }
}
