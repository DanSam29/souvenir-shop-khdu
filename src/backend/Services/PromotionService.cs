using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services;

public class PromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<UserPromotion> _userPromotionRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;

    public PromotionService(
        IPromotionRepository promotionRepository,
        IUserRepository userRepository,
        IRepository<UserPromotion> userPromotionRepository,
        IRepository<OrderItem> orderItemRepository)
    {
        _promotionRepository = promotionRepository;
        _userRepository = userRepository;
        _userPromotionRepository = userPromotionRepository;
        _orderItemRepository = orderItemRepository;
    }

    // Отримання списку активних акцій для конкретного користувача на основі його статусу
    public async Task<List<Promotion>> GetActivePromotionsForUserAsync(string? studentStatus)
    {
        var now = DateTime.UtcNow;
        // Отримання акцій, які не є промокодами (автоматичні акції)
        var promos = await _promotionRepository.Query()
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
    // 2. Стакування за рівнями (персональні → студентські → товар/категорія → загальні)
    // 3. Розрахунок ціни після автоматичних акцій
    public decimal GetPriceAfterPromotions(Product product, List<Promotion> promotions)
    {
        var (price, _) = ApplyPromotionsStacked(product.Price, product, promotions);
        return price;
    }

    // Розрахунок ціни з поверненням ідентифікатора найвпливовішої застосованої автоматичної акції (для аудиту, п. 7.5)
    public (decimal Price, int? AppliedPromotionId) GetPriceAfterPromotionsWithPromotion(Product product, List<Promotion> promotions)
    {
        return ApplyPromotionsStacked(product.Price, product, promotions);
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

    // Збір усіх можливих (застосовних до кошика) автоматичних знижок користувача
    public async Task<List<Promotion>> GetApplicablePromotionsAsync(int userId, List<CartItem> cartItems)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var now = DateTime.UtcNow;
        // Персональні знижки, призначені конкретному користувачу
        var assignedIds = await _userPromotionRepository.Query()
            .Where(up => up.UserId == userId)
            .Select(up => up.PromotionId)
            .ToListAsync();
        // Активні автоматичні акції (без промокодів)
        var activePromotions = await _promotionRepository.Query()
            .Where(p => p.IsActive &&
                        string.IsNullOrEmpty(p.PromoCode) &&
                        (p.StartDate == null || p.StartDate <= now) &&
                        (p.EndDate == null || p.EndDate >= now))
            .ToListAsync();
        // Фільтрація за аудиторією користувача та приналежністю до товарів кошика
        return activePromotions
            .Where(p => IsAudienceMatch(p, user, assignedIds) && AppliesToCart(p, cartItems))
            .OrderByDescending(p => p.Priority)
            .ToList();
    }

    // Валідація промокоду за 6 правилами з локалізованими повідомленнями про помилки
    public async Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string? code, int userId, List<CartItem> cartItems)
    {
        var result = new PromoCodeValidationResult { Valid = false };
        if (string.IsNullOrWhiteSpace(code))
        {
            result.Error = "Промокод не знайдено";
            return result;
        }
        var normalized = code.Trim();
        // 1. Існування промокоду
        var promo = await _promotionRepository.Query()
            .FirstOrDefaultAsync(p => p.PromoCode == normalized);
        if (promo == null)
        {
            result.Error = "Промокод не знайдено";
            return result;
        }
        // 2. Активність
        if (!promo.IsActive)
        {
            result.Error = "Промокод недійсний";
            return result;
        }
        // 3. Термін дії
        var now = DateTime.UtcNow;
        if ((promo.StartDate.HasValue && promo.StartDate > now) ||
            (promo.EndDate.HasValue && promo.EndDate < now))
        {
            result.Error = "Термін дії закінчився";
            return result;
        }
        // 4. Ліміт використань
        if (promo.UsageLimit.HasValue && promo.CurrentUsage >= promo.UsageLimit.Value)
        {
            result.Error = "Промокод вичерпано";
            return result;
        }
        // 5. Мінімальна сума замовлення
        var cartTotal = cartItems.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity);
        if (promo.MinOrderAmount.HasValue && cartTotal < promo.MinOrderAmount.Value)
        {
            result.Error = $"Мінімальна сума: {promo.MinOrderAmount.Value:0.##} грн";
            return result;
        }
        // 6. Мінімальна кількість товарів
        var totalQuantity = cartItems.Sum(ci => ci.Quantity);
        if (promo.MinQuantity.HasValue && totalQuantity < promo.MinQuantity.Value)
        {
            result.Error = $"Мінімальна кількість: {promo.MinQuantity.Value}";
            return result;
        }
        result.Valid = true;
        result.Promotion = promo;
        return result;
    }

    // Розрахунок фінальних цін для всього кошика з урахуванням автоматичних знижок та промокоду
    public async Task<CartPricingResult> CalculateFinalPricesAsync(int userId, List<CartItem> cartItems, string? promoCode = null)
    {
        var result = new CartPricingResult();
        if (cartItems == null || cartItems.Count == 0)
        {
            return result;
        }
        var applicable = await GetApplicablePromotionsAsync(userId, cartItems);
        // Розрахунок ціни кожної позиції з автоматичними знижками
        foreach (var cartItem in cartItems)
        {
            var product = cartItem.Product;
            var originalPrice = product.Price;
            var (finalUnitPrice, appliedPromotionId) = ApplyPromotionsStacked(originalPrice, product, applicable);
            var itemPrice = new CartItemPrice
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                OriginalPrice = originalPrice,
                AppliedPromotionId = appliedPromotionId,
                FinalPrice = finalUnitPrice,
                DiscountAmount = Math.Round((originalPrice - finalUnitPrice) * cartItem.Quantity, 2)
            };
            result.Items.Add(itemPrice);
            result.Subtotal += originalPrice * cartItem.Quantity;
            result.TotalDiscount += itemPrice.DiscountAmount;
        }
        result.Total = result.Subtotal - result.TotalDiscount;
        // Промокод застосовується поверх автоматичних знижок
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var validation = await ValidatePromoCodeAsync(promoCode, userId, cartItems);
            if (validation.Valid && validation.Promotion != null)
            {
                result.AppliedPromoCodeId = validation.Promotion.PromotionId;
                ApplyPromoCodeOverlay(result, validation.Promotion);
            }
            else
            {
                result.PromoCodeError = validation.Error;
            }
        }
        result.Items.ForEach(i =>
        {
            i.OriginalPrice = Math.Round(i.OriginalPrice, 2);
            i.FinalPrice = Math.Round(i.FinalPrice, 2);
            i.DiscountAmount = Math.Round(i.DiscountAmount, 2);
        });
        result.Subtotal = Math.Round(result.Subtotal, 2);
        result.TotalDiscount = Math.Round(Math.Max(0, result.TotalDiscount), 2);
        result.Total = Math.Round(Math.Max(0, result.Total), 2);
        return result;
    }

    // Призначення персональної знижки користувачу
    public async Task<bool> AssignPersonalPromotionAsync(int userId, int promotionId)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId);
        if (promotion == null)
        {
            return false;
        }
        var alreadyAssigned = await _userPromotionRepository
            .ExistsAsync(up => up.UserId == userId && up.PromotionId == promotionId);
        if (alreadyAssigned)
        {
            return false;
        }
        await _userPromotionRepository.AddAsync(new UserPromotion
        {
            UserId = userId,
            PromotionId = promotionId,
            AssignedAt = DateTime.UtcNow,
            UsedCount = 0
        });
        await _userPromotionRepository.SaveChangesAsync();
        return true;
    }

    // Призначення студентських знижок за статусом (REGULAR / SCHOLARSHIP / HIGH_ACHIEVER)
    public async Task<List<Promotion>> AssignStudentPromotionsAsync(int userId, string studentStatus)
    {
        var assigned = new List<Promotion>();
        if (string.IsNullOrWhiteSpace(studentStatus) || studentStatus == "NONE")
        {
            return assigned;
        }
        var now = DateTime.UtcNow;
        var promotions = await _promotionRepository.Query()
            .Where(p => p.IsActive &&
                        p.AudienceType == studentStatus &&
                        (p.StartDate == null || p.StartDate <= now) &&
                        (p.EndDate == null || p.EndDate >= now))
            .ToListAsync();
        if (promotions.Count == 0)
        {
            return assigned;
        }
        var existingIds = await _userPromotionRepository.Query()
            .Where(up => up.UserId == userId)
            .Select(up => up.PromotionId)
            .ToListAsync();
        foreach (var promo in promotions.Where(p => !existingIds.Contains(p.PromotionId)))
        {
            await _userPromotionRepository.AddAsync(new UserPromotion
            {
                UserId = userId,
                PromotionId = promo.PromotionId,
                AssignedAt = DateTime.UtcNow,
                UsedCount = 0
            });
            assigned.Add(promo);
        }
        if (assigned.Count > 0)
        {
            await _userPromotionRepository.SaveChangesAsync();
        }
        return assigned;
    }

    // Статистика використання акції (для адмін-панелі та ROI)
    public async Task<PromotionStatistics?> GetPromotionStatisticsAsync(int promotionId)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId);
        if (promotion == null)
        {
            return null;
        }
        var assignedUsers = await _userPromotionRepository
            .CountAsync(up => up.PromotionId == promotionId);
        var orderItems = await _orderItemRepository.Query()
            .Where(oi => oi.AppliedPromotionId == promotionId)
            .Select(oi => new { oi.OrderId, oi.Quantity, oi.DiscountAmount, oi.FinalPrice })
            .ToListAsync();
        return new PromotionStatistics
        {
            PromotionId = promotion.PromotionId,
            Name = promotion.Name,
            CurrentUsage = promotion.CurrentUsage,
            UsageLimit = promotion.UsageLimit,
            AssignedUsers = assignedUsers,
            TotalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
            TotalQuantitySold = orderItems.Sum(oi => oi.Quantity),
            TotalDiscountGiven = orderItems.Sum(oi => oi.DiscountAmount),
            TotalRevenue = orderItems.Sum(oi => oi.FinalPrice * oi.Quantity)
        };
    }

    // Визначення рівня стакування: 1 - персональні, 2 - студентські, 3 - товар/категорія, 4 - загальні
    private static int GetStackingTier(Promotion promo) => promo.AudienceType switch
    {
        "CUSTOM" => 1,
        "STUDENTS" or "REGULAR" or "SCHOLARSHIP" or "HIGH_ACHIEVER" => 2,
        _ => promo.TargetType is "PRODUCT" or "CATEGORY" ? 3 : 4
    };

    // Чи підходить знижка до конкретного товару
    private static bool AppliesToProduct(Promotion promo, Product product) => promo.TargetType switch
    {
        "PRODUCT" => promo.TargetId == product.ProductId,
        "CATEGORY" => promo.TargetId == product.CategoryId,
        "CART" => true,
        _ => false
    };

    // Чи підходить знижка хоча б до однієї позиції кошика
    private static bool AppliesToCart(Promotion promo, List<CartItem> cartItems)
    {
        if (cartItems == null || cartItems.Count == 0)
        {
            return false;
        }
        return promo.TargetType switch
        {
            "PRODUCT" => cartItems.Any(ci => ci.ProductId == promo.TargetId),
            "CATEGORY" => cartItems.Any(ci => ci.Product != null && ci.Product.CategoryId == promo.TargetId),
            "CART" => true,
            _ => false
        };
    }

    // Чи відповідає аудиторія знижки користувачу
    private static bool IsAudienceMatch(Promotion promo, User? user, List<int> assignedPromotionIds)
    {
        if (promo.AudienceType == "ALL")
        {
            return true;
        }
        if (promo.AudienceType == "CUSTOM")
        {
            return assignedPromotionIds.Contains(promo.PromotionId);
        }
        var studentStatus = user?.StudentStatus ?? "NONE";
        if (promo.AudienceType == "STUDENTS")
        {
            return studentStatus != "NONE";
        }
        if (promo.AudienceType == "STAFF")
        {
            return user?.Role is "Manager" or "Administrator" or "SuperAdmin";
        }
        // NONE / REGULAR / SCHOLARSHIP / HIGH_ACHIEVER
        return promo.AudienceType == studentStatus;
    }

    // Кумулятивне застосування знижок (стакування) до ціни одиниці товару (п. 7.3).
    // Рівні застосування: 1) персональні → 2) студентські → 3) товар/категорія → 4) загальні → 5) промокод (overlay).
    // У межах однієї цілі (TargetType + TargetId) обирається одна найкраща знижка:
    // спочатку за найвищим пріоритетом, за однакового пріоритету — найбільша вигода.
    // Різні цілі стакуються послідовно за рівнями. Фінальна ціна не може бути меншою за 0.
    private (decimal Price, int? AppliedPromotionId) ApplyPromotionsStacked(decimal basePrice, Product product, List<Promotion> promotions)
    {
        var price = basePrice;
        int? appliedPromotionId = null;
        decimal largestDiscount = 0;

        var applicable = promotions.Where(p => AppliesToProduct(p, product)).ToList();
        var groups = applicable
            .GroupBy(p => new { Tier = GetStackingTier(p), p.TargetType, p.TargetId })
            .OrderBy(g => g.Key.Tier)
            .ThenByDescending(g => g.Max(p => p.Priority));

        foreach (var group in groups)
        {
            // Найкраща знижка в межах цілі (7.3): спочатку найвищий пріоритет,
            // за однакового пріоритету — найбільша вигода
            var best = group
                .OrderByDescending(p => p.Priority)
                .ThenByDescending(p => price - CalculateDiscountedPrice(price, p))
                .First();
            var bestPrice = CalculateDiscountedPrice(price, best);
            var discount = price - bestPrice;
            if (discount > largestDiscount)
            {
                largestDiscount = discount;
                appliedPromotionId = best.PromotionId;
            }
            price = Math.Max(0, bestPrice);
        }
        return (Math.Round(price, 2), appliedPromotionId);
    }

    // Застосування промокоду поверх автоматичних знижок
    private static void ApplyPromoCodeOverlay(CartPricingResult result, Promotion promo)
    {
        if (promo.Type == "PERCENTAGE")
        {
            var percent = Math.Clamp((double)promo.Value, 0, 100);
            foreach (var item in result.Items)
            {
                var perUnitDiscount = Math.Round(item.FinalPrice * (decimal)(percent / 100.0), 2);
                var itemDiscount = perUnitDiscount * item.Quantity;
                item.DiscountAmount += itemDiscount;
                item.FinalPrice = Math.Max(0, item.FinalPrice - perUnitDiscount);
                item.AppliedPromotionId = promo.PromotionId;
                result.TotalDiscount += itemDiscount;
            }
        }
        else
        {
            // FIXED_AMOUNT / SPECIAL_PRICE — розподіл суми знижки пропорційно між позиціями
            var reduction = promo.Type == "SPECIAL_PRICE"
                ? Math.Max(0, result.Total - promo.Value)
                : Math.Max(0, promo.Value);
            var appliedTotal = Math.Min(reduction, result.Total);
            if (appliedTotal > 0 && result.Total > 0)
            {
                foreach (var item in result.Items)
                {
                    var itemSubtotal = item.FinalPrice * item.Quantity;
                    var share = itemSubtotal / result.Total;
                    var itemDiscount = Math.Round(appliedTotal * share, 2);
                    var perUnitDiscount = item.Quantity > 0 ? Math.Round(itemDiscount / item.Quantity, 2) : 0;
                    item.DiscountAmount += itemDiscount;
                    item.FinalPrice = Math.Max(0, item.FinalPrice - perUnitDiscount);
                    item.AppliedPromotionId = promo.PromotionId;
                    result.TotalDiscount += itemDiscount;
                }
            }
        }
        result.Total = Math.Max(0, result.Subtotal - result.TotalDiscount);
    }
}
