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
    public class CategoriesController(AppDbContext context, ILogger<CategoriesController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<CategoriesController> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        // GET: api/Categories
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Category>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCategories()
        {
            _logger.LogInformation("Запит на отримання всіх категорій");

            var cacheKey = "categories:roots";
            if (!_cache.TryGetValue(cacheKey, out List<Category>? categories))
            {
                categories = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(5));
            }

            return Ok(ApiResponse<IEnumerable<Category>>.SuccessResult(categories ?? new List<Category>()));
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCategory(int id)
        {
            _logger.LogInformation("Запит на категорію з ID: {CategoryId}", id);

            var cacheKey = $"category:{id}";
            if (!_cache.TryGetValue(cacheKey, out Category? category))
            {
                category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category != null)
                {
                    _cache.Set(cacheKey, category, TimeSpan.FromMinutes(5));
                }
            }

            if (category == null)
            {
                _logger.LogWarning("Категорію з ID {CategoryId} не знайдено", id);
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            return Ok(ApiResponse<Category>.SuccessResult(category));
        }

        // GET: api/Categories/5/products
        [HttpGet("{id}/products")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Product>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCategoryProducts(int id)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Категорію не знайдено", "NotFound"));
            }

            var cacheKey = $"category:{id}:products";
            if (!_cache.TryGetValue(cacheKey, out List<Product>? products))
            {
                products = await _context.Products
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == id)
                    .ToListAsync();

                _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));
            }

            return Ok(ApiResponse<IEnumerable<Product>>.SuccessResult(products ?? new List<Product>()));
        }
    }
}
