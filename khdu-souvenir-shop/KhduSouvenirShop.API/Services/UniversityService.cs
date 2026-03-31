using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KhduSouvenirShop.API.Services
{
    public interface IUniversityService
    {
        Task<UniversityStudentInfo?> GetStudentInfoAsync(string email);
        Task<bool> VerifyAndApplyStudentStatusAsync(int userId);
    }

    public class UniversityService : IUniversityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UniversityService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public UniversityService(
            AppDbContext context, 
            ILogger<UniversityService> logger, 
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<UniversityStudentInfo?> GetStudentInfoAsync(string email)
        {
            var allowedDomains = _configuration.GetSection("University:AllowedDomains").Get<string[]>() ?? 
                                 new[] { "ksu.edu.ua", "student.ksu.edu.ua" };

            if (!allowedDomains.Any(domain => email.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            // Імітація затримки мережі
            await Task.Delay(500);

            // Логіка визначення статусу на основі GPA (імітація)
            // Припустимо, ми отримуємо ці дані з API університету
            return new UniversityStudentInfo
            {
                Email = email,
                IsActive = true,
                GPA = 4.5m, // Приклад
                FullName = "Ім'я Студента ХДУ",
                Faculty = "ФПМК"
            };
        }

        public async Task<bool> VerifyAndApplyStudentStatusAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var studentInfo = await GetStudentInfoAsync(user.Email);
            
            if (studentInfo != null && studentInfo.IsActive)
            {
                user.GPA = studentInfo.GPA;
                
                // Визначаємо категорію знижки за GPA
                if (user.GPA >= 4.8m) user.StudentStatus = "HIGH_ACHIEVER";
                else if (user.GPA >= 4.0m) user.StudentStatus = "SCHOLARSHIP";
                else user.StudentStatus = "REGULAR";

                user.StudentVerifiedAt = DateTime.UtcNow;
                user.StudentExpiresAt = DateTime.UtcNow.AddMonths(4); // Згідно з планом: термін дії 4 міс

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Користувач {UserId} автоматично верифікований як студент. Статус: {Status}", userId, user.StudentStatus);
                return true;
            }

            return false;
        }
    }

    public class UniversityStudentInfo
    {
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal GPA { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
    }
}