using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IReadOnlyList<Category>> GetRootCategoriesAsync();
        Task<Category?> GetWithSubCategoriesAsync(int id);
        Task<bool> HasProductsAsync(int categoryId);
    }
}
