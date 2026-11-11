using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using KhduSouvenirShop.API.DTOs.Admin;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Адміністративне керування користувачами: перегляд, зміна ролі, блокування та скидання паролю.
    /// Доступ: Administrator і SuperAdmin. Administrator не має прав над SuperAdmin-акаунтами.
    /// </summary>
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public class AdminUsersController(
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        ILogger<AdminUsersController> logger) : ControllerBase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly ILogger<AdminUsersController> _logger = logger;
        // Ролі, дозволені для призначення через адмін-панель
        private static readonly string[] AssignableRoles = { "Customer", "Manager", "Administrator", "SuperAdmin" };
        // Роль поточного адміністратора з JWT
        private string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        // Перевірка обмежень RBAC: Administrator не керує SuperAdmin і не може призначити роль SuperAdmin
        private bool IsForbiddenTarget(string targetRole, string? newRole = null)
        {
            if (CurrentUserRole != "Administrator") return false;
            return targetRole == "SuperAdmin" || newRole == "SuperAdmin";
        }
        private ObjectResult ForbiddenResult() =>
            StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult("Недостатньо прав для цієї дії", "Forbidden"));
        // GET: api/admin/users - список користувачів (пагінація, пошук, фільтр за роллю/статусом)
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminUserListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            // Адмін бачить і заблокованих користувачів, тому ігноруємо фільтр soft-delete
            var query = _userRepository.Query().IgnoreQueryFilters();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(s)
                    || u.FirstName.ToLower().Contains(s)
                    || u.LastName.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                var blocked = status.Equals("blocked", StringComparison.OrdinalIgnoreCase);
                query = query.Where(u => u.IsDeleted == blocked);
            }
            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserListItemResponse
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    StudentStatus = u.StudentStatus,
                    CreatedAt = u.CreatedAt,
                    IsDeleted = u.IsDeleted
                })
                .ToListAsync();
            var response = new PagedResponse<AdminUserListItemResponse>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<AdminUserListItemResponse>>.SuccessResult(response));
        }
        // GET: api/admin/users/{id} - деталі профілю користувача
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<AdminUserDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserById(int id)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            var result = new AdminUserDetailResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                StudentStatus = user.StudentStatus,
                GPA = user.GPA,
                StudentVerifiedAt = user.StudentVerifiedAt,
                StudentExpiresAt = user.StudentExpiresAt,
                CreatedAt = user.CreatedAt,
                IsBlocked = user.IsDeleted
            };
            return Ok(ApiResponse<AdminUserDetailResponse>.SuccessResult(result));
        }
        // GET: api/admin/users/{id}/orders - замовлення користувача (пагінація)
        [HttpGet("{id:int}/orders")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderSummaryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserOrders(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var exists = await _userRepository.Query().IgnoreQueryFilters().AnyAsync(u => u.UserId == id);
            if (!exists)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            var query = _orderRepository.Query().Where(o => o.UserId == id);
            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderSummaryResponse
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
            var response = new PagedResponse<OrderSummaryResponse>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<OrderSummaryResponse>>.SuccessResult(response));
        }
        // PUT: api/admin/users/{id}/role - зміна ролі користувача
        [HttpPut("{id:int}/role")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest dto)
        {
            var role = dto.Role?.Trim();
            if (string.IsNullOrEmpty(role) || !AssignableRoles.Contains(role))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Неприпустиме значення ролі", "InvalidRole"));
            }
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            if (IsForbiddenTarget(user.Role, role))
            {
                return ForbiddenResult();
            }
            user.Role = role;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Роль користувача {UserId} змінено на {Role}", id, role);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Роль користувача оновлено"));
        }
        // PUT: api/admin/users/{id}/block - блокування користувача (soft delete)
        [HttpPut("{id:int}/block")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> BlockUser(int id)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            if (IsForbiddenTarget(user.Role))
            {
                return ForbiddenResult();
            }
            if (GetCurrentUserId() == id)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Не можна заблокувати власний акаунт", "BadRequest"));
            }
            if (user.IsDeleted)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Користувач уже заблокований", "BadRequest"));
            }
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Користувача {UserId} заблоковано", id);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Користувача заблоковано"));
        }
        // PUT: api/admin/users/{id}/unblock - розблокування користувача
        [HttpPut("{id:int}/unblock")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UnblockUser(int id)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            if (IsForbiddenTarget(user.Role))
            {
                return ForbiddenResult();
            }
            if (!user.IsDeleted)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Користувач не заблокований", "BadRequest"));
            }
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedBy = null;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Користувача {UserId} розблоковано", id);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Користувача розблоковано"));
        }
        // POST: api/admin/users/{id}/reset-password - скидання паролю адміністратором
        [HttpPost("{id:int}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordRequest dto)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            if (IsForbiddenTarget(user.Role))
            {
                return ForbiddenResult();
            }
            // Якщо новий пароль не передано - генеруємо тимчасовий і повертаємо його адміністратору
            var isGenerated = string.IsNullOrWhiteSpace(dto.NewPassword);
            var newPassword = isGenerated ? GenerateTemporaryPassword() : dto.NewPassword!;
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Адміністратор скинув пароль користувача {UserId}", id);
            return Ok(ApiResponse<object>.SuccessResult(
                new { temporaryPassword = isGenerated ? newPassword : null },
                "Пароль користувача скинуто"));
        }
        // POST: api/admin/users/{id}/student-extend - подовження студентського статусу ще на 4 місяці
        [HttpPost("{id:int}/student-extend")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ExtendStudentStatus(int id)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            if (IsForbiddenTarget(user.Role))
            {
                return ForbiddenResult();
            }
            if (user.StudentStatus == "NONE")
            {
                return BadRequest(ApiResponse<object>.FailureResult("Користувач не має студентського статусу", "NoStudentStatus"));
            }
            // Продовжуємо від поточної дати закінчення, якщо вона ще в майбутньому, інакше — від сьогодні
            var basis = user.StudentExpiresAt.HasValue && user.StudentExpiresAt.Value > DateTime.UtcNow
                ? user.StudentExpiresAt.Value
                : DateTime.UtcNow;
            user.StudentExpiresAt = basis.AddMonths(UniversityService.StudentStatusValidMonths);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Студентський статус користувача {UserId} продовжено до {ExpiresAt}", id, user.StudentExpiresAt);
            return Ok(ApiResponse<object>.SuccessResult(
                new { studentExpiresAt = user.StudentExpiresAt },
                "Термін студентського статусу продовжено"));
        }
        // Генерація тимчасового паролю, що задовольняє вимоги складності
        private static string GenerateTemporaryPassword()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var bytes = RandomNumberGenerator.GetBytes(12);
            return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
        }
    }
}
