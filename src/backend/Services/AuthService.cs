using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Сервіс автентифікації: генерація та оновлення JWT/refresh-токенів, скидання паролю.
    /// </summary>
    public interface IAuthService
    {
        string GenerateAccessToken(User user);
        Task<string> IssueRefreshTokenAsync(User user);
        Task<(User? User, string? AccessToken, string? RefreshToken)> RefreshAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> RequestPasswordResetAsync(string email, string lang = "ua");
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private const int PasswordResetTokenLifetimeHours = 1;

        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IRepository<PasswordResetToken> _passwordResetTokenRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IConfiguration configuration,
            IUserRepository userRepository,
            IRepository<RefreshToken> refreshTokenRepository,
            IRepository<PasswordResetToken> passwordResetTokenRepository,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>Генерує підписаний JWT access-токен для користувача.</summary>
        public string GenerateAccessToken(User user)
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

        /// <summary>Створює та зберігає новий refresh-токен для користувача.</summary>
        public async Task<string> IssueRefreshTokenAsync(User user)
        {
            var refreshDays = int.TryParse(_configuration["Jwt:RefreshExpireDays"], out var d) ? d : 30;
            var token = GenerateSecureToken();

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
                CreatedAt = DateTime.UtcNow
            });
            await _refreshTokenRepository.SaveChangesAsync();
            return token;
        }

        /// <summary>Перевіряє refresh-токен та видає нову пару токенів (ротація).</summary>
        public async Task<(User?, string?, string?)> RefreshAsync(string refreshToken)
        {
            var stored = await _refreshTokenRepository.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (stored == null || !stored.IsActive)
            {
                _logger.LogWarning("Спроба використати недійсний refresh-токен");
                return (null, null, null);
            }

            var user = await _userRepository.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserId == stored.UserId);
            if (user == null || user.IsDeleted)
            {
                return (null, null, null);
            }

            // Ротація: старий токен відкликається, видається новий
            stored.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepository.Update(stored);
            await _refreshTokenRepository.SaveChangesAsync();

            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = await IssueRefreshTokenAsync(user);
            return (user, accessToken, newRefreshToken);
        }

        /// <summary>Відкликає refresh-токен (вихід із системи).</summary>
        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var stored = await _refreshTokenRepository.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (stored == null || stored.RevokedAt != null)
            {
                return false;
            }

            stored.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepository.Update(stored);
            await _refreshTokenRepository.SaveChangesAsync();
            return true;
        }

        /// <summary>Створює токен скидання паролю та надсилає його на email. Не розкриває наявність акаунта.</summary>
        public async Task<bool> RequestPasswordResetAsync(string email, string lang = "ua")
        {
            var normalizedEmail = email.ToLower().Trim();
            var user = await _userRepository.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || user.IsDeleted)
            {
                _logger.LogInformation("Запит на скидання паролю для невідомого email: {Email}", normalizedEmail);
                return false;
            }

            var token = GenerateSecureToken();
            await _passwordResetTokenRepository.AddAsync(new PasswordResetToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(PasswordResetTokenLifetimeHours),
                CreatedAt = DateTime.UtcNow
            });
            await _passwordResetTokenRepository.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, token, lang);
            _logger.LogInformation("Створено токен скидання паролю для {Email}", user.Email);
            return true;
        }

        /// <summary>Встановлює новий пароль за чинним токеном скидання (одноразово).</summary>
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var stored = await _passwordResetTokenRepository.FirstOrDefaultAsync(t => t.Token == token);
            if (stored == null || !stored.IsValid)
            {
                _logger.LogWarning("Спроба скидання паролю за недійсним токеном");
                return false;
            }

            var user = await _userRepository.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserId == stored.UserId);
            if (user == null)
            {
                return false;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            stored.UsedAt = DateTime.UtcNow;
            _passwordResetTokenRepository.Update(stored);
            // Репозиторії працюють зі спільним DbContext, тож зберігаємо обидві зміни одним викликом
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Пароль користувача {Email} успішно скинуто", user.Email);
            return true;
        }

        // Криптографічно стійкий токен (64 випадкових байти, URL-безпечне кодування)
        private static string GenerateSecureToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
    }
}
