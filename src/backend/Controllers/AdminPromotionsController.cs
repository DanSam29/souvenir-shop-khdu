using System.Globalization;
using System.Text;
using System.Text.Json;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Адміністративне керування акціями: CRUD, персональне призначення користувачам,
    /// статистика ROI та експорт статистики у CSV/PDF.
    /// Доступ: Manager, Administrator, SuperAdmin.
    /// </summary>
    [Route("api/admin/promotions")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class AdminPromotionsController(
        IPromotionRepository promotionRepository,
        IUserRepository userRepository,
        PromotionService promotionService) : ControllerBase
    {
        private readonly IPromotionRepository _promotionRepository = promotionRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly PromotionService _promotionService = promotionService;

        // Список акцій із фільтрами (тип, активність, пошук) та пагінацією
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetPromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? type = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            var query = _promotionRepository.Query().AsQueryable();
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(p => p.Type == type);
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s)
                    || (p.NameEn != null && p.NameEn.ToLower().Contains(s))
                    || (p.PromoCode != null && p.PromoCode.ToLower().Contains(s)));
            }
            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                    p.AudienceType,
                    p.StartDate,
                    p.EndDate,
                    p.PromoCode,
                    p.MinOrderAmount,
                    p.MinQuantity,
                    p.Priority,
                    p.UsageLimit,
                    p.CurrentUsage,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();
            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        // Деталі окремої акції
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            return Ok(ApiResponse<object>.SuccessResult(ToResult(promo)));
        }

        // Створення нової акції
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreatePromotion([FromBody] PromotionDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Назва акції обов'язкова", "BadRequest"));
            }
            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                var codeExists = await _promotionRepository.ExistsAsync(p => p.PromoCode == dto.PromoCode);
                if (codeExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Промокод уже використовується", "BadRequest"));
                }
            }
            var promo = new Promotion
            {
                Name = dto.Name.Trim(),
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
                MinQuantity = dto.MinQuantity,
                Priority = dto.Priority,
                UsageLimit = dto.UsageLimit,
                IsActive = true
            };
            await _promotionRepository.AddAsync(promo);
            await _promotionRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = promo.PromotionId }, ApiResponse<object>.SuccessResult(ToResult(promo), "Акцію створено"));
        }

        // Повне оновлення акції
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdatePromotion(int id, [FromBody] PromotionDto dto)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Назва акції обов'язкова", "BadRequest"));
            }
            if (!string.IsNullOrWhiteSpace(dto.PromoCode) && dto.PromoCode != promo.PromoCode)
            {
                var codeExists = await _promotionRepository.ExistsAsync(p => p.PromoCode == dto.PromoCode && p.PromotionId != id);
                if (codeExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Промокод уже використовується", "BadRequest"));
                }
            }
            promo.Name = dto.Name.Trim();
            promo.NameEn = dto.NameEn;
            promo.Description = dto.Description;
            promo.DescriptionEn = dto.DescriptionEn;
            promo.Type = dto.Type;
            promo.Value = dto.Value;
            promo.TargetType = dto.TargetType;
            promo.TargetId = dto.TargetId;
            promo.AudienceType = dto.AudienceType;
            promo.StartDate = dto.StartDate;
            promo.EndDate = dto.EndDate;
            promo.PromoCode = dto.PromoCode;
            promo.MinOrderAmount = dto.MinOrderAmount;
            promo.MinQuantity = dto.MinQuantity;
            promo.Priority = dto.Priority;
            promo.UsageLimit = dto.UsageLimit;
            await _promotionRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(ToResult(promo), "Акцію оновлено"));
        }

        // Часткове оновлення окремих полів
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PatchPromotion(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            foreach (var update in updates)
            {
                var value = update.Value;
                switch (update.Key.ToLower())
                {
                    case "name": promo.Name = value.GetString() ?? promo.Name; break;
                    case "nameen": promo.NameEn = value.GetString(); break;
                    case "description": promo.Description = value.GetString(); break;
                    case "value":
                        if (value.ValueKind == JsonValueKind.Number) promo.Value = value.GetDecimal();
                        else if (decimal.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var v)) promo.Value = v;
                        break;
                    case "isactive":
                        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) promo.IsActive = value.GetBoolean();
                        else if (bool.TryParse(value.GetString(), out var a)) promo.IsActive = a;
                        break;
                    case "enddate":
                        if (value.ValueKind == JsonValueKind.Null) promo.EndDate = null;
                        else if (DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var d)) promo.EndDate = d;
                        break;
                }
            }
            await _promotionRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(ToResult(promo), "Акцію частково оновлено"));
        }

        // Перемикання активності акції
        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> TogglePromotion(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            promo.IsActive = !promo.IsActive;
            await _promotionRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { id = promo.PromotionId, isActive = promo.IsActive }, "Статус акції змінено"));
        }

        // Видалення акції
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeletePromotion(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            _promotionRepository.Remove(promo);
            await _promotionRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Акцію видалено"));
        }

        // Масове видалення акцій
        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Список ідентифікаторів порожній", "BadRequest"));
            }
            var promos = await _promotionRepository.Query().Where(p => ids.Contains(p.PromotionId)).ToListAsync();
            _promotionRepository.RemoveRange(promos);
            await _promotionRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount = promos.Count }, $"Видалено {promos.Count} акцій"));
        }

        // Персональне призначення акції конкретному користувачу
        [HttpPost("{id}/assign/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AssignPromotion(int id, int userId)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            var assigned = await _promotionService.AssignPersonalPromotionAsync(userId, id);
            if (!assigned)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Акцію вже призначено цьому користувачу", "BadRequest"));
            }
            return Ok(ApiResponse<object>.SuccessResult(new { promotionId = id, userId }, "Акцію призначено користувачу"));
        }

        // Статистика використання акції (ROI)
        [HttpGet("{id}/statistics")]
        [ProducesResponseType(typeof(ApiResponse<PromotionStatistics>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetStatistics(int id)
        {
            var stats = await _promotionService.GetPromotionStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            return Ok(ApiResponse<PromotionStatistics>.SuccessResult(stats));
        }

        // Експорт статистики акції у CSV
        [HttpGet("{id}/statistics/export/csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportStatisticsCsv(int id)
        {
            var stats = await _promotionService.GetPromotionStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            var csv = new StringBuilder();
            csv.AppendLine("PromotionId,Name,CurrentUsage,UsageLimit,AssignedUsers,TotalOrders,TotalQuantitySold,TotalDiscountGiven,TotalRevenue,GeneratedAt");
            csv.AppendLine($"{stats.PromotionId},\"{stats.Name}\",{stats.CurrentUsage},{stats.UsageLimit?.ToString() ?? string.Empty},{stats.AssignedUsers},{stats.TotalOrders},{stats.TotalQuantitySold},{stats.TotalDiscountGiven.ToString(CultureInfo.InvariantCulture)},{stats.TotalRevenue.ToString(CultureInfo.InvariantCulture)},{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"promotion_{stats.PromotionId}_statistics_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Експорт статистики акції у PDF
        [HttpGet("{id}/statistics/export/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportStatisticsPdf(int id)
        {
            var stats = await _promotionService.GetPromotionStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Акцію не знайдено", "NotFound"));
            }
            using var stream = new MemoryStream();
            using (var writer = new PdfWriter(stream))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                var font = ResolveFont();
                document.Add(new Paragraph("Статистика акції / Promotion statistics").SetFont(font).SetFontSize(16));
                document.Add(new Paragraph($"#{stats.PromotionId} {stats.Name}").SetFont(font).SetFontSize(12));
                document.Add(new Paragraph($"Сформовано: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").SetFont(font).SetFontSize(10));
                var table = new Table(new float[] { 3, 1 }).UseAllAvailableWidth();
                void AddRow(string label, string value)
                {
                    table.AddCell(new Cell().Add(new Paragraph(label).SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(value).SetFont(font)));
                }
                AddRow("Поточне використання / Current usage", stats.CurrentUsage.ToString());
                AddRow("Ліміт використання / Usage limit", stats.UsageLimit?.ToString() ?? "-");
                AddRow("Призначено користувачам / Assigned users", stats.AssignedUsers.ToString());
                AddRow("Замовлень / Total orders", stats.TotalOrders.ToString());
                AddRow("Продано одиниць / Total quantity sold", stats.TotalQuantitySold.ToString());
                AddRow("Сума знижки / Total discount given", $"{stats.TotalDiscountGiven:0.00} UAH");
                AddRow("Дохід / Total revenue", $"{stats.TotalRevenue:0.00} UAH");
                document.Add(table);
            }
            return File(stream.ToArray(), "application/pdf", $"promotion_{stats.PromotionId}_statistics_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private static object ToResult(Promotion promo) => new
        {
            promo.PromotionId,
            promo.Name,
            promo.NameEn,
            promo.Description,
            promo.DescriptionEn,
            promo.Type,
            promo.Value,
            promo.TargetType,
            promo.TargetId,
            promo.AudienceType,
            promo.StartDate,
            promo.EndDate,
            promo.PromoCode,
            promo.MinOrderAmount,
            promo.MinQuantity,
            promo.Priority,
            promo.UsageLimit,
            promo.CurrentUsage,
            promo.IsActive,
            promo.CreatedAt,
            promo.UpdatedAt
        };

        // Вибір шрифту з підтримкою кирилиці (Arial/DejaVu) з відкатом на стандартний
        private static PdfFont ResolveFont()
        {
            var windowsFont = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            var candidates = new[]
            {
                windowsFont,
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/System/Library/Fonts/Supplemental/Arial.ttf"
            };
            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    try
                    {
                        return PdfFontFactory.CreateFont(path, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                    catch
                    {
                        // спробувати наступний шрифт
                    }
                }
            }
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
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
        public int? MinQuantity { get; set; }
        public int Priority { get; set; } = 50;
        public int? UsageLimit { get; set; }
    }
}
