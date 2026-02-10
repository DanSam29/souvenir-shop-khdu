using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(AppDbContext context, ILogger<ProductsController> logger, IMemoryCache cache) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IMemoryCache _cache = cache;

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Запит на отримання всіх товарів");
            
            var cacheKey = "products:all";
            var products = _cache.Get<List<Product>>(cacheKey);
            if (products is null)
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .ToListAsync();

                _cache.Set(cacheKey, products, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            return Ok(products ?? new List<Product>());
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);

            var cacheKey = $"product:{id}";
            var product = _cache.Get<Product>(cacheKey);
            if (product is null)
            {
                product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product != null)
                {
                    _cache.Set(cacheKey, product, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                }
            }

            if (product == null)
            {
                _logger.LogWarning("Товар з ID {ProductId} не знайдено", id);
                return NotFound(new { error = "Товар не знайдено" });
            }

            return Ok(product);
        }

        // GET: api/Products/search?query=футболка
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string query)
        {
            _logger.LogInformation("Пошук товарів за запитом: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Пошуковий запит не може бути порожнім" });
            }

            var norm = query.Trim().ToLowerInvariant();
            var cacheKey = $"products:search:{norm}";
            var products = _cache.Get<List<Product>>(cacheKey);
            if (products is null)
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                    .ToListAsync();

                _cache.Set(cacheKey, products, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                });
            }

            return Ok(products ?? new List<Product>());
        }

        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", categoryId);

            var cacheKey = $"products:category:{categoryId}";
            var products = _cache.Get<List<Product>>(cacheKey);
            if (products is null)
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => p.CategoryId == categoryId)
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
