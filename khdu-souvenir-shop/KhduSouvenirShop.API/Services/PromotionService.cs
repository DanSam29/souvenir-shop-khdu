using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services;

public class PromotionService
{
    private readonly AppDbContext _context;

    public PromotionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Promotion>> GetActivePromotionsForUserAsync(string? studentStatus)
    {
        var now = DateTime.UtcNow;
        var promos = await _context.Promotions
            .Where(p => p.IsActive && (p.StartDate == null || p.StartDate <= now) && (p.EndDate == null || p.EndDate >= now))
            .ToListAsync();

        var filtered = promos.Where(p => p.AudienceType == "ALL" || (studentStatus != null && p.AudienceType == studentStatus)).ToList();

        // Сортуємо по пріоритету (вищий пріоритет застосовується першим)
        return filtered.OrderByDescending(p => p.Priority).ToList();
    }

    public decimal GetPriceAfterPromotions(Product product, List<Promotion> promotions)
    {
        decimal price = product.Price;

        foreach (var promo in promotions)
        {
            if (promo.Type != "PERCENTAGE") continue;

            // CART applies to all products
            if (promo.TargetType == "CART")
            {
                var percent = Math.Clamp((double)promo.Value, 0, 100);
                price = Math.Round(price * (decimal)(1 - percent / 100.0), 2);
            }
            else if (promo.TargetType == "PRODUCT" && promo.TargetId == product.ProductId)
            {
                var percent = Math.Clamp((double)promo.Value, 0, 100);
                price = Math.Round(price * (decimal)(1 - percent / 100.0), 2);
            }
            else if (promo.TargetType == "CATEGORY" && promo.TargetId == product.CategoryId)
            {
                var percent = Math.Clamp((double)promo.Value, 0, 100);
                price = Math.Round(price * (decimal)(1 - percent / 100.0), 2);
            }
        }

        return price;
    }
}
