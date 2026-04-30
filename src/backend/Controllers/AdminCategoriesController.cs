using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

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

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Категорію оновлено"));
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
