using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        // Статуси, за якими замовлення вважається фактичним продажем (оплачені).
        private static readonly string[] PaidStatuses = { "Paid", "Shipped", "Delivered" };

        private readonly AppDbContext _context;

        public AnalyticsRepository(AppDbContext context) => _context = context;

        // База продажів — оплачені замовлення за період.
        private IQueryable<Order> PaidOrderQuery(DateTime? from, DateTime? to)
        {
            var query = _context.Orders.Where(o => PaidStatuses.Contains(o.Status));
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            return query;
        }

        // Усі замовлення періоду — для розподілу за статусами.
        private IQueryable<Order> AllOrderQuery(DateTime? from, DateTime? to)
        {
            var query = _context.Orders.AsQueryable();
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            return query;
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null) =>
            await PaidOrderQuery(from, to).SumAsync(o => (decimal?)(o.TotalAmount - o.ShippingCost)) ?? 0m;

        public async Task<int> GetOrdersCountAsync(DateTime? from = null, DateTime? to = null) =>
            await PaidOrderQuery(from, to).CountAsync();

        public async Task<decimal> GetAverageOrderValueAsync(DateTime? from = null, DateTime? to = null) =>
            await PaidOrderQuery(from, to).AverageAsync(o => (decimal?)(o.TotalAmount - o.ShippingCost)) ?? 0m;

        public async Task<IReadOnlyList<ProductSalesRecord>> GetTopSellingProductsAsync(int top, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.OrderItems
                .Join(PaidOrderQuery(from, to), oi => oi.OrderId, o => o.OrderId, (oi, o) => oi)
                .Join(_context.Products, oi => oi.ProductId, p => p.ProductId, (oi, p) => new { oi, p })
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

        public async Task<IReadOnlyList<StatusCountRecord>> GetOrdersCountByStatusAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = AllOrderQuery(from, to)
                .GroupBy(o => o.Status)
                .Select(g => new StatusCountRecord(g.Key, g.Count()));
            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<TimeSeriesRecord>> GetSalesByDayAsync(DateTime? from = null, DateTime? to = null)
        {
            var rows = await PaidOrderQuery(from, to)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(o => o.TotalAmount - o.ShippingCost),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return rows
                .Select(x => new TimeSeriesRecord(x.Date.ToString("yyyy-MM-dd"), x.Amount, x.Count))
                .ToList();
        }

        public async Task<IReadOnlyList<TimeSeriesRecord>> GetSalesByMonthAsync(DateTime? from = null, DateTime? to = null)
        {
            var rows = await PaidOrderQuery(from, to)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(o => o.TotalAmount - o.ShippingCost),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return rows
                .Select(x => new TimeSeriesRecord($"{x.Year:D4}-{x.Month:D2}", x.Amount, x.Count))
                .ToList();
        }

        public async Task<IReadOnlyList<CategorySalesRecord>> GetTopCategoriesAsync(int top, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.OrderItems
                .Join(PaidOrderQuery(from, to), oi => oi.OrderId, o => o.OrderId, (oi, o) => oi)
                .Join(_context.Products, oi => oi.ProductId, p => p.ProductId, (oi, p) => new { oi, p })
                .Join(_context.Categories, x => x.p.CategoryId, c => c.CategoryId, (x, c) => new { x.oi, c })
                .GroupBy(x => new { x.c.CategoryId, x.c.Name })
                .Select(g => new CategorySalesRecord(
                    g.Key.CategoryId,
                    g.Key.Name,
                    g.Sum(x => x.oi.FinalPrice * x.oi.Quantity)))
                .OrderByDescending(r => r.Revenue)
                .Take(top);

            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<PaymentMethodCountRecord>> GetPaymentMethodStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Payments
                .Join(PaidOrderQuery(from, to), p => p.OrderId, o => o.OrderId, (p, o) => p)
                .GroupBy(p => p.Method)
                .Select(g => new PaymentMethodCountRecord(g.Key, g.Count()));
            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<PromotionUsageRecord>> GetPromotionStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            // Вибираємо потрібні поля, а групування та підрахунок унікальних замовлень
            // виконуємо в пам'яті (distinct за замовленням не завжди транслюється в SQL).
            var rows = await _context.OrderItems
                .Join(PaidOrderQuery(from, to), oi => oi.OrderId, o => o.OrderId, (oi, o) => oi)
                .Where(oi => oi.AppliedPromotionId != null)
                .Join(_context.Promotions, oi => oi.AppliedPromotionId, p => p.PromotionId,
                    (oi, p) => new
                    {
                        oi.OrderId,
                        oi.Quantity,
                        oi.DiscountAmount,
                        oi.FinalPrice,
                        p.PromotionId,
                        p.Name
                    })
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.PromotionId, x.Name })
                .Select(g => new PromotionUsageRecord(
                    g.Key.PromotionId,
                    g.Key.Name,
                    g.Select(x => x.OrderId).Distinct().Count(),
                    g.Sum(x => x.DiscountAmount),
                    g.Sum(x => x.FinalPrice * x.Quantity)))
                .OrderByDescending(r => r.Uses)
                .ToList();
        }

        public async Task<decimal> GetTotalPurchaseCostAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.IncomingDocuments.Where(d => d.Reason == "INCOMING");
            if (from.HasValue) query = query.Where(d => d.DocumentDate >= from.Value);
            if (to.HasValue) query = query.Where(d => d.DocumentDate <= to.Value);
            return await query.SumAsync(d => (decimal?)(d.PurchasePrice * d.Quantity)) ?? 0m;
        }
    }
}
