using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Доступ для менеджерів і вище (Manager+)
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IOrderRepository _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IWarehouseRepository _warehouseRepository;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            IOrderRepository orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IWarehouseRepository warehouseRepository)
        {
            _analyticsService = analyticsService;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _warehouseRepository = warehouseRepository;
        }

        // Короткий зведений звіт для головного дашборду (KPI + динаміка продажів)
        [HttpGet("overview")]
        public async Task<ActionResult> GetOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var endDate = NormalizeTo(to);
            var metrics = await _analyticsService.GetSalesMetricsAsync(from, endDate);
            var financial = await _analyticsService.GetFinancialAsync(from, endDate);
            var payments = await _analyticsService.GetPaymentStatsAsync(from, endDate);
            var timeSeries = await _analyticsService.GetTimeSeriesAsync(from, endDate);

            var result = new AnalyticsOverviewResponse
            {
                TotalRevenue = metrics.TotalRevenue,
                TotalOrders = metrics.TotalOrders,
                AverageOrderValue = metrics.AverageOrderValue,
                GrossProfit = financial.GrossProfit,
                Margin = financial.Margin,
                Payments = payments,
                SalesByDay = timeSeries.SalesByDay
            };
            return Ok(ApiResponse<AnalyticsOverviewResponse>.SuccessResult(result));
        }

        // Продажі: метрики та часовий ряд із гранулярністю day|month
        [HttpGet("sales")]
        public async Task<ActionResult> GetSales(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string groupBy = "day")
        {
            var endDate = NormalizeTo(to);
            var metrics = await _analyticsService.GetSalesMetricsAsync(from, endDate);
            var timeSeries = await _analyticsService.GetTimeSeriesAsync(from, endDate);

            var series = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase)
                ? timeSeries.SalesByMonth
                : timeSeries.SalesByDay;

            var result = new SalesReportResponse { Metrics = metrics, Series = series };
            return Ok(ApiResponse<SalesReportResponse>.SuccessResult(result));
        }

        // Товари-лідери за продажами
        [HttpGet("products")]
        public async Task<ActionResult> GetProducts(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int top = 10)
        {
            var result = await _analyticsService.GetTopProductsAsync(top, from, NormalizeTo(to));
            return Ok(ApiResponse<List<TopSellingProductResponse>>.SuccessResult(result));
        }

        // Категорії-лідери за виручкою
        [HttpGet("categories")]
        public async Task<ActionResult> GetCategories(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int top = 10)
        {
            var result = await _analyticsService.GetTopCategoriesAsync(top, from, NormalizeTo(to));
            return Ok(ApiResponse<List<TopCategoryResponse>>.SuccessResult(result));
        }

        // Ефективність акцій
        [HttpGet("promotions")]
        public async Task<ActionResult> GetPromotions([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _analyticsService.GetPromotionStatsAsync(from, NormalizeTo(to));
            return Ok(ApiResponse<List<PromotionStatsResponse>>.SuccessResult(result));
        }

        // Фінансові показники: виручка, собівартість, валовий прибуток, маржа
        [HttpGet("financial")]
        public async Task<ActionResult> GetFinancial([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _analyticsService.GetFinancialAsync(from, NormalizeTo(to));
            return Ok(ApiResponse<FinancialResponse>.SuccessResult(result));
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = to ?? DateTime.UtcNow;
            var paidOrders = await _orderRepository.Query()
                .Where(o => o.Status == "Paid" || o.Status == "Delivered" || o.Status == "Shipped")
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();
            var totalIncome = paidOrders.Sum(o => o.TotalAmount - o.ShippingCost);
            var ordersCount = paidOrders.Count;
            var avgCheck = ordersCount > 0 ? totalIncome / ordersCount : 0;
            // Розрахунок витрат на основі закупівельних цін товарів за вказаний період
            var incomingDocuments = await _warehouseRepository.GetIncomingListAsync();
            var totalExpenses = incomingDocuments
                .Where(d => d.DocumentDate >= startDate && d.DocumentDate <= endDate)
                .Sum(d => d.PurchasePrice * d.Quantity);
            var popularProducts = await _orderItemRepository.Query()
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status != "Cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new
                {
                    productId = g.Key.ProductId,
                    name = g.Key.Name,
                    quantity = g.Sum(x => x.Quantity),
                    revenue = g.Sum(x => x.FinalPrice * x.Quantity)
                })
                .OrderByDescending(x => x.quantity)
                .Take(5)
                .ToListAsync();
            var salesByDay = paidOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    amount = g.Sum(o => o.TotalAmount - o.ShippingCost),
                    count = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();
            return Ok(ApiResponse<object>.SuccessResult(new
            {
                totalIncome,
                totalExpenses,
                profit = totalIncome - totalExpenses,
                ordersCount,
                avgCheck,
                popularProducts,
                salesByDay
            }));
        }

        [HttpGet("export/sales")]
        public async Task<IActionResult> ExportSalesCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = to ?? DateTime.UtcNow;
            var orders = await _orderRepository.Query()
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("OrderNumber,Date,Customer,Amount,Status");
            foreach (var o in orders)
            {
                csv.AppendLine($"{o.OrderNumber},{o.CreatedAt:yyyy-MM-dd HH:mm},\"{o.User.FirstName} {o.User.LastName}\",{o.TotalAmount},{o.Status}");
            }
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"sales_report_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Якщо в `to` передано лише дату (час 00:00), включаємо весь останній день періоду.
        private static DateTime? NormalizeTo(DateTime? to)
            => to.HasValue && to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.Date.AddDays(1).AddSeconds(-1)
                : to;
    }
}
