using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

using System.Globalization;
using System.Text.Json;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class AdminProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository, IRepository<ProductImage> productImageRepository, IImageService imageService, ILogger<AdminProductsController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly ICategoryRepository _categoryRepository = categoryRepository;
        private readonly IRepository<ProductImage> _productImageRepository = productImageRepository;
        private readonly IImageService _imageService = imageService;
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
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
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
            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();
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
            var query = _productRepository.Query().Include(p => p.Category).AsQueryable();
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
            var product = await _productRepository.Query()
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
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            if (dto.CategoryId != product.CategoryId)
            {
                var categoryExists = await _categoryRepository.Query().AnyAsync(c => c.CategoryId == dto.CategoryId);
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
            await _productRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар оновлено"));
        }
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PatchProduct(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var product = await _productRepository.GetByIdAsync(id);
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
            await _productRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар частково оновлено"));
        }
        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var products = await _productRepository.Query()
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
                _productRepository.Remove(product);
                deletedCount++;
            }
            await _productRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount, skippedCount }, $"Видалено {deletedCount} товарів. Пропущено {skippedCount} (мають замовлення)"));
        }
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _productRepository.Query()
                .Include(p => p.Images)
                .Include(p => p.OrderItems).ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            // Блокуємо soft delete лише за наявності активних замовлень (не Delivered і не Cancelled)
            var hasActiveOrders = product.OrderItems.Any(oi =>
                oi.Order != null && oi.Order.Status != "Delivered" && oi.Order.Status != "Cancelled");
            if (hasActiveOrders)
            {
                return Conflict(ApiResponse<object>.FailureResult("Неможливо видалити товар, що входить до активних замовлень", "Conflict"));
            }
            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар видалено"));
        }
        [HttpPost("{id}/images")]
        [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UploadImage(int id, IFormFile file, [FromQuery] bool isPrimary = false)
        {
            var product = await _productRepository.Query()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            try
            {
                var url = await _imageService.UploadImageAsync(file);
                // Якщо це перше зображення - автоматично стає головним
                if (!product.Images.Any()) isPrimary = true;
                // Нове головне зображення знімає цей статус з усіх інших
                if (isPrimary)
                {
                    foreach (var img in product.Images) img.IsPrimary = false;
                }
                var productImage = new ProductImage
                {
                    ProductId = id,
                    ImageURL = url,
                    IsPrimary = isPrimary,
                    DisplayOrder = product.Images.Count
                };
                await _productImageRepository.AddAsync(productImage);
                await _productImageRepository.SaveChangesAsync();
                InvalidateCache();
                var result = new
                {
                    imageId = productImage.ImageId,
                    imageURL = productImage.ImageURL,
                    isPrimary = productImage.IsPrimary,
                    displayOrder = productImage.DisplayOrder
                };
                return Ok(ApiResponse<object>.SuccessResult(result, "Зображення додано"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message, "BadRequest"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завантаженні зображення для товару {ProductId}", id);
                return BadRequest(ApiResponse<object>.FailureResult("Помилка при завантаженні зображення", "UploadError"));
            }
        }
        [HttpPut("{id}/images/reorder")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ReorderImages(int id, [FromBody] List<ImageReorderDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Список для зміни порядку порожній", "BadRequest"));
            }
            var product = await _productRepository.Query()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            var orderMap = items.GroupBy(i => i.ImageId).ToDictionary(g => g.Key, g => g.Last().DisplayOrder);
            foreach (var image in product.Images)
            {
                if (orderMap.TryGetValue(image.ImageId, out var order))
                {
                    image.DisplayOrder = order;
                }
            }
            await _productImageRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { updated = product.Images.Count }, "Порядок зображень оновлено"));
        }
        [HttpPut("{id}/images/{imageId}/primary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SetPrimaryImage(int id, int imageId)
        {
            var product = await _productRepository.Query()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            var image = product.Images.FirstOrDefault(i => i.ImageId == imageId);
            if (image == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Зображення не знайдено", "NotFound"));
            }
            foreach (var img in product.Images) img.IsPrimary = img.ImageId == imageId;
            await _productImageRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Головне зображення оновлено"));
        }
        [HttpDelete("{id}/images/{imageId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteImage(int id, int imageId)
        {
            var product = await _productRepository.Query()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }
            var image = product.Images.FirstOrDefault(i => i.ImageId == imageId);
            if (image == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Зображення не знайдено", "NotFound"));
            }
            await _imageService.DeleteImageAsync(image.ImageURL);
            var wasPrimary = image.IsPrimary;
            _productImageRepository.Remove(image);
            // Якщо видалили головне зображення - призначаємо головним наступне
            if (wasPrimary)
            {
                var next = product.Images.FirstOrDefault(i => i.ImageId != imageId);
                if (next != null) next.IsPrimary = true;
            }
            await _productImageRepository.SaveChangesAsync();
            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Зображення видалено"));
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
    public class ImageReorderDto
    {
        public int ImageId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
