using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(AppDbContext context, ILogger<ProductsController> logger, IMemoryCache cache, KhduSouvenirShop.API.Services.PromotionService promotionService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IMemoryCache _cache = cache;
        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService = promotionService;

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            _logger.LogInformation("Запит на отримання всіх товарів");
            // Опримуємо studentStatus (якщо користувач авторизований)
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

            var cacheKey = $"products:all:{studentStatus}";
            var dtoList = _cache.Get<List<object>>(cacheKey);
            if (dtoList is null)
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
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

                _cache.Set(cacheKey, dtoList, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(dtoList ?? new List<object>());
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);

            // Student status for pricing
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
            var dto = _cache.Get<object>(cacheKey);
            if (dto is null)
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    _logger.LogWarning("Товар з ID {ProductId} не знайдено", id);
                    return NotFound(new { error = "Товар не знайдено" });
                }

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

                dto = new
                {
                    productId = product.ProductId,
                    name = product.Name,
                    description = product.Description,
                    category = product.Category != null ? new { categoryId = product.Category.CategoryId, name = product.Category.Name } : null,
                    images = product.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(product, promos),
                    originalPrice = product.Price,
                    stock = product.Stock
                };

                _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(dto);
        }

        // GET: api/Products/search?query=футболка
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchProducts([FromQuery] string query)
        {
            _logger.LogInformation("Пошук товарів за запитом: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Пошуковий запит не може бути порожнім" });
            }

            var norm = query.Trim().ToLowerInvariant();

            // student status
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
            var dtoList = _cache.Get<List<object>>(cacheKey);
            if (dtoList is null)
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
                    images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(p, promos),
                    originalPrice = p.Price,
                    stock = p.Stock
                }).ToList<object>();

                _cache.Set(cacheKey, dtoList, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                });
            }

            return Ok(dtoList ?? new List<object>());
        }

        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsByCategory(int categoryId)
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
            var dtoList = _cache.Get<List<object>>(cacheKey);
            if (dtoList is null)
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

                _cache.Set(cacheKey, dtoList, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(dtoList ?? new List<object>());
        }
    }
}
