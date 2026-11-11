using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using KhduSouvenirShop.API.DTOs.Admin;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Адміністративне керування замовленнями: список із фільтрами, деталі з історією статусів
    /// та зміна статусу з валідацією дозволених переходів.
    /// Доступ: Manager, Administrator, SuperAdmin.
    /// </summary>
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = "Manager,Administrator,SuperAdmin")]
    public class AdminOrdersController(
        IOrderRepository orderRepository,
        IShippingRepository shippingRepository,
        IRepository<OrderHistory> orderHistoryRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IEmailService emailService,
        PromotionService promotionService,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<AdminOrdersController> logger) : ControllerBase
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IShippingRepository _shippingRepository = shippingRepository;
        private readonly IRepository<OrderHistory> _orderHistoryRepository = orderHistoryRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IPaymentService _paymentService = paymentService;
        private readonly IEmailService _emailService = emailService;
        private readonly PromotionService _promotionService = promotionService;
        private readonly IMapper _mapper = mapper;
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<AdminOrdersController> _logger = logger;
        private void InvalidateCache()
        {
            var currentVersion = _cache.Get<int>("Products_Cache_Version");
            _cache.Set("Products_Cache_Version", currentVersion + 1);
            _cache.Remove("Public_Products_All");
            // Аналітичні агрегати залежать від замовлень — інвалідуємо разом
            AnalyticsService.InvalidateCache(_cache);
        }
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        // GET: api/admin/orders - список замовлень (пагінація, фільтри статусу/дати/способу оплати/пошуку)
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminOrderListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? search = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var query = _orderRepository.Query().Include(o => o.User).Include(o => o.Shipping).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }
            if (dateFrom.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= dateTo.Value);
            }
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(o => o.Payment != null && o.Payment.Method == paymentMethod);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(s)
                    || o.User.Email.ToLower().Contains(s));
            }
            var count = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = _mapper.Map<List<AdminOrderListItemResponse>>(orders);
            var response = new PagedResponse<AdminOrderListItemResponse>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<AdminOrderListItemResponse>>.SuccessResult(response));
        }
        // GET: api/admin/orders/{id} - деталі замовлення з історією зміни статусів
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetOrderById(int id)
        {
            var order = await _orderRepository.Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipping)
                .Include(o => o.Payment)
                .Include(o => o.OrderHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }
            var result = _mapper.Map<OrderResponse>(order);
            result.History = _mapper.Map<List<OrderHistoryResponse>>(order.OrderHistories.OrderByDescending(h => h.Timestamp).ToList());
            result.AppliedDiscounts = await _promotionService.GetAppliedDiscountsAsync(order.OrderItems);
            return Ok(ApiResponse<OrderResponse>.SuccessResult(result));
        }
        // PUT: api/admin/orders/{id}/status - зміна статусу замовлення
        [HttpPut("{id:int}/status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest dto)
        {
            var adminUserId = GetCurrentUserId();
            if (adminUserId == null)
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }
            var newStatus = dto.Status?.Trim();
            if (string.IsNullOrEmpty(newStatus))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Не вказано новий статус", "BadRequest"));
            }
            var order = await _orderRepository.Query()
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Замовлення не знайдено", "NotFound"));
            }
            // Скасування виконується за логікою пункту 4.3
            if (newStatus == "Cancelled")
            {
                return await CancelOrderAsync(order, adminUserId.Value, dto.Comment);
            }
            if (order.Status == "Cancelled" || order.Status == "Delivered")
            {
                return BadRequest(ApiResponse<object>.FailureResult($"Замовлення у статусі '{order.Status}' не підлягає зміні статусу", "BadRequest"));
            }
            // Валідація дозволеного переходу (єдине джерело істини — OrderStateMachine)
            if (!OrderStateMachine.CanTransition(order.Status, newStatus))
            {
                return BadRequest(ApiResponse<object>.FailureResult($"Недопустимий перехід статусу: {order.Status} → {newStatus}", "BadRequest"));
            }
            string comment;
            // Processing/Paid → Shipped: обов'язковий ТТН Nova Poshta
            if (newStatus == "Shipped")
            {
                if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Для статусу 'Shipped' необхідно вказати номер ТТН", "BadRequest"));
                }
                var shipping = await _shippingRepository.Query().FirstOrDefaultAsync(s => s.OrderId == id);
                if (shipping == null)
                {
                    return BadRequest(ApiResponse<object>.FailureResult("Для замовлення не знайдено запис доставки", "BadRequest"));
                }
                var trackingNumber = dto.TrackingNumber.Trim();
                shipping.TrackingNumber = trackingNumber;
                comment = $"Відправлено Новою Поштою. ТТН: {trackingNumber}";
            }
            else
            {
                comment = string.IsNullOrWhiteSpace(dto.Comment) ? $"Статус змінено адміністратором на {newStatus}" : dto.Comment.Trim();
            }
            var oldStatus = order.Status;
            order.Status = newStatus;
            // Shipped → Delivered: для накладеного платежу (COD) оплата вважається завершеною
            if (newStatus == "Delivered"
                && order.Payment != null
                && PaymentStates.IsCashOnDelivery(order.Payment.Method)
                && !PaymentStates.IsPaid(order.Payment.Status))
            {
                order.Payment.Status = PaymentStatus.Completed.ToString();
                order.Payment.CompletedAt = DateTime.UtcNow;
                order.Payment.UpdatedAt = DateTime.UtcNow;
                comment = "Оплата при отриманні прийнята";
            }
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = adminUserId,
                Comment = comment,
                Timestamp = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            InvalidateCache();
            _logger.LogInformation("Замовлення {OrderId}: статус {OldStatus} → {NewStatus}", id, oldStatus, newStatus);
            return Ok(ApiResponse<object>.SuccessResult(new { orderId = order.OrderId, status = order.Status }, "Статус замовлення оновлено"));
        }
        // Скасування замовлення адміністратором (логіка пункту 4.3)
        private async Task<ActionResult> CancelOrderAsync(Order order, int adminUserId, string? comment)
        {
            if (order.Status == "Cancelled")
            {
                return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення вже скасоване"));
            }
            if (order.Status == "Delivered")
            {
                return BadRequest(ApiResponse<object>.FailureResult("Доставлене замовлення не можна скасувати", "BadRequest"));
            }
            var reason = string.IsNullOrWhiteSpace(comment) ? "Скасовано адміністратором" : comment.Trim();
            bool cancelled;
            if (order.Payment != null && order.Payment.Status == "Completed")
            {
                cancelled = await _paymentService.RefundPaymentAsync(order.OrderId, reason);
                if (!cancelled)
                {
                    cancelled = await _paymentService.CancelOrderAndRestoreStock(order.OrderId, reason);
                }
            }
            else
            {
                cancelled = await _paymentService.CancelOrderAndRestoreStock(order.OrderId, reason);
            }
            if (!cancelled)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.FailureResult("Не вдалося скасувати замовлення", "InternalServerError"));
            }
            var user = await _userRepository.GetByIdAsync(order.UserId);
            if (user != null)
            {
                await _emailService.SendOrderCancellationAsync(user.Email, order.OrderNumber, reason, user.PreferredLanguage);
            }
            InvalidateCache();
            _logger.LogInformation("Замовлення {OrderId} скасовано адміністратором {AdminId}", order.OrderId, adminUserId);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Замовлення скасовано"));
        }
    }
}
