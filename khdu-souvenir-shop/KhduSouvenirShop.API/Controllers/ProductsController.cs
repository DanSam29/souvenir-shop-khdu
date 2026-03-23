using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
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
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProducts()
        {
            _logger.LogInformation("Запит на отримання всіх товарів");
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
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
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
                    images = product.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(product, promos),
                    originalPrice = product.Price,
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
                    images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(p, promos),
                    originalPrice = p.Price,
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
    }
}
