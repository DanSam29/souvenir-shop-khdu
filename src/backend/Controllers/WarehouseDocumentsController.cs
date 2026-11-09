using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KhduSouvenirShop.API.Controllers
{
    // Контролер складу (Manager+): прибуткові/видаткові накладні та поточні залишки
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class WarehouseDocumentsController : ControllerBase
    {
        // Допустимі причини прибуткової накладної
        private static readonly string[] IncomingReasons = { "INCOMING", "RETURN" };

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

        // Перевірка компанії-постачальника: компанія обов'язкова та має бути активна
        private async Task<(string? Error, string? Code)> ValidateCompanyAsync(int? companyId)
        {
            if (!companyId.HasValue)
                return ("Для прибуткової накладної обов'язково вказати компанію-постачальника", "ValidationError");
            var company = await _companyRepository.GetByIdAsync(companyId.Value);
            if (company == null) return ("Компанію не знайдено", "NotFound");
            if (!company.IsActive) return ("Компанія деактивована", "BadRequest");
            return (null, null);
        }

        // ======================= Прибуткові накладні =======================

        [HttpGet("incoming")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIncomingDocuments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? productId = null,
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? date = null)
        {
            IQueryable<IncomingDocument> query = _incomingRepository.Query()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser);
            if (productId.HasValue) query = query.Where(d => d.ProductId == productId);
            if (companyId.HasValue) query = query.Where(d => d.CompanyId == companyId);
            if (date.HasValue) query = query.Where(d => d.DocumentDate.Date == date.Value.Date);
            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.DocumentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        [HttpGet("incoming/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetIncomingDocument(int id)
        {
            var doc = await _incomingRepository.Query()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.CreatedByUser)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Прибуткову накладну не знайдено", "NotFound"));
            return Ok(ApiResponse<object>.SuccessResult(doc));
        }

        [HttpPost("incoming")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateIncomingDocument([FromBody] IncomingDocumentDto dto)
        {
            var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "INCOMING" : dto.Reason;
            if (!IncomingReasons.Contains(reason))
                return BadRequest(ApiResponse<object>.FailureResult("Некоректна причина надходження", "ValidationError"));
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            // Для INCOMING компанія обов'язкова; для RETURN — необов'язкова, але якщо вказана, має бути активною
            if (reason != "RETURN" || dto.CompanyId.HasValue)
            {
                var (error, code) = await ValidateCompanyAsync(dto.CompanyId);
                if (error != null) return BadRequest(ApiResponse<object>.FailureResult(error, code));
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var doc = new IncomingDocument
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    PurchasePrice = dto.PurchasePrice,
                    Reason = reason,
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

        [HttpPut("incoming/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateIncomingDocument(int id, [FromBody] IncomingDocumentDto dto)
        {
            var doc = await _warehouseRepository.GetIncomingByIdAsync(id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Прибуткову накладну не знайдено", "NotFound"));
            var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "INCOMING" : dto.Reason;
            if (!IncomingReasons.Contains(reason))
                return BadRequest(ApiResponse<object>.FailureResult("Некоректна причина надходження", "ValidationError"));
            if (reason != "RETURN" || dto.CompanyId.HasValue)
            {
                var (error, code) = await ValidateCompanyAsync(dto.CompanyId);
                if (error != null) return BadRequest(ApiResponse<object>.FailureResult(error, code));
            }
            var newProduct = await _productRepository.GetByIdAsync(dto.ProductId);
            if (newProduct == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (doc.ProductId != dto.ProductId)
                {
                    // Товар змінився: повертаємо стару кількість старому товару, додаємо нову - новому
                    var oldProduct = await _productRepository.GetByIdAsync(doc.ProductId);
                    if (oldProduct != null) oldProduct.Stock -= doc.Quantity;
                    newProduct.Stock += dto.Quantity;
                }
                else
                {
                    newProduct.Stock += dto.Quantity - doc.Quantity;
                }
                doc.ProductId = dto.ProductId;
                doc.Quantity = dto.Quantity;
                doc.PurchasePrice = dto.PurchasePrice;
                doc.Reason = reason;
                doc.CompanyId = dto.CompanyId;
                if (dto.DocumentDate.HasValue) doc.DocumentDate = dto.DocumentDate.Value;
                doc.Notes = dto.Notes;
                _warehouseRepository.Update(doc);
                await _warehouseRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                InvalidateCache();
                return Ok(ApiResponse<object>.SuccessResult(doc, "Прибуткову накладну оновлено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при оновленні прибуткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося оновити документ", "InternalServerError"));
            }
        }

        [HttpDelete("incoming/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteIncomingDocument(int id)
        {
            var doc = await _warehouseRepository.GetIncomingByIdAsync(id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Прибуткову накладну не знайдено", "NotFound"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepository.GetByIdAsync(doc.ProductId);
                if (product != null) product.Stock -= doc.Quantity; // Повернення залишку назад
                _warehouseRepository.Remove(doc);
                await _warehouseRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                InvalidateCache();
                return Ok(ApiResponse<object?>.SuccessResult(null, "Прибуткову накладну видалено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при видаленні прибуткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося видалити документ", "InternalServerError"));
            }
        }

        // ======================= Видаткові накладні =======================

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

        [HttpGet("outgoing/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetOutgoingDocument(int id)
        {
            var doc = await _outgoingRepository.Query()
                .Include(d => d.Product)
                .Include(d => d.Company)
                .Include(d => d.Order)
                .Include(d => d.AppliedPromotion)
                .Include(d => d.CreatedByUser)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Видаткову накладну не знайдено", "NotFound"));
            return Ok(ApiResponse<object>.SuccessResult(doc));
        }

        [HttpPost("outgoing")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateOutgoingDocument([FromBody] OutgoingDocumentDto dto)
        {
            // ORDER створюється системою автоматично при оформленні замовлення, вручну - заборонено
            if (dto.Reason == "Order")
                return BadRequest(ApiResponse<object>.FailureResult("Накладну з причиною ORDER створює система автоматично при оформленні замовлення", "ValidationError"));
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            if (product.Stock < dto.Quantity)
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {product.Stock}", "BadRequest"));
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

        [HttpPut("outgoing/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateOutgoingDocument(int id, [FromBody] OutgoingDocumentDto dto)
        {
            var doc = await _warehouseRepository.GetOutgoingByIdAsync(id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Видаткову накладну не знайдено", "NotFound"));
            if (dto.Reason == "Order")
                return BadRequest(ApiResponse<object>.FailureResult("Причину ORDER не можна встановити вручну", "ValidationError"));
            if (dto.Reason == "Return" && !dto.CompanyId.HasValue)
                return BadRequest(ApiResponse<object>.FailureResult("Для повернення (RETURN) обов'язково вказати компанію", "ValidationError"));
            var newProduct = await _productRepository.GetByIdAsync(dto.ProductId);
            if (newProduct == null) return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            // Перевірка достатності залишку з урахуванням повернення старої кількості
            var available = doc.ProductId == dto.ProductId
                ? newProduct.Stock + doc.Quantity
                : newProduct.Stock;
            if (available < dto.Quantity)
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {available}", "BadRequest"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (doc.ProductId != dto.ProductId)
                {
                    // Товар змінився: повертаємо стару кількість старому товару, списуємо нову - з нового
                    var oldProduct = await _productRepository.GetByIdAsync(doc.ProductId);
                    if (oldProduct != null) oldProduct.Stock += doc.Quantity;
                    newProduct.Stock -= dto.Quantity;
                }
                else
                {
                    newProduct.Stock += doc.Quantity - dto.Quantity;
                }
                doc.ProductId = dto.ProductId;
                doc.Quantity = dto.Quantity;
                doc.Reason = dto.Reason;
                doc.CompanyId = dto.CompanyId;
                if (dto.DocumentDate.HasValue) doc.DocumentDate = dto.DocumentDate.Value;
                doc.Notes = dto.Notes;
                _warehouseRepository.Update(doc);
                await _warehouseRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                InvalidateCache();
                return Ok(ApiResponse<object>.SuccessResult(doc, "Видаткову накладну оновлено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при оновленні видаткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося оновити документ", "InternalServerError"));
            }
        }

        [HttpDelete("outgoing/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteOutgoingDocument(int id)
        {
            var doc = await _warehouseRepository.GetOutgoingByIdAsync(id);
            if (doc == null) return NotFound(ApiResponse<object>.FailureResult("Видаткову накладну не знайдено", "NotFound"));
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepository.GetByIdAsync(doc.ProductId);
                if (product != null) product.Stock += doc.Quantity; // Повернення залишку назад
                _warehouseRepository.Remove(doc);
                await _warehouseRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                InvalidateCache();
                return Ok(ApiResponse<object?>.SuccessResult(null, "Видаткову накладну видалено"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при видаленні видаткової накладної");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося видалити документ", "InternalServerError"));
            }
        }

        // ======================= Поточні залишки =======================

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
        // Reason: INCOMING (за замовчуванням) або RETURN
        public string Reason { get; set; } = "INCOMING";
        // Для RETURN компанія необов'язкова
        public int? CompanyId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class OutgoingDocumentDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Other"; // Damaged, Lost, Return, Inventory, Other (Order - лише автоматично)
        public int? CompanyId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? Notes { get; set; }
    }
}
