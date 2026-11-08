using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Публічний доступ до акцій: список активних загальних акцій, персональні акції користувача
    /// та валідація промокоду під час оформлення замовлення.
    /// Керування акціями (CRUD, призначення, статистика) — у AdminPromotionsController.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICartRepository _cartRepository;
        private readonly Services.PromotionService _promotionService;
        public PromotionsController(
            IPromotionRepository promotionRepository,
            IUserRepository userRepository,
            ICartRepository cartRepository,
            Services.PromotionService promotionService)
        {
            _promotionRepository = promotionRepository;
            _userRepository = userRepository;
            _cartRepository = cartRepository;
            _promotionService = promotionService;
        }
        // Активні загальні акції для публічної сторінки "Акції"
        [HttpGet("active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetActivePromotions()
        {
            var now = DateTime.UtcNow;
            var items = await _promotionRepository.Query()
                .Where(p => p.IsActive &&
                            string.IsNullOrEmpty(p.PromoCode) &&
                            p.AudienceType == "ALL" &&
                            (p.StartDate == null || p.StartDate <= now) &&
                            (p.EndDate == null || p.EndDate >= now))
                .OrderByDescending(p => p.Priority)
                .Select(p => new
                {
                    p.PromotionId,
                    p.Name,
                    p.NameEn,
                    p.Description,
                    p.DescriptionEn,
                    p.Type,
                    p.Value,
                    p.TargetType,
                    p.TargetId,
                    p.EndDate
                })
                .ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(items));
        }
        // Доступні поточному користувачу акції (персональні та за статусом)
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult> GetMyPromotions()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
            var user = await _userRepository.GetByIdAsync(userId);
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
        // Валідація промокоду при checkout (6 правил у PromotionService)
        [HttpPost("validate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }
            if (string.IsNullOrWhiteSpace(dto?.Code))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Промокод не знайдено", "BadRequest"));
            }
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || cart.CartItems.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кошик порожній", "BadRequest"));
            }
            var validation = await _promotionService.ValidatePromoCodeAsync(dto.Code, userId, cart.CartItems.ToList());
            var payload = new
            {
                valid = validation.Valid,
                error = validation.Error,
                promotion = validation.Valid && validation.Promotion != null
                    ? new
                    {
                        promotionId = validation.Promotion.PromotionId,
                        name = validation.Promotion.Name,
                        nameEn = validation.Promotion.NameEn,
                        type = validation.Promotion.Type,
                        value = validation.Promotion.Value,
                        targetType = validation.Promotion.TargetType
                    }
                    : null
            };
            return Ok(ApiResponse<object>.SuccessResult(payload, validation.Valid ? "Промокод дійсний" : validation.Error));
        }
    }
    public class ValidatePromoCodeDto
    {
        public string? Code { get; set; }
    }
}
