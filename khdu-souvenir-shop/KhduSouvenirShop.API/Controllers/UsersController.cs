using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using BCrypt.Net;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, ILogger<UsersController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // POST: api/Users/register
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegisterUser([FromBody] RegisterDto registerDto)
        {
            _logger.LogInformation("Спроба реєстрації користувача: {Email}", registerDto.Email);

            // Перевірка на наявність email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} вже зареєстрований", registerDto.Email);
                return Conflict(ApiResponse<object>.FailureResult("Цей Email вже зареєстрований", "Conflict"));
            }

            // Хешування паролю
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password, workFactor: 12);

            // Створення користувача
            var newUser = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                Password = passwordHash,
                Phone = registerDto.Phone,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            // Логіка: Встановлення студентського статусу за доменом email
            var emailLower = (newUser.Email ?? string.Empty).ToLowerInvariant();
            if (emailLower.EndsWith("@university.ks.ua"))
            {
                newUser.StudentStatus = "REGULAR";
                newUser.StudentVerifiedAt = DateTime.UtcNow;
                newUser.StudentExpiresAt = DateTime.UtcNow.AddYears(1);
                _logger.LogInformation("Користувач {Email} зареєстрований як студент", newUser.Email);
            }
            else
            {
                newUser.StudentStatus = "NONE";
                _logger.LogInformation("Користувач {Email} зареєстрований як зовнішній користувач", newUser.Email);
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Користувач {Email} успішно зареєстрований", newUser.Email);

            var token = GenerateJwtToken(newUser);

            var result = new
            {
                userId = newUser.UserId,
                firstName = newUser.FirstName,
                lastName = newUser.LastName,
                email = newUser.Email,
                phone = newUser.Phone,
                role = newUser.Role,
                studentStatus = newUser.StudentStatus,
                studentVerifiedAt = newUser.StudentVerifiedAt,
                studentExpiresAt = newUser.StudentExpiresAt,
                token = token
            };

            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, ApiResponse<object>.SuccessResult(result, "Користувач успішно зареєстрований"));
        }

        // POST: api/Users/login
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Спроба авторизації: {Email}", loginDto.Email);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                _logger.LogWarning("Користувача з email {Email} не знайдено", loginDto.Email);
                return Unauthorized(ApiResponse<object>.FailureResult("Невірний email або пароль", "Unauthorized"));
            }

            var isValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);
            if (!isValid)
            {
                _logger.LogWarning("Невірний пароль для {Email}", loginDto.Email);
                return Unauthorized(ApiResponse<object>.FailureResult("Невірний email або пароль", "Unauthorized"));
            }

            var token = GenerateJwtToken(user);

            _logger.LogInformation("Користувач {Email} успішно авторизований", user.Email);

            var result = new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                studentStatus = user.StudentStatus,
                studentVerifiedAt = user.StudentVerifiedAt,
                studentExpiresAt = user.StudentExpiresAt,
                token = token
            };

            return Ok(ApiResponse<object>.SuccessResult(result, "Авторизація успішна"));
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

            var user = await _context.Users.FindAsync(userId);

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

        // GET: api/Users/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

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

        // Метод генерації JWT токену
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key не налаштовано");
            var jwtIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer не налаштовано");
            var jwtAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience не налаштовано");
            var jwtExpireMinutes = jwtSettings["ExpireMinutes"] ?? "1440";
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtExpireMinutes)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Хешування через BCrypt (workFactor=12)
        // Збережено для можливих міграцій або альтернативних сценаріїв
        private string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    // DTO для реєстрації
    public class RegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    // DTO для авторизації
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
