using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext context, IMemoryCache cache, ILogger<CategoriesController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // --- Public Methods ---

        [HttpGet]
        public async Task<ActionResult> GetCategories()
        {
            var cacheKey = "Public_Categories_Tree";
            if (_cache.TryGetValue(cacheKey, out List<object>? cachedTree))
            {
                return Ok(ApiResponse<object>.SuccessResult(cachedTree!));
            }

            var allCategories = await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // Побудова дерева категорій
            var tree = allCategories
                .Where(c => c.ParentCategoryId == null)
                .Select(c => BuildCategoryNode(c, allCategories))
                .ToList<object>();

            _cache.Set(cacheKey, tree, TimeSpan.FromHours(1));

            return Ok(ApiResponse<object>.SuccessResult(tree));
        }

        private object BuildCategoryNode(Category category, List<Category> all)
        {
            return new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                description = category.Description,
                displayOrder = category.DisplayOrder,
                subCategories = all
                    .Where(c => c.ParentCategoryId == category.CategoryId)
                    .Select(c => BuildCategoryNode(c, all))
                    .ToList()
            };
        }

        // --- Admin Methods ---

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> CreateCategory([FromBody] CategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentCategoryId = dto.ParentCategoryId,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(category, "Категорію створено"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ParentCategoryId = dto.ParentCategoryId;
            category.DisplayOrder = dto.DisplayOrder;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(category, "Категорію оновлено"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));

            if (category.Products.Any())
                return BadRequest(ApiResponse<object>.FailureResult("Неможливо видалити категорію, що містить товари", "Conflict"));

            if (category.SubCategories.Any())
                return BadRequest(ApiResponse<object>.FailureResult("Неможливо видалити категорію, що містить підкатегорії", "Conflict"));

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            InvalidateCache();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Категорію видалено"));
        }

        private void InvalidateCache()
        {
            _cache.Remove("Public_Categories_Tree");
            _cache.Remove("Public_Products_All"); // Про всяк випадок, бо товари пов'язані з категоріями
        }
    }

    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}