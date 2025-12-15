using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
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

            // Хешування паролю (простий варіант для MVP)
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

            // Повертаємо дані без паролю
            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, new
            {
                userId = newUser.UserId,
                firstName = newUser.FirstName,
                lastName = newUser.LastName,
                email = newUser.Email,
                role = newUser.Role
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

            // Повертаємо без паролю
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
}