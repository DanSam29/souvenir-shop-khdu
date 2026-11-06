using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Публічний контролер категорій каталогу (доступний усім користувачам).
    /// Адміністративне керування категоріями винесено в AdminCategoriesController.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private const string CategoriesTreeCacheKey = "Public_Categories_Tree";
        private const string ProductsCacheVersionKey = "Products_Cache_Version";

        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly PromotionService _promotionService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            PromotionService promotionService,
            IMemoryCache cache,
            ILogger<CategoriesController> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _promotionService = promotionService;
            _cache = cache;
            _logger = logger;
        }

        // GET: api/categories
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryTreeResponse>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCategories()
        {
            if (_cache.TryGetValue(CategoriesTreeCacheKey, out List<CategoryTreeResponse>? cachedTree))
            {
                return Ok(ApiResponse<IEnumerable<CategoryTreeResponse>>.SuccessResult(cachedTree!));
            }

            var allCategories = await _categoryRepository.Query()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var tree = allCategories
                .Where(c => c.ParentCategoryId == null)
                .Select(c => BuildCategoryNode(c, allCategories))
                .ToList();

            _cache.Set(CategoriesTreeCacheKey, tree, TimeSpan.FromMinutes(5));
            return Ok(ApiResponse<IEnumerable<CategoryTreeResponse>>.SuccessResult(tree));
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryTreeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCategory(int id)
        {
            var allCategories = await _categoryRepository.Query()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var category = allCategories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null)
            {
                _logger.LogWarning("Категорію з ID {CategoryId} не знайдено", id);
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            return Ok(ApiResponse<CategoryTreeResponse>.SuccessResult(BuildCategoryNode(category, allCategories)));
        }

        // GET: api/categories/5/products
        [HttpGet("{id}/products")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCategoryProducts(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var categoryExists = await _categoryRepository.Query().AnyAsync(c => c.CategoryId == id);
            if (!categoryExists)
            {
                _logger.LogWarning("Категорію з ID {CategoryId} не знайдено", id);
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            var studentStatus = await GetCurrentStudentStatusAsync();
            var cacheVersion = _cache.Get<int>(ProductsCacheVersionKey);
            var cacheKey = $"v{cacheVersion}_cat:{id}:products:{page}:{pageSize}:{sortBy}:{minPrice}:{maxPrice}:{inStock}:{studentStatus}";

            if (!_cache.TryGetValue(cacheKey, out PagedResponse<object>? paged))
            {
                var categoryIds = await GetCategoryIdsWithDescendantsAsync(id);

                var query = _productRepository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => categoryIds.Contains(p.CategoryId));

                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);
                if (inStock == true)
                    query = query.Where(p => p.Stock > 0);

                var totalItems = await query.CountAsync();

                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "name_asc" => query.OrderBy(p => p.Name),
                    "name_desc" => query.OrderByDescending(p => p.Name),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);
                var items = products.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    nameEn = p.NameEn,
                    description = p.Description,
                    descriptionEn = p.DescriptionEn,
                    category = p.Category != null ? new
                    {
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
                }).ToList<object>();

                paged = new PagedResponse<object>(items, totalItems, page, pageSize);
                _cache.Set(cacheKey, paged, TimeSpan.FromMinutes(3));
            }

            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(paged!));
        }

        private CategoryTreeResponse BuildCategoryNode(Category category, List<Category> all) => new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            NameEn = category.NameEn,
            Description = category.Description,
            DescriptionEn = category.DescriptionEn,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            SubCategories = all
                .Where(c => c.ParentCategoryId == category.CategoryId)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => BuildCategoryNode(c, all))
                .ToList()
        };

        // Збирає ідентифікатори категорії та всіх її нащадків (для вибірки товарів усього піддерева)
        private async Task<List<int>> GetCategoryIdsWithDescendantsAsync(int categoryId)
        {
            var all = await _categoryRepository.Query()
                .Select(c => new { c.CategoryId, c.ParentCategoryId })
                .ToListAsync();

            var result = new List<int> { categoryId };
            var queue = new Queue<int>();
            queue.Enqueue(categoryId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in all.Where(c => c.ParentCategoryId == current))
                {
                    if (result.Contains(child.CategoryId)) continue;
                    result.Add(child.CategoryId);
                    queue.Enqueue(child.CategoryId);
                }
            }
            return result;
        }

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
