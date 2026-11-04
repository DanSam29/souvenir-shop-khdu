using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly AppDbContext _context;

        public AnalyticsRepository(AppDbContext context) => _context = context;

        private IQueryable<Order> OrderQuery(DateTime? from, DateTime? to)
        {
            var query = _context.Orders.Where(o => o.Status != "Cancelled");
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            return query;
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null) =>
            await OrderQuery(from, to).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        public async Task<int> GetOrdersCountAsync(DateTime? from = null, DateTime? to = null) =>
            await OrderQuery(from, to).CountAsync();

        public async Task<decimal> GetAverageOrderValueAsync(DateTime? from = null, DateTime? to = null) =>
            await OrderQuery(from, to).AverageAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        public async Task<IReadOnlyList<ProductSalesRecord>> GetTopSellingProductsAsync(int top, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.OrderItems
                .Join(OrderQuery(from, to), oi => oi.OrderId, o => o.OrderId, (oi, o) => new { oi, o })
                .Join(_context.Products, x => x.oi.ProductId, p => p.ProductId, (x, p) => new { x.oi, p })
                .GroupBy(x => new { x.oi.ProductId, x.p.Name })
                .Select(g => new ProductSalesRecord(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Sum(x => x.oi.Quantity),
                    g.Sum(x => x.oi.FinalPrice * x.oi.Quantity)))
                .OrderByDescending(r => r.QuantitySold)
                .Take(top);

            return await query.ToListAsync();
        }
    }
}
