using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IShippingRepository : IRepository<Shipping>
    {
        Task<Shipping?> GetByOrderIdAsync(int orderId);
    }
}
