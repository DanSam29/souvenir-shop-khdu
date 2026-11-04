using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class PromotionRepository : Repository<Promotion>, IPromotionRepository
    {
        public PromotionRepository(AppDbContext context) : base(context) { }

        public async Task<Promotion?> GetByCodeAsync(string promoCode) =>
            await DbSet.FirstOrDefaultAsync(p => p.PromoCode == promoCode);

        public async Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTime at) =>
            await DbSet
                .AsNoTracking()
                .Where(p => p.IsActive
                    && (p.StartDate == null || p.StartDate <= at)
                    && (p.EndDate == null || p.EndDate >= at))
                .OrderByDescending(p => p.Priority)
                .ToListAsync();
    }
}
