using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

using System.Text.Json;
using System.Globalization;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Services.PromotionService _promotionService;
        private readonly IMemoryCache _cache;

        public PromotionsController(AppDbContext context, Services.PromotionService promotionService, IMemoryCache cache)
        {
            _context = context;
            _promotionService = promotionService;
            _cache = cache;
        }

        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }

        // --- Public Methods ---

        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult> GetMyPromotions()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var promos = await _promotionService.GetActivePromotionsForUserAsync(user.StudentStatus);
            
            var result = promos.Select(p => new {
                p.PromotionId,
                p.Name,
                p.NameEn,
                p.Description,
                p.DescriptionEn,
                p.Type,
                p.Value,
                p.TargetType,
                p.EndDate
            });

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        // --- Admin Methods ---

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllPromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? type = null,
            [FromQuery] bool? isActive = null)
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(p => p.Type == type);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreatePromotion([FromBody] PromotionDto dto)
        {
            var promo = new Promotion
            {
                Name = dto.Name,
                NameEn = dto.NameEn,
                Description = dto.Description,
                DescriptionEn = dto.DescriptionEn,
                Type = dto.Type,
                Value = dto.Value,
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                AudienceType = dto.AudienceType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PromoCode = dto.PromoCode,
                MinOrderAmount = dto.MinOrderAmount,
                Priority = dto.Priority,
                UsageLimit = dto.UsageLimit,
                IsActive = true
            };

            _context.Promotions.Add(promo);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(promo, "Акцію створено"));
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> PatchPromotion(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            foreach (var update in updates)
            {
                var value = update.Value;
                switch (update.Key.ToLower())
                {
                    case "name": promo.Name = value.GetString() ?? promo.Name; break;
                    case "isactive": 
                        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) promo.IsActive = value.GetBoolean();
                        else if (bool.TryParse(value.GetString(), out var a)) promo.IsActive = a; 
                        break;
                    case "value": 
                        if (value.ValueKind == JsonValueKind.Number) promo.Value = value.GetDecimal();
                        else if (decimal.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var v)) promo.Value = v;
                        break;
                    case "enddate": 
                        if (value.ValueKind == JsonValueKind.Null) promo.EndDate = null;
                        else if (DateTime.TryParse(value.GetString(), out var d)) promo.EndDate = d;
                        break;
                }
            }

            await _context.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(promo, "Акцію частково оновлено"));
        }

        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> TogglePromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            promo.IsActive = !promo.IsActive;
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { id = promo.PromotionId, isActive = promo.IsActive }));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> DeletePromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(null, "Акцію видалено"));
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var promos = await _context.Promotions.Where(p => ids.Contains(p.PromotionId)).ToListAsync();
            _context.Promotions.RemoveRange(promos);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(null, $"Видалено {promos.Count} акцій"));
        }
    }

    public class PromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public string Type { get; set; } = "PERCENTAGE";
        public decimal Value { get; set; }
        public string TargetType { get; set; } = "PRODUCT";
        public int? TargetId { get; set; }
        public string AudienceType { get; set; } = "ALL";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PromoCode { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int Priority { get; set; } = 0;
        public int? UsageLimit { get; set; }
    }
}
