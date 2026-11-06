using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Публічний контролер каталогу товарів (доступний усім користувачам).
    /// Адміністративне керування товарами та зображеннями винесено в AdminProductsController.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private const string ProductsCacheVersionKey = "Products_Cache_Version";

        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService;
        public ProductsController(IProductRepository productRepository, IUserRepository userRepository, ILogger<ProductsController> logger, IMemoryCache cache, KhduSouvenirShop.API.Services.PromotionService promotionService)
        {
            _productRepository = productRepository;
            _userRepository = userRepository;
            _logger = logger;
            _cache = cache;
            _promotionService = promotionService;
        }
        // GET: api/Products
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation("Запит на отримання товарів з фільтрами");
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_Products_{categoryId}_{search}_{sortBy}_{minPrice}_{maxPrice}_{inStock}_{page}_{pageSize}_{studentStatus}";

            if (!_cache.TryGetValue(cacheKey, out PagedResponse<object>? paged))
            {
                var query = _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .AsQueryable();

                // Фільтрація
                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(p => p.Name.Contains(term) || p.Description.Contains(term) ||
                                            (p.NameEn != null && p.NameEn.Contains(term)) ||
                                            (p.DescriptionEn != null && p.DescriptionEn.Contains(term)));
                }
                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);
                if (inStock == true)
                    query = query.Where(p => p.Stock > 0);

                var totalItems = await query.CountAsync();

                query = ApplySorting(query, sortBy);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                var items = products.Select(p => BuildProductDto(p, promos)).ToList();

                paged = new PagedResponse<object>(items, totalItems, page, pageSize);
                _cache.Set(cacheKey, paged, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(paged!));
        }
        // GET: api/Products/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);
            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_product:{id}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out object? dto))
            {
                var product = await _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null)
                {
                    _logger.LogWarning("Товар з ID {ProductId} не знайдено", id);
                    return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
                }
                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                // Пов'язані товари: інші товари тієї ж категорії
                var related = await _productRepository.Query()
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(4)
                    .ToListAsync();
                dto = new
                {
                    productId = product.ProductId,
                    name = product.Name,
                    nameEn = product.NameEn,
                    description = product.Description,
                    descriptionEn = product.DescriptionEn,
                    category = product.Category != null ? new { 
                        categoryId = product.Category.CategoryId, 
                        name = product.Category.Name,
                        nameEn = product.Category.NameEn
                    } : null,
                    categoryId = product.CategoryId,
                    images = product.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
                    price = _promotionService.GetPriceAfterPromotions(product, promos),
                    originalPrice = product.Price,
                    weight = product.Weight,
                    createdAt = product.CreatedAt,
                    stock = product.Stock,
                    relatedProducts = related.Select(r => new
                    {
                        productId = r.ProductId,
                        name = r.Name,
                        nameEn = r.NameEn,
                        price = _promotionService.GetPriceAfterPromotions(r, promos),
                        originalPrice = r.Price,
                        stock = r.Stock,
                        primaryImage = r.Images.Where(i => i.IsPrimary).Select(i => i.ImageURL).FirstOrDefault()
                                       ?? r.Images.Select(i => i.ImageURL).FirstOrDefault()
                    }).ToList()
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
            var term = query.Trim();
            var norm = term.ToLowerInvariant();
            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_products:search:{norm}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var products = await _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.Name.Contains(term) || p.Description.Contains(term) || 
                                (p.NameEn != null && p.NameEn.Contains(term)) || 
                                (p.DescriptionEn != null && p.DescriptionEn.Contains(term)))
                    .ToListAsync();
                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                dtoList = products.Select(p => BuildProductDto(p, promos)).ToList();
                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(2));
            }
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }
        // GET: api/Products/featured?limit=8
        [HttpGet("featured")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetFeaturedProducts([FromQuery] int limit = 8)
        {
            _logger.LogInformation("Запит на рекомендовані товари");
            if (limit < 1) limit = 8;
            if (limit > 50) limit = 50;

            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_products:featured:{limit}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                // Популярні: за сумарною кількістю проданих одиниць, далі - найновіші
                var products = await _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                dtoList = products.Select(p => BuildProductDto(p, promos)).ToList();
                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }
        // GET: api/Products/new?limit=8
        [HttpGet("new")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetNewProducts([FromQuery] int limit = 8)
        {
            _logger.LogInformation("Запит на новинки каталогу");
            if (limit < 1) limit = 8;
            if (limit > 50) limit = 50;

            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_products:new:{limit}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var products = await _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                dtoList = products.Select(p => BuildProductDto(p, promos)).ToList();
                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }
        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", categoryId);
            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_products:category:{categoryId}:{studentStatus}";
            if (!_cache.TryGetValue(cacheKey, out List<object>? dtoList))
            {
                var products = await _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == categoryId)
                    .ToListAsync();
                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                dtoList = products.Select(p => BuildProductDto(p, promos)).ToList();
                _cache.Set(cacheKey, dtoList, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<IEnumerable<object>>.SuccessResult(dtoList ?? new List<object>()));
        }

        // Проєкція товару для публічних списків каталогу (єдиний формат для всіх GET-методів)
        private object BuildProductDto(Product p, List<Promotion> promos) => new
        {
            productId = p.ProductId,
            name = p.Name,
            nameEn = p.NameEn,
            description = p.Description,
            descriptionEn = p.DescriptionEn,
            category = p.Category != null ? new { 
                categoryId = p.Category.CategoryId, 
                name = p.Category.Name,
                nameEn = p.Category.NameEn
            } : null,
            categoryId = p.CategoryId,
            images = p.Images.Select(i => new { imageId = i.ImageId, imageURL = i.ImageURL, isPrimary = i.IsPrimary }).ToList(),
            price = _promotionService.GetPriceAfterPromotions(p, promos),
            originalPrice = p.Price,
            weight = p.Weight,
            createdAt = p.CreatedAt,
            stock = p.Stock
        };

        // Застосування сортування списку товарів
        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy) => sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "popular" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // Визначає студентський статус поточного користувача (для розрахунку знижок)
        private async Task<string> GetCurrentStudentStatusAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return "NONE";

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return "NONE";

            var user = await _userRepository.GetByIdAsync(userId);
            return user?.StudentStatus ?? "NONE";
        }
    }
}
