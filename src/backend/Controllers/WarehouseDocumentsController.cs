using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
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
        private readonly IProductRepository _productRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IRepository<IncomingDocument> _incomingRepository;
        private readonly IRepository<OutgoingDocument> _outgoingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WarehouseDocumentsController> _logger;
        private readonly IMemoryCache _cache;
        public WarehouseDocumentsController(
            IProductRepository productRepository,
            ICompanyRepository companyRepository,
            IWarehouseRepository warehouseRepository,
            IRepository<IncomingDocument> incomingRepository,
            IRepository<OutgoingDocument> outgoingRepository,
            IUnitOfWork unitOfWork,
            ILogger<WarehouseDocumentsController> logger,
            IMemoryCache cache)
        {
            _productRepository = productRepository;
            _companyRepository = companyRepository;
            _warehouseRepository = warehouseRepository;
            _incomingRepository = incomingRepository;
            _outgoingRepository = outgoingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cache = cache;
        }
        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
        }
        // Робота з прибутковими накладними (надходження товарів на склад від постачальників)
        [HttpGet("incoming")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIncomingDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productId = null, 
            [FromQuery] int? companyId = null)
        {
            IQueryable<IncomingDocument> query = _incomingRepository.Query()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser);
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
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            var company = await _companyRepository.GetByIdAsync(dto.CompanyId);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));
            if (!company.IsActive) return BadRequest(ApiResponse<object>.FailureResult("Компанія деактивована", "BadRequest"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
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
                await _warehouseRepository.AddAsync(doc);
                product.Stock += dto.Quantity; // Оновлення залишку
                await _warehouseRepository.SaveChangesAsync();
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
        // Робота з видатковими накладними (списання товарів зі складу або повернення постачальнику)
        [HttpGet("outgoing")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetOutgoingDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productId = null, 
            [FromQuery] string? reason = null)
        {
            IQueryable<OutgoingDocument> query = _outgoingRepository.Query()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser);
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
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            if (product.Stock < dto.Quantity)
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {product.Stock}", "BadRequest"));
            // Валідація правил reason з плану
            if (dto.Reason == "Return" && !dto.CompanyId.HasValue)
                return BadRequest(ApiResponse<object>.FailureResult("Для повернення (RETURN) обов'язково вказати компанію", "ValidationError"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
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
                await _warehouseRepository.AddAsync(doc);
                product.Stock -= dto.Quantity; // Оновлення залишку
                await _warehouseRepository.SaveChangesAsync();
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
            var stock = await _productRepository.Query()
                .Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    currentStock = p.Stock,
                    totalIncoming = _incomingRepository.Query().Where(id => id.ProductId == p.ProductId).Sum(id => id.Quantity),
                    totalOutgoing = _outgoingRepository.Query().Where(od => od.ProductId == p.ProductId).Sum(od => od.Quantity)
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
