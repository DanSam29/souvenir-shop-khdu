using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class AnalyticsController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = to ?? DateTime.UtcNow;

            var paidOrders = await _context.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Delivered" || o.Status == "Shipped")
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var totalIncome = paidOrders.Sum(o => o.TotalAmount);
            var ordersCount = paidOrders.Count;
            var avgCheck = ordersCount > 0 ? totalIncome / ordersCount : 0;

            // Витрати (закупівля товарів, що були продані)
            // Для спрощення беремо суму всіх прибуткових накладних за період
            var totalExpenses = await _context.IncomingDocuments
                .Where(d => d.DocumentDate >= startDate && d.DocumentDate <= endDate)
                .SumAsync(d => d.PurchasePrice * d.Quantity);

            var popularProducts = await _context.OrderItems
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
                    amount = g.Sum(o => o.TotalAmount),
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

            var orders = await _context.Orders
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
    }
}
