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
    // Отримання списку активних акцій для конкретного користувача на основі його статусу
    public async Task<List<Promotion>> GetActivePromotionsForUserAsync(string? studentStatus)
    {
        var now = DateTime.UtcNow;
        // Отримання акцій, які не є промокодами (автоматичні акції)
        var promos = await _context.Promotions
            .Where(p => p.IsActive && 
                        string.IsNullOrEmpty(p.PromoCode) && 
                        (p.StartDate == null || p.StartDate <= now) && 
                        (p.EndDate == null || p.EndDate >= now))
            .ToListAsync();
        // Фільтрація за аудиторією (всі або специфічний статус студента)
        var filtered = promos.Where(p => 
            p.AudienceType == "ALL" || 
            (studentStatus != null && p.AudienceType == studentStatus)
        ).ToList();
        // Сортування за пріоритетом (вищий пріоритет застосовується першим)
        return filtered.OrderByDescending(p => p.Priority).ToList();
    }
    // Двигун розрахунку ціни (Promotion Engine):
    // 1. Збір всіх застосованих автоматичних знижок
    // 2. Вибір найвигіднішої знижки на кожну позицію (якщо пріоритети рівні)
    // 3. Розрахунок ціну після автоматичних акцій
    public decimal GetPriceAfterPromotions(Product product, List<Promotion> promotions)
    {
        decimal bestPrice = product.Price;
        int topPriority = -1;
        // Вибір найвигіднішої знижки на кожну позицію (якщо пріоритети рівні)
        foreach (var promo in promotions)
        {
            bool isApplicable = false;
            if (promo.TargetType == "PRODUCT" && promo.TargetId == product.ProductId) isApplicable = true;
            else if (promo.TargetType == "CATEGORY" && promo.TargetId == product.CategoryId) isApplicable = true;
            else if (promo.TargetType == "CART") isApplicable = true;
            if (isApplicable)
            {
                decimal currentPromoPrice = CalculateDiscountedPrice(product.Price, promo);
                // Якщо пріоритет вищий - обов'язковий вибір цієї знижки
                if (promo.Priority > topPriority)
                {
                    topPriority = promo.Priority;
                    bestPrice = currentPromoPrice;
                }
                // Якщо пріоритет такий самий - вибір вигіднішої знижки
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
    // Розрахунок ціни після застосування конкретної знижки
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
    // Перевірка та повернення промокоду, якщо він дійсний
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