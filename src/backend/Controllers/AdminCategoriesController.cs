using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;

using System.Text.Json;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/categories")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class AdminCategoriesController(ICategoryRepository categoryRepository, ILogger<AdminCategoriesController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository = categoryRepository;
        private readonly ILogger<AdminCategoriesController> _logger = logger;
        private readonly IMemoryCache _cache = cache;
        private void InvalidateCache()
        {
            _cache.Remove("Public_Categories_Tree");
            _cache.Remove("Public_Products_All");
            // Інкрементування версії товарів, бо вони залежать від категорій
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
                var parentExists = await _categoryRepository.Query().AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
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
            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();
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
            var query = _categoryRepository.Query().IgnoreQueryFilters().Where(c => includeDeleted || !c.IsDeleted).AsQueryable();
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
            var category = await _categoryRepository.Query()
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
            var category = await _categoryRepository.GetByIdAsync(id);
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
                var parentExists = await _categoryRepository.Query().AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Батьківська категорія не знайдена", "NotFound"));
                }
            }
            category.Name = string.IsNullOrWhiteSpace(dto.Name) ? category.Name : dto.Name.Trim();
            category.ParentCategoryId = dto.ParentCategoryId;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію оновлено"));
        }
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PatchCategory(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
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
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію частково оновлено"));
        }
        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var categories = await _categoryRepository.Query()
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
                _categoryRepository.Remove(category);
                deletedCount++;
            }
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount, skippedCount }, $"Видалено {deletedCount} категорій. Пропущено {skippedCount} (мають товари або підкатегорії)"));
        }
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RestoreCategory(int id)
        {
            var category = await _categoryRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено"));
            category.IsDeleted = false;
            category.DeletedAt = null;
            category.DeletedBy = null;
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію відновлено"));
        }
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _categoryRepository.Query()
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }
            if (category.SubCategories.Count > 0)
            {
                return Conflict(ApiResponse<object>.FailureResult("Спочатку видаліть підкатегорії", "Conflict"));
            }
            if (category.Products.Count > 0)
            {
                return Conflict(ApiResponse<object>.FailureResult("Категорія містить товари. Видаліть або перемістіть товари", "Conflict"));
            }
            _categoryRepository.Remove(category);
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію видалено"));
        }
        [HttpPost("reorder")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ReorderCategories([FromBody] List<CategoryReorderDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Список для зміни порядку порожній", "BadRequest"));
            }
            var ids = items.Select(i => i.CategoryId).Distinct().ToList();
            var categories = await _categoryRepository.Query()
                .Where(c => ids.Contains(c.CategoryId))
                .ToListAsync();
            var orderMap = items.GroupBy(i => i.CategoryId).ToDictionary(g => g.Key, g => g.Last().DisplayOrder);
            foreach (var category in categories)
            {
                if (orderMap.TryGetValue(category.CategoryId, out var order))
                {
                    category.DisplayOrder = order;
                }
            }
            await _categoryRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { updated = categories.Count }, "Порядок категорій оновлено"));
        }
    }
    public class AdminCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
    public class CategoryReorderDto
    {
        public int CategoryId { get; set; }
        public int DisplayOrder { get; set; }
    }
}