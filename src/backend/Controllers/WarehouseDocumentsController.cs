using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class WarehouseDocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WarehouseDocumentsController> _logger;

        public WarehouseDocumentsController(AppDbContext context, ILogger<WarehouseDocumentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- Incoming Documents (Прибуткові накладні) ---

        [HttpGet("incoming")]
        public async Task<ActionResult> GetIncomingDocuments([FromQuery] int? productId, [FromQuery] int? companyId)
        {
            var query = _context.IncomingDocuments
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(d => d.ProductId == productId);
            if (companyId.HasValue) query = query.Where(d => d.CompanyId == companyId);

            var docs = await query.OrderByDescending(d => d.DocumentDate).ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(docs));
        }

        [HttpPost("incoming")]
        public async Task<ActionResult> CreateIncomingDocument([FromBody] IncomingDocumentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                    CreatedByUserId = userId,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.IncomingDocuments.Add(doc);
                product.Stock += dto.Quantity; // Оновлення залишку
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
        public async Task<ActionResult> GetOutgoingDocuments([FromQuery] int? productId, [FromQuery] string? reason)
        {
            var query = _context.OutgoingDocuments
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(d => d.ProductId == productId);
            if (!string.IsNullOrEmpty(reason)) query = query.Where(d => d.Reason == reason);

            var docs = await query.OrderByDescending(d => d.DocumentDate).ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(docs));
        }

        [HttpPost("outgoing")]
        public async Task<ActionResult> CreateOutgoingDocument([FromBody] OutgoingDocumentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                    OriginalPrice = product.Price, // Для ручних видаткових за замовчуванням ціна з картки товару
                    FinalPrice = product.Price,
                    DocumentDate = dto.DocumentDate ?? DateTime.UtcNow,
                    CreatedByUserId = userId,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OutgoingDocuments.Add(doc);
                product.Stock -= dto.Quantity; // Оновлення залишку
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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