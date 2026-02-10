using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

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
        public async Task<ActionResult> CreateProduct([FromBody] ProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = "Назва товару обов'язкова" });
            }

            if (dto.Price <= 0 || dto.Weight <= 0)
            {
                return BadRequest(new { error = "Ціна та вага мають бути більше 0" });
            }

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { error = "Категорію не знайдено" });
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

            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, new
            {
                productId = product.ProductId,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                weight = product.Weight,
                categoryId = product.CategoryId,
                stock = product.Stock
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(new { error = "Товар не знайдено" });
            }

            return Ok(new
            {
                productId = product.ProductId,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                weight = product.Weight,
                categoryId = product.CategoryId,
                stock = product.Stock
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Товар не знайдено" });
            }

            if (dto.CategoryId != product.CategoryId)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
                if (!categoryExists)
                {
                    return BadRequest(new { error = "Категорію не знайдено" });
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

            return Ok(new { message = "Товар оновлено" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(new { error = "Товар не знайдено" });
            }

            if (product.Images.Count > 0)
            {
                _context.ProductImages.RemoveRange(product.Images);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Товар видалено" });
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
