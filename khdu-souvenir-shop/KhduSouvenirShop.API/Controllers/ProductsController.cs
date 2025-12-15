using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Запит на отримання всіх товарів");
            
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

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

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Запит на товари категорії {CategoryId}", categoryId);

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            return Ok(products);
        }
    }
}