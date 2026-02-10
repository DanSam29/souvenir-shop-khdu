using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
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
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            _logger.LogInformation("Запит на отримання всіх категорій");

            var cacheKey = "categories:roots";
            var categories = _cache.Get<List<Category>>(cacheKey);
            if (categories is null)
            {
                categories = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, categories, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(categories ?? new List<Category>());
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            _logger.LogInformation("Запит на категорію з ID: {CategoryId}", id);

            var cacheKey = $"category:{id}";
            var category = _cache.Get<Category>(cacheKey);
            if (category is null)
            {
                category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category != null)
                {
                    _cache.Set(cacheKey, category, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                }
            }

            if (category == null)
            {
                _logger.LogWarning("Категорію з ID {CategoryId} не знайдено", id);
                return NotFound(new { error = "Категорію не знайдено" });
            }

            return Ok(category);
        }

        // GET: api/Categories/5/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetCategoryProducts(int id)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { error = "Категорію не знайдено" });
            }

            var cacheKey = $"category:{id}:products";
            var products = _cache.Get<List<Product>>(cacheKey);
            if (products is null)
            {
                products = await _context.Products
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == id)
                    .ToListAsync();

                _cache.Set(cacheKey, products, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(products ?? new List<Product>());
        }
    }
}
