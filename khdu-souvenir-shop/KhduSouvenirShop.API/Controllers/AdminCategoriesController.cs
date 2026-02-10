using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/categories")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator")]
    public class AdminCategoriesController(AppDbContext context, ILogger<AdminCategoriesController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<AdminCategoriesController> _logger = logger;

        [HttpPost]
        public async Task<ActionResult> CreateCategory([FromBody] AdminCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = "Назва категорії обов'язкова" });
            }

            if (dto.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest(new { error = "Батьківська категорія не знайдена" });
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

            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                parentCategoryId = category.ParentCategoryId,
                description = category.Description,
                displayOrder = category.DisplayOrder
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = "Категорію не знайдено" });
            }

            return Ok(new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                parentCategoryId = category.ParentCategoryId,
                description = category.Description,
                displayOrder = category.DisplayOrder,
                subCategoriesCount = category.SubCategories.Count,
                productsCount = category.Products.Count
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] AdminCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { error = "Категорію не знайдено" });
            }

            if (dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value == id)
            {
                return BadRequest(new { error = "Категорія не може бути власним батьком" });
            }

            if (dto.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return BadRequest(new { error = "Батьківська категорія не знайдена" });
                }
            }

            category.Name = string.IsNullOrWhiteSpace(dto.Name) ? category.Name : dto.Name.Trim();
            category.ParentCategoryId = dto.ParentCategoryId;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорію оновлено" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = "Категорію не знайдено" });
            }

            if (category.SubCategories.Count > 0)
            {
                return BadRequest(new { error = "Спочатку видаліть підкатегорії" });
            }

            if (category.Products.Count > 0)
            {
                return BadRequest(new { error = "Категорія містить товари. Видаліть або перемістіть товари" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорію видалено" });
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
