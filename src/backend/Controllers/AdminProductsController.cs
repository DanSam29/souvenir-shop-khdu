using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

using System.Globalization;
using System.Text.Json;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator")]
    public class AdminProductsController(AppDbContext context, ILogger<AdminProductsController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<AdminProductsController> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }

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
                NameEn = dto.NameEn?.Trim(),
                Description = dto.Description ?? string.Empty,
                DescriptionEn = dto.DescriptionEn,
                Price = dto.Price,
                Weight = dto.Weight,
                CategoryId = dto.CategoryId,
                Stock = dto.Stock
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            InvalidateCache();

            var result = new
            {
                productId = product.ProductId,
                name = product.Name,
                nameEn = product.NameEn,
                description = product.Description,
                descriptionEn = product.DescriptionEn,
                price = product.Price,
                weight = product.Weight,
                categoryId = product.CategoryId,
                stock = product.Stock
            };

            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, ApiResponse<object>.SuccessResult(result, "Товар створено"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] bool descending = false,
            [FromQuery] bool? inStock = null)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || (p.NameEn != null && p.NameEn.ToLower().Contains(s)));
            }

            if (inStock.HasValue)
                query = inStock.Value ? query.Where(p => p.Stock > 0) : query.Where(p => p.Stock == 0);

            query = sortBy?.ToLower() switch
            {
                "price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "stock" => descending ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
                "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                _ => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            };

            var count = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.NameEn,
                    p.Price,
                    p.Stock,
                    p.CategoryId,
                    categoryName = p.Category.Name,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
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
                nameEn = product.NameEn,
                description = product.Description,
                descriptionEn = product.DescriptionEn,
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
            product.NameEn = dto.NameEn?.Trim();
            product.Description = dto.Description ?? product.Description;
            product.DescriptionEn = dto.DescriptionEn;
            product.Price = dto.Price > 0 ? dto.Price : product.Price;
            product.Weight = dto.Weight > 0 ? dto.Weight : product.Weight;
            product.CategoryId = dto.CategoryId;
            product.Stock = dto.Stock >= 0 ? dto.Stock : product.Stock;

            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар оновлено"));
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PatchProduct(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено"));

            foreach (var update in updates)
            {
                var value = update.Value;
                switch (update.Key.ToLower())
                {
                    case "name": product.Name = value.GetString() ?? product.Name; break;
                    case "nameen": product.NameEn = value.GetString(); break;
                    case "price": 
                        if (value.ValueKind == JsonValueKind.Number) product.Price = value.GetDecimal();
                        else if (decimal.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var p)) product.Price = p;
                        break;
                    case "stock": 
                        if (value.ValueKind == JsonValueKind.Number) product.Stock = value.GetInt32();
                        else if (int.TryParse(value.GetString(), out var s)) product.Stock = s;
                        break;
                    case "categoryid": 
                        if (value.ValueKind == JsonValueKind.Number) product.CategoryId = value.GetInt32();
                        else if (int.TryParse(value.GetString(), out var c)) product.CategoryId = c;
                        break;
                }
            }

            await _context.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар частково оновлено"));
        }

        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var products = await _context.Products
                .Include(p => p.OrderItems)
                .Where(p => ids.Contains(p.ProductId))
                .ToListAsync();

            var deletedCount = 0;
            var skippedCount = 0;

            foreach (var product in products)
            {
                if (product.OrderItems.Count > 0)
                {
                    skippedCount++;
                    continue;
                }
                _context.Products.Remove(product);
                deletedCount++;
            }

            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount, skippedCount }, $"Видалено {deletedCount} товарів. Пропущено {skippedCount} (мають замовлення)"));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            if (product.OrderItems.Count > 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Неможливо видалити товар, який фігурує у замовленнях. Він буде прихований (Soft Delete) автоматично, якщо ви просто спробуєте видалити, але ми додамо перевірку", "BadRequest"));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            InvalidateCache();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар видалено"));
        }
    }

    public class ProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public int CategoryId { get; set; }
        public int Stock { get; set; } = 0;
    }
}
