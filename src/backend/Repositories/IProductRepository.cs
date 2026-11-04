using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetWithDetailsAsync(int id);
        Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
        Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold);
    }
}
