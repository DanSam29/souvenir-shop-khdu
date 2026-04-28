using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator")]
    public class AdminProductsController(AppDbContext context, ILogger<AdminProductsController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<AdminProductsController> _logger = logger;

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateProduct([FromBody] ProductDto dto)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                Weight = dto.Weight,
                CategoryId = dto.CategoryId,
                Stock = dto.Stock
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = new
            {
                productId = product.ProductId,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                weight = product.Weight,
                categoryId = product.CategoryId,
                stock = product.Stock
            };

            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, ApiResponse<object>.SuccessResult(result, "Товар створено"));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            var result = new
            {
                productId = product.ProductId,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                weight = product.Weight,
                categoryId = product.CategoryId,
                stock = product.Stock
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            if (dto.CategoryId != product.CategoryId)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
                if (!categoryExists)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
                }
            }

            product.Name = string.IsNullOrWhiteSpace(dto.Name) ? product.Name : dto.Name.Trim();
            product.Description = dto.Description ?? product.Description;
            product.Price = dto.Price > 0 ? dto.Price : product.Price;
            product.Weight = dto.Weight > 0 ? dto.Weight : product.Weight;
            product.CategoryId = dto.CategoryId;
            product.Stock = dto.Stock >= 0 ? dto.Stock : product.Stock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар оновлено"));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            if (product.Images.Count > 0)
            {
                _context.ProductImages.RemoveRange(product.Images);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар видалено"));
        }
    }

    public class ProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public int CategoryId { get; set; }
        public int Stock { get; set; } = 0;
    }
}
