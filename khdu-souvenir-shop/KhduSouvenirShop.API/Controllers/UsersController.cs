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
        public async Task<ActionResult<User>> RegisterUser([FromBody] RegisterDto registerDto)
        {
            _logger.LogInformation("Спроба реєстрації користувача: {Email}", registerDto.Email);

            // Валідація вхідних даних
            if (string.IsNullOrWhiteSpace(registerDto.FirstName) ||
                string.IsNullOrWhiteSpace(registerDto.LastName) ||
                string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return BadRequest(new { error = "Всі поля обов'язкові" });
            }

            // Перевірка на наявність email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} вже зареєстрований", registerDto.Email);
                return Conflict(new { error = "Цей Email вже зареєстрований" });
            }

            // Хешування паролю
            var passwordHash = HashPassword(registerDto.Password);

            // Створення користувача
            var newUser = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Phone = registerDto.Phone,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Користувач {Email} успішно зареєстрований", newUser.Email);

            // Генеруємо JWT токен
            var token = GenerateJwtToken(newUser);

            // Повертаємо дані без паролю + токен
            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, new
            {
                userId = newUser.UserId,
                firstName = newUser.FirstName,
                lastName = newUser.LastName,
                email = newUser.Email,
                phone = newUser.Phone,
                role = newUser.Role,
                token = token
            });
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Спроба авторизації: {Email}", loginDto.Email);

            // Валідація
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new { error = "Email та пароль обов'язкові" });
            }

            // Пошук користувача
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                _logger.LogWarning("Користувача з email {Email} не знайдено", loginDto.Email);
                return Unauthorized(new { error = "Невірний email або пароль" });
            }

            // Перевірка паролю
            var passwordHash = HashPassword(loginDto.Password);
            if (user.PasswordHash != passwordHash)
            {
                _logger.LogWarning("Невірний пароль для {Email}", loginDto.Email);
                return Unauthorized(new { error = "Невірний email або пароль" });
            }

            // Генеруємо JWT токен
            var token = GenerateJwtToken(user);

            _logger.LogInformation("Користувач {Email} успішно авторизований", user.Email);

            return Ok(new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                token = token
            });
        }

        // GET: api/Users/me - отримання даних поточного користувача
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                return Unauthorized(new { error = "Не авторизовано" });
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { error = "Користувача не знайдено" });
            }

            return Ok(new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                createdAt = user.CreatedAt
            });
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { error = "Користувача не знайдено" });
            }

            return Ok(new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                createdAt = user.CreatedAt
            });
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

        // Простий метод хешування (для MVP, пізніше використаємо BCrypt)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
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