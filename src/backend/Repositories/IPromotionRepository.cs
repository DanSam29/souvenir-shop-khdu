using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Task<Promotion?> GetByCodeAsync(string promoCode);
        Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTime at);
    }
}
