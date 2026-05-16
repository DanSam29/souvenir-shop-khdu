using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Manager")]
    public class WarehouseDocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WarehouseDocumentsController> _logger;
        private readonly IMemoryCache _cache;

        public WarehouseDocumentsController(AppDbContext context, ILogger<WarehouseDocumentsController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }

        // --- Incoming Documents (Прибуткові накладні) ---

        [HttpGet("incoming")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIncomingDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productId = null, 
            [FromQuery] int? companyId = null)
        {
            var query = _context.IncomingDocuments
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(d => d.ProductId == productId);
            if (companyId.HasValue) query = query.Where(d => d.CompanyId == companyId);

            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.DocumentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        [HttpPost("incoming")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateIncomingDocument([FromBody] IncomingDocumentDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            var company = await _context.Companies.FindAsync(dto.CompanyId);

            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));
            if (!company.IsActive) return BadRequest(ApiResponse<object>.FailureResult("Компанія деактивована", "BadRequest"));

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doc = new IncomingDocument
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    PurchasePrice = dto.PurchasePrice,
                    CompanyId = dto.CompanyId,
                    DocumentDate = dto.DocumentDate ?? DateTime.UtcNow,
                    Notes = dto.Notes
                };

                _context.IncomingDocuments.Add(doc);
                product.Stock += dto.Quantity; // Оновлення залишку

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                InvalidateCache();

                return Ok(ApiResponse<object>.SuccessResult(doc, "Прибуткову накладну створено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при створенні прибуткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося створити документ", "InternalServerError"));
            }
        }

        // --- Outgoing Documents (Видаткові накладні) ---

        [HttpGet("outgoing")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetOutgoingDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productId = null, 
            [FromQuery] string? reason = null)
        {
            var query = _context.OutgoingDocuments
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(d => d.ProductId == productId);
            if (!string.IsNullOrEmpty(reason)) query = query.Where(d => d.Reason == reason);

            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.DocumentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        [HttpPost("outgoing")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateOutgoingDocument([FromBody] OutgoingDocumentDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));

            if (product.Stock < dto.Quantity)
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {product.Stock}", "BadRequest"));

            // Валідація правил reason з плану
            if (dto.Reason == "RETURN" && !dto.CompanyId.HasValue)
                return BadRequest(ApiResponse<object>.FailureResult("Для повернення (RETURN) обов'язково вказати компанію", "ValidationError"));

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doc = new OutgoingDocument
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    Reason = dto.Reason,
                    CompanyId = dto.CompanyId,
                    OriginalPrice = product.Price, 
                    FinalPrice = product.Price,
                    DocumentDate = dto.DocumentDate ?? DateTime.UtcNow,
                    Notes = dto.Notes
                };

                _context.OutgoingDocuments.Add(doc);
                product.Stock -= dto.Quantity; // Оновлення залишку

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                InvalidateCache();

                return Ok(ApiResponse<object>.SuccessResult(doc, "Видаткову накладну створено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при створенні видаткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося створити документ", "InternalServerError"));
            }
        }

        [HttpGet("stock")]
        public async Task<ActionResult> GetCurrentStock()
        {
            var stock = await _context.Products
                .Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    currentStock = p.Stock,
                    totalIncoming = _context.IncomingDocuments.Where(id => id.ProductId == p.ProductId).Sum(id => id.Quantity),
                    totalOutgoing = _context.OutgoingDocuments.Where(od => od.ProductId == p.ProductId).Sum(od => od.Quantity)
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResult(stock));
        }
    }

    public class IncomingDocumentDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public int CompanyId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class OutgoingDocumentDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Other"; // Damaged, Lost, Return, Inventory, Other
        public int? CompanyId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? Notes { get; set; }
    }
}
