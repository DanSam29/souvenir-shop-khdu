using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService;
        private readonly KhduSouvenirShop.API.Services.IImageService _imageService;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger, IMemoryCache cache, KhduSouvenirShop.API.Services.PromotionService promotionService, KhduSouvenirShop.API.Services.IImageService imageService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _promotionService = promotionService;
            _imageService = imageService;
        }

        // GET: api/Products
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProducts(
            [FromQuery] int? categoryId, 
            [FromQuery] string? search, 
            [FromQuery] string? sortBy = "newest",
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            _logger.LogInformation("Запит на отримання товарів з фільтрами");
            
            // Спочатку визначаємо studentStatus
            string studentStatus = "NONE";
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null) studentStatus = user.StudentStatus ?? "NONE";
                }
            }
            
            // Ключ кешу залежить від фільтрів ТА studentStatus
            string cacheKey = $"Products_{categoryId}_{search}_{sortBy}_{minPrice}_{maxPrice}_{studentStatus}";
            
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .AsQueryable();

                // Фільтрація
                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);

                // Сортування
                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "name_asc" => query.OrderBy(p => p.Name),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                var products = await query.ToListAsync();

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

                dtoList = products.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category != null ? new { categoryId = p.Category.CategoryId, name = p.Category.Name } : null,
                    categoryId = p.CategoryId,
                    images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(p, promos),
                    originalPrice = p.Price,
                    weight = p.Weight,
                    createdAt = p.CreatedAt,
                    stock = p.Stock
                }).ToList<object>();

                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(5));
            }

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);

            string studentStatus = "NONE";
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null) studentStatus = user.StudentStatus ?? "NONE";
                }
            }

            var cacheKey = $"product:{id}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out object? dto))
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    _logger.LogWarning("Товар з ID {ProductId} не знайдено", id);
                    return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
                }

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

                dto = new
                {
                    productId = product.ProductId,
                    name = product.Name,
                    description = product.Description,
                    category = product.Category != null ? new { categoryId = product.Category.CategoryId, name = product.Category.Name } : null,
                    categoryId = product.CategoryId,
                    images = product.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(product, promos),
                    originalPrice = product.Price,
                    weight = product.Weight,
                    createdAt = product.CreatedAt,
                    stock = product.Stock
                };

                _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
            }

            if (dto == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            return Ok(ApiResponse<object>.SuccessResult(dto));
        }

        // GET: api/Products/search?query=футболка
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SearchProducts([FromQuery] string query)
        {
            _logger.LogInformation("Пошук товарів за запитом: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Пошуковий запит не може бути порожнім", "BadRequest"));
            }

            var norm = query.Trim().ToLowerInvariant();

            string studentStatus = "NONE";
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null) studentStatus = user.StudentStatus ?? "NONE";
                }
            }

            var cacheKey = $"products:search:{norm}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                    .ToListAsync();

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                dtoList = products.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category != null ? new { categoryId = p.Category.CategoryId, name = p.Category.Name } : null,
                    categoryId = p.CategoryId,
                    images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(p, promos),
                    originalPrice = p.Price,
                    weight = p.Weight,
                    createdAt = p.CreatedAt,
                    stock = p.Stock
                }).ToList<object>();

                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(2));
            }

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }

        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", categoryId);
            string studentStatus = "NONE";
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null) studentStatus = user.StudentStatus ?? "NONE";
                }
            }

            var cacheKey = $"products:category:{categoryId}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == categoryId)
                    .ToListAsync();

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

                dtoList = products.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category != null ? new { categoryId = p.Category.CategoryId, name = p.Category.Name } : null,
                    images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(p, promos),
                    originalPrice = p.Price,
                    stock = p.Stock
                }).ToList<object>();

                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(5));
            }

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }

        // --- Admin Methods ---

        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            var product = new Product
            {
                Name = dto.Name!,
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                Weight = dto.Weight,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(product, "Товар створено"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));

            product.Name = dto.Name!;
            product.Description = dto.Description ?? string.Empty;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.Weight = dto.Weight;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            InvalidateCache();
            return Ok(ApiResponse<object>.SuccessResult(product, "Товар оновлено"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));

            // Видалення зображень з диска
            foreach (var img in product.Images)
            {
                await _imageService.DeleteImageAsync(img.ImageURL);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            InvalidateCache();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Товар видалено"));
        }

        [HttpPost("{id}/images")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> UploadImage(int id, IFormFile file, [FromQuery] bool isPrimary = false)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));

            try
            {
                var url = await _imageService.UploadImageAsync(file);

                // Якщо це перше зображення, робимо його головним автоматично
                if (!product.Images.Any()) isPrimary = true;

                // Якщо ми ставимо нове головне зображення, знімаємо прапорець з інших
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

                _context.ProductImages.Add(productImage);
                await _context.SaveChangesAsync();

                InvalidateCache();
                return Ok(ApiResponse<object>.SuccessResult(productImage, "Зображення додано"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завантаженні зображення");
                return BadRequest(ApiResponse<object>.FailureResult("Помилка при завантаженні зображення", "UploadError"));
            }
        }

        [HttpDelete("images/{imageId}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> DeleteImage(int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null) return NotFound(ApiResponse<object>.FailureResult("Зображення не знайдено", "NotFound"));

            await _imageService.DeleteImageAsync(img.ImageURL);
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();

            InvalidateCache();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Зображення видалено"));
        }

        private void InvalidateCache()
        {
            _cache.Remove("Public_Products_All");
            // В ідеалі тут треба видалити всі ключі, що починаються з Products_
        }
    }

    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public decimal Weight { get; set; } = 0.5m;
    }

    public class ProductUpdateDto : ProductCreateDto { }
}
