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

    /// <summary>
    /// Отримує список активних акцій для конкретного користувача на основі його статусу.
    /// </summary>
    public async Task<List<Promotion>> GetActivePromotionsForUserAsync(string? studentStatus)
    {
        var now = DateTime.UtcNow;
        // Отримуємо акції, які не є промокодами (автоматичні акції)
        var promos = await _context.Promotions
            .Where(p => p.IsActive && 
                        string.IsNullOrEmpty(p.PromoCode) && 
                        (p.StartDate == null || p.StartDate <= now) && 
                        (p.EndDate == null || p.EndDate >= now))
            .ToListAsync();

        // Фільтруємо за аудиторією (всі або специфічний статус студента)
        var filtered = promos.Where(p => 
            p.AudienceType == "ALL" || 
            (studentStatus != null && p.AudienceType == studentStatus)
        ).ToList();

        // Сортуємо за пріоритетом (вищий пріоритет застосовується першим)
        return filtered.OrderByDescending(p => p.Priority).ToList();
    }

    /// <summary>
    /// Двигун розрахунку ціни (Promotion Engine):
    /// 1. Збирає всі застосовні автоматичні знижки.
    /// 2. Вибирає найвигіднішу знижку на кожну позицію (якщо пріоритети рівні).
    /// 3. Розраховує ціну після автоматичних акцій.
    /// </summary>
    public decimal GetPriceAfterPromotions(Product product, List<Promotion> promotions)
    {
        decimal bestPrice = product.Price;
        int topPriority = -1;

        // Згідно з планом: "вибрати найвигіднішу на лінійку"
        foreach (var promo in promotions)
        {
            bool isApplicable = false;

            if (promo.TargetType == "PRODUCT" && promo.TargetId == product.ProductId) isApplicable = true;
            else if (promo.TargetType == "CATEGORY" && promo.TargetId == product.CategoryId) isApplicable = true;
            else if (promo.TargetType == "CART") isApplicable = true;

            if (isApplicable)
            {
                decimal currentPromoPrice = CalculateDiscountedPrice(product.Price, promo);
                
                // Якщо пріоритет вищий - беремо цю знижку обов'язково
                if (promo.Priority > topPriority)
                {
                    topPriority = promo.Priority;
                    bestPrice = currentPromoPrice;
                }
                // Якщо пріоритет такий самий - беремо ту, де ціна нижча (вигідніша)
                else if (promo.Priority == topPriority)
                {
                    if (currentPromoPrice < bestPrice)
                    {
                        bestPrice = currentPromoPrice;
                    }
                }
            }
        }

        return bestPrice;
    }

    /// <summary>
    /// Розраховує ціну після застосування конкретної знижки.
    /// </summary>
    public decimal CalculateDiscountedPrice(decimal originalPrice, Promotion promo)
    {
        if (promo.Type == "PERCENTAGE")
        {
            var percent = Math.Clamp((double)promo.Value, 0, 100);
            return Math.Round(originalPrice * (decimal)(1 - percent / 100.0), 2);
        }
        else if (promo.Type == "FIXED_AMOUNT")
        {
            return Math.Max(0, originalPrice - promo.Value);
        }
        else if (promo.Type == "SPECIAL_PRICE")
        {
            return promo.Value;
        }
        
        return originalPrice;
    }

    /// <summary>
    /// Перевіряє та повертає промокод, якщо він дійсний.
    /// </summary>
    public async Task<Promotion?> ValidatePromoCodeAsync(string code, decimal currentTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var now = DateTime.UtcNow;
        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => 
                p.PromoCode == code && 
                p.IsActive && 
                (p.StartDate == null || p.StartDate <= now) && 
                (p.EndDate == null || p.EndDate >= now) &&
                (p.UsageLimit == null || p.CurrentUsage < p.UsageLimit));

        if (promo != null && promo.MinOrderAmount.HasValue && currentTotal < promo.MinOrderAmount.Value)
        {
            return null; // Не виконується умова мінімальної суми
        }

        return promo;
    }
}