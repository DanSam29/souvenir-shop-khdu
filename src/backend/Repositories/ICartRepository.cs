using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<Cart?> GetWithItemsAsync(int cartId);
    }
}
