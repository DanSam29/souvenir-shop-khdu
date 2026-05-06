using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

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
                p.Description,
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
        public async Task<ActionResult> GetAllPromotions()
        {
            var promos = await _context.Promotions.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(promos));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> CreatePromotion([FromBody] PromotionDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var promo = new Promotion
            {
                Name = dto.Name,
                Description = dto.Description,
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
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Promotions.Add(promo);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(promo, "Акцію створено"));
        }

        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> TogglePromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            promo.IsActive = !promo.IsActive;
            promo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { id = promo.PromotionId, isActive = promo.IsActive }));
        }
    }

    public class PromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
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
