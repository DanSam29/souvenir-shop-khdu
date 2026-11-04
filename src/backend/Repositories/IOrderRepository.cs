using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByNumberAsync(string orderNumber);
        Task<Order?> GetWithDetailsAsync(int orderId);
        Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId);
    }
}
