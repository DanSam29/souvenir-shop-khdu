using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

using System.Text.Json;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/categories")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator")]
    public class AdminCategoriesController(AppDbContext context, ILogger<AdminCategoriesController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<AdminCategoriesController> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        private void InvalidateCache()
        {
            _cache.Remove("Public_Categories_Tree");
            _cache.Remove("Public_Products_All");
            
            // Також інкрементуємо версію товарів, бо вони залежать від категорій
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateCategory([FromBody] AdminCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Назва категорії обов'язкова", "BadRequest"));
            }

            if (dto.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Батьківська категорія не знайдена", "NotFound"));
                }
            }

            var category = new Category
            {
                Name = dto.Name.Trim(),
                ParentCategoryId = dto.ParentCategoryId,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            InvalidateCache();

            var result = new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                parentCategoryId = category.ParentCategoryId,
                description = category.Description,
                displayOrder = category.DisplayOrder
            };

            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, ApiResponse<object>.SuccessResult(result, "Категорію створено"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCategories([FromQuery] bool includeDeleted = false)
        {
            var query = _context.Categories.IgnoreQueryFilters().Where(c => includeDeleted || !c.IsDeleted).AsQueryable();
            
            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    c.NameEn,
                    c.ParentCategoryId,
                    c.DisplayOrder,
                    c.IsDeleted,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(categories));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            var result = new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                parentCategoryId = category.ParentCategoryId,
                description = category.Description,
                displayOrder = category.DisplayOrder,
                subCategoriesCount = category.SubCategories.Count,
                productsCount = category.Products.Count
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] AdminCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            if (dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value == id)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Категорія не може бути власним батьком", "BadRequest"));
            }

            if (dto.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Батьківська категорія не знайдена", "NotFound"));
                }
            }

            category.Name = string.IsNullOrWhiteSpace(dto.Name) ? category.Name : dto.Name.Trim();
            category.ParentCategoryId = dto.ParentCategoryId;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;

            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію оновлено"));
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PatchCategory(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено"));

            foreach (var update in updates)
            {
                var value = update.Value;
                switch (update.Key.ToLower())
                {
                    case "name": category.Name = value.GetString() ?? category.Name; break;
                    case "nameen": category.NameEn = value.GetString(); break;
                    case "description": category.Description = value.GetString(); break;
                    case "displayorder": 
                        if (value.ValueKind == JsonValueKind.Number) category.DisplayOrder = value.GetInt32();
                        else if (int.TryParse(value.GetString(), out var o)) category.DisplayOrder = o;
                        break;
                    case "parentcategoryid": 
                        if (value.ValueKind == JsonValueKind.Null) category.ParentCategoryId = null;
                        else if (value.ValueKind == JsonValueKind.Number) category.ParentCategoryId = value.GetInt32();
                        else if (int.TryParse(value.GetString(), out var p)) category.ParentCategoryId = p;
                        break;
                }
            }

            await _context.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію частково оновлено"));
        }

        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .Where(c => ids.Contains(c.CategoryId))
                .ToListAsync();

            var deletedCount = 0;
            var skippedCount = 0;

            foreach (var category in categories)
            {
                if (category.Products.Count > 0 || category.SubCategories.Count > 0)
                {
                    skippedCount++;
                    continue;
                }
                _context.Categories.Remove(category);
                deletedCount++;
            }

            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount, skippedCount }, $"Видалено {deletedCount} категорій. Пропущено {skippedCount} (мають товари або підкатегорії)"));
        }

        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RestoreCategory(int id)
        {
            var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено"));

            category.IsDeleted = false;
            category.DeletedAt = null;
            category.DeletedBy = null;

            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію відновлено"));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            if (category.SubCategories.Count > 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Спочатку видаліть підкатегорії", "BadRequest"));
            }

            if (category.Products.Count > 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Категорія містить товари. Видаліть або перемістіть товари", "BadRequest"));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію видалено"));
        }
    }

    public class AdminCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}
