using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(
        IUserRepository userRepository,
        IAuthService authService,
        IMapper mapper,
        ILogger<UsersController> logger,
        IConfiguration configuration,
        IUniversityService universityService,
        IEmailService emailService) : ControllerBase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAuthService _authService = authService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<UsersController> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly IUniversityService _universityService = universityService;
        private readonly IEmailService _emailService = emailService;
        // POST: api/Users/register
        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegisterUser([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Спроба реєстрації користувача: {Email}", request.Email);
            var email = request.Email.ToLower().Trim();
            // Перевірка унікальності email
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} вже зареєстрований", email);
                return Conflict(ApiResponse<object>.FailureResult("Цей Email вже зареєстрований", "Conflict"));
            }
            // Створення користувача (хеш паролю формується окремо через BCrypt)
            var newUser = _mapper.Map<User>(request);
            newUser.Email = email;
            newUser.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
            newUser.Role = "Customer";
            // Автоматичне створення кошика для нового користувача
            newUser.Cart = new Cart { CreatedAt = DateTime.UtcNow };
            // Встановлення студентського статусу за доменом email (запуск верифікації, Етап 9)
            var isUniEnabled = _configuration.GetValue<bool>("Features:UniversityEnabled");
            var allowedDomains = _configuration.GetSection("University:AllowedDomains").Get<string[]>() ??
                                 ["ksu.edu.ua", "student.ksu.edu.ua"];
            if (isUniEnabled && allowedDomains.Any(domain => email.EndsWith("@" + domain)))
            {
                var studentInfo = await _universityService.GetStudentInfoAsync(newUser.Email!);
                if (studentInfo != null && studentInfo.IsActive)
                {
                    newUser.GPA = studentInfo.GPA;
                    if (newUser.GPA >= 4.8m) newUser.StudentStatus = "HIGH_ACHIEVER";
                    else if (newUser.GPA >= 4.0m) newUser.StudentStatus = "SCHOLARSHIP";
                    else newUser.StudentStatus = "REGULAR";
                    newUser.StudentVerifiedAt = DateTime.UtcNow;
                    newUser.StudentExpiresAt = DateTime.UtcNow.AddMonths(4);
                    _logger.LogInformation("Користувач {Email} автоматично верифікований як студент. Статус: {Status}", newUser.Email, newUser.StudentStatus);
                    await _emailService.SendStudentVerificationEmailAsync(newUser.Email, newUser.StudentStatus, "ua");
                }
            }
            else
            {
                newUser.StudentStatus = "NONE";
                _logger.LogInformation("Користувач {Email} зареєстрований як зовнішній користувач", newUser.Email);
            }
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Користувач {Email} успішно зареєстрований", newUser.Email);
            // Видача пари токенів: короткоживучий access + довгоживучий refresh
            var response = _mapper.Map<AuthResponse>(newUser);
            response.Token = _authService.GenerateAccessToken(newUser);
            response.RefreshToken = await _authService.IssueRefreshTokenAsync(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, ApiResponse<AuthResponse>.SuccessResult(response, "Користувач успішно зареєстрований"));
        }
        // POST: api/Users/login
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Спроба авторизації: {Email}", request.Email);
            var email = request.Email.ToLower().Trim();
            // Пошук користувача, ігноруючи фільтр видалення, щоб перевірити на блокування
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("Користувача з email {Email} не знайдено", email);
                return Unauthorized(ApiResponse<object>.FailureResult("Невірний email або пароль", "Unauthorized"));
            }
            if (user.IsDeleted)
            {
                _logger.LogWarning("Спроба входу в заблокований акаунт: {Email}", email);
                return BadRequest(ApiResponse<object>.FailureResult("AccountBlocked", "Ваш акаунт заблоковано. Будь ласка, зверніться до адміністратора."));
            }
            var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isValid)
            {
                _logger.LogWarning("Невірний пароль для {Email}", email);
                return Unauthorized(ApiResponse<object>.FailureResult("Невірний email або пароль", "Unauthorized"));
            }
            _logger.LogInformation("Користувач {Email} успішно авторизований", user.Email);
            var response = _mapper.Map<AuthResponse>(user);
            response.Token = _authService.GenerateAccessToken(user);
            response.RefreshToken = await _authService.IssueRefreshTokenAsync(user);
            return Ok(ApiResponse<AuthResponse>.SuccessResult(response, "Авторизація успішна"));
        }
        // POST: api/Users/refresh
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var (user, accessToken, refreshToken) = await _authService.RefreshAsync(request.RefreshToken);
            if (user == null || accessToken == null || refreshToken == null)
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Недійсний або прострочений refresh-токен", "Unauthorized"));
            }
            var response = _mapper.Map<AuthResponse>(user);
            response.Token = accessToken;
            response.RefreshToken = refreshToken;
            return Ok(ApiResponse<AuthResponse>.SuccessResult(response, "Токен оновлено"));
        }
        // POST: api/Users/logout
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Ви успішно вийшли із системи"));
        }
        // POST: api/Users/forgot-password
        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.RequestPasswordResetAsync(request.Email, "ua");
            // Не розкриваємо, чи існує такий email (захист від перебору акаунтів)
            return Ok(ApiResponse<object?>.SuccessResult(null, "Якщо такий email зареєстровано, інструкції для скидання паролю надіслано"));
        }
        // POST: api/Users/reset-password
        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var success = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Недійсний або прострочений токен скидання паролю", "InvalidToken"));
            }
            return Ok(ApiResponse<object?>.SuccessResult(null, "Пароль успішно змінено. Тепер ви можете увійти з новим паролем"));
        }
        // GET: api/Users/me
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            var result = new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                createdAt = user.CreatedAt,
                studentStatus = user.StudentStatus,
                studentVerifiedAt = user.StudentVerifiedAt,
                studentExpiresAt = user.StudentExpiresAt,
                gpa = user.GPA
            };
            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        // Методи керування користувачами для адміністраторів (перегляд списку, зміна ролей та блокування)
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null)
        {
            var query = _userRepository.Query();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(s) || u.FirstName.ToLower().Contains(s) || u.LastName.ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }
            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new {
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Role,
                    u.StudentStatus,
                    u.CreatedAt,
                    u.IsDeleted
                })
                .ToListAsync();
            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }
        [HttpPatch("{id}/role")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено"));
            user.Role = dto.Role;
            await _userRepository.SaveChangesAsync();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Роль користувача оновлено"));
        }
        [HttpPost("{id}/toggle-block")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ToggleBlockUser(int id)
        {
            var user = await _userRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено"));
            if (user.IsDeleted)
            {
                user.IsDeleted = false;
                user.DeletedAt = null;
                user.DeletedBy = null;
                await _userRepository.SaveChangesAsync();
                return Ok(ApiResponse<object>.SuccessResult(new { isBlocked = false }, "Користувача розблоковано"));
            }
            else
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                // Ідентифікатор того, хто видалив, буде встановлено автоматично в SaveChangesAsync
                await _userRepository.SaveChangesAsync();
                return Ok(ApiResponse<object>.SuccessResult(new { isBlocked = true }, "Користувача заблоковано (Soft Delete)"));
            }
        }
        // PUT: api/Users/me
        [Authorize]
        [HttpPut("me")]
        public async Task<ActionResult> UpdateProfile([FromBody] UserUpdateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Phone = dto.Phone;
            await _userRepository.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new {
                user.FirstName,
                user.LastName,
                user.Phone
            }, "Профіль оновлено"));
        }
        // POST: api/Users/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();
            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Невірний старий пароль", "InvalidPassword"));
            }
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);

            await _userRepository.SaveChangesAsync();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Пароль змінено"));
        }
        // GET: api/Users/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }
            var result = new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                createdAt = user.CreatedAt,
                studentStatus = user.StudentStatus,
                studentVerifiedAt = user.StudentVerifiedAt,
                studentExpiresAt = user.StudentExpiresAt,
                gpa = user.GPA
            };
            return Ok(ApiResponse<object>.SuccessResult(result));
        }
    }
    // DTO для оновлення даних користувача
    public class UserUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
    public class UpdateRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }
}
