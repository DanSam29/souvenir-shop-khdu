using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Services
{
    // Результат автоматичної верифікації студента
    public enum StudentVerificationOutcome
    {
        Verified,    // статус підтверджено та застосовано
        NotEligible, // email не належить дозволеному університетському домену
        NotFound,    // студента не знайдено або він неактивний
        Queued       // University API недоступний — запит додано в чергу на повтор
    }

    public interface IUniversityService
    {
        Task<UniversityStudentInfo?> GetStudentInfoAsync(string email);
        Task<bool> VerifyAndApplyStudentStatusAsync(int userId);
        Task<StudentVerificationOutcome> VerifyStudentByEmailAsync(string email, int userId);
        Task<StudentVerificationOutcome> TryVerifyStudentByEmailAsync(string email, int userId);
    }

    public class UniversityService : IUniversityService
    {
        // Відповідність студентського статусу розміру знижки (у відсотках)
        private static readonly IReadOnlyDictionary<string, int> StatusDiscounts = new Dictionary<string, int>
        {
            ["REGULAR"] = 10,
            ["SCHOLARSHIP"] = 15,
            ["HIGH_ACHIEVER"] = 20
        };

        private const int DefaultTimeoutSeconds = 5;
        private const int DefaultRetryDelayMinutes = 60;
        // Термін дії студентського статусу (місяців) — спільний для автоматичної та ручної верифікації
        public const int StudentStatusValidMonths = 4;
        private const decimal HighAchieverGpa = 4.5m;

        private readonly IUserRepository _userRepository;
        private readonly IRepository<VerificationQueue> _queueRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<UniversityService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly HttpClient _httpClient;

        public UniversityService(
            IUserRepository userRepository,
            IRepository<VerificationQueue> queueRepository,
            IEmailService emailService,
            ILogger<UniversityService> logger,
            IConfiguration configuration,
            IHostEnvironment environment,
            HttpClient httpClient)
        {
            _userRepository = userRepository;
            _queueRepository = queueRepository;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _httpClient = httpClient;
        }

        // Розмір знижки для студентського статусу (0, якщо статус не студентський)
        public static int GetDiscountPercent(string status) =>
            StatusDiscounts.TryGetValue(status, out var discount) ? discount : 0;

        // Перевірка, що email належить дозволеному університетському домену
        private bool IsAllowedDomain(string email)
        {
            var allowedDomains = _configuration.GetSection("University:AllowedDomains").Get<string[]>()
                                 ?? new[] { "ksu.edu.ua", "student.ksu.edu.ua" };
            return allowedDomains.Any(domain => email.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase));
        }

        // Заглушка вмикається лише в Development, коли не задано ні ApiKey (University:ApiKey / UNIVERSITY_API_KEY), ні BaseUrl
        private bool IsStubEnabled()
        {
            if (!_environment.IsDevelopment()) return false;
            var apiKey = _configuration["University:ApiKey"] ?? _configuration["UNIVERSITY_API_KEY"];
            var baseUrl = _configuration["University:BaseUrl"];
            return string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(baseUrl);
        }

        // Детермінована заглушка University API: статус визначається за локальною частиною email
        private UniversityStudentInfo? GetStubStudentInfo(string email)
        {
            var localPart = email.Split('@')[0].ToLowerInvariant();
            if (localPart.Contains("notfound"))
            {
                return null; // імітація 404 — студента не знайдено
            }

            var info = new UniversityStudentInfo
            {
                Email = email,
                IsActive = true,
                FullName = "Студент Тестовий",
                Faculty = "Факультет тестування"
            };
            if (localPart.Contains("scholarship"))
            {
                info.StudentStatus = "SCHOLARSHIP";
                info.GPA = 4.0m;
            }
            else if (localPart.Contains("high"))
            {
                // GPA >= 4.5 у мапінгу дає HIGH_ACHIEVER
                info.StudentStatus = "HIGH_ACHIEVER";
                info.GPA = 4.8m;
            }
            else
            {
                info.StudentStatus = "REGULAR";
                info.GPA = 3.5m;
            }
            return info;
        }

        // HTTP GET до University API. Повертає дані студента, null — якщо студента не знайдено (404),
        // кидає UniversityApiUnavailableException — якщо API недоступний (немає BaseUrl, мережа, timeout, 5xx).
        public async Task<UniversityStudentInfo?> GetStudentInfoAsync(string email)
        {
            if (!IsAllowedDomain(email)) return null;

            // У Development без налаштованого API використовується детермінована заглушка (Етап 9.5)
            if (IsStubEnabled())
            {
                _logger.LogDebug("University API (stub) для {Email}", email);
                return GetStubStudentInfo(email);
            }

            var baseUrl = _configuration["University:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new UniversityApiUnavailableException("University API не налаштований (University:BaseUrl порожній)");
            }

            var timeoutSeconds = _configuration.GetValue<int?>("University:RequestTimeoutSeconds") ?? DefaultTimeoutSeconds;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            var requestUrl = $"{baseUrl.TrimEnd('/')}/students/{Uri.EscapeDataString(email)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var apiKey = _configuration["University:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            try
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                if (!response.IsSuccessStatusCode)
                {
                    throw new UniversityApiUnavailableException($"University API повернув статус {(int)response.StatusCode}");
                }

                var payload = await response.Content.ReadFromJsonAsync<UniversityApiResponse>(cancellationToken: cts.Token);
                if (payload == null) return null;
                return new UniversityStudentInfo
                {
                    Email = email,
                    IsActive = payload.IsActive,
                    GPA = payload.Gpa ?? 0m,
                    StudentStatus = payload.StudentStatus ?? string.Empty,
                    FullName = payload.FullName ?? string.Empty,
                    Faculty = payload.Faculty ?? string.Empty
                };
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new UniversityApiUnavailableException($"Перевищено час очікування відповіді University API ({timeoutSeconds} с)");
            }
            catch (HttpRequestException ex)
            {
                throw new UniversityApiUnavailableException($"Помилка з'єднання з University API: {ex.Message}");
            }
        }

        // Основна точка входу: перевірка студента за email і застосування статусу зі знижкою.
        // Якщо University API недоступний — запит додається в чергу на повтор.
        public async Task<StudentVerificationOutcome> VerifyStudentByEmailAsync(string email, int userId)
        {
            if (!IsAllowedDomain(email)) return StudentVerificationOutcome.NotEligible;

            try
            {
                return await VerifyCoreAsync(email, userId);
            }
            catch (UniversityApiUnavailableException ex)
            {
                _logger.LogWarning(ex, "University API недоступний для {Email}; запит додано в чергу повторів", email);
                await EnqueueForRetryAsync(userId, email, ex.Message);
                return StudentVerificationOutcome.Queued;
            }
        }

        // Те саме, але без запису в чергу — використовується фоновим сервісом, який сам керує станом запису черги
        public async Task<StudentVerificationOutcome> TryVerifyStudentByEmailAsync(string email, int userId)
        {
            if (!IsAllowedDomain(email)) return StudentVerificationOutcome.NotEligible;

            try
            {
                return await VerifyCoreAsync(email, userId);
            }
            catch (UniversityApiUnavailableException)
            {
                return StudentVerificationOutcome.Queued;
            }
        }

        public async Task<bool> VerifyAndApplyStudentStatusAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            var outcome = await VerifyStudentByEmailAsync(user.Email, userId);
            return outcome == StudentVerificationOutcome.Verified;
        }

        // Одна спроба верифікації: HTTP-виклик API + застосування статусу
        private async Task<StudentVerificationOutcome> VerifyCoreAsync(string email, int userId)
        {
            var studentInfo = await GetStudentInfoAsync(email);
            if (studentInfo == null || !studentInfo.IsActive)
            {
                return StudentVerificationOutcome.NotFound;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return StudentVerificationOutcome.NotFound;

            await ApplyStudentStatusAsync(user, studentInfo);
            return StudentVerificationOutcome.Verified;
        }

        // Записує статус студента, термін дії та надсилає лист про призначену знижку
        private async Task ApplyStudentStatusAsync(User user, UniversityStudentInfo studentInfo)
        {
            user.GPA = studentInfo.GPA;
            user.StudentStatus = MapToStudentStatus(studentInfo);
            user.StudentVerifiedAt = DateTime.UtcNow;
            user.StudentExpiresAt = DateTime.UtcNow.AddMonths(StudentStatusValidMonths); // термін дії 4 місяці
            user.StudentExpiryReminderSentAt = null; // новий термін — нагадування дозволено знову
            await _userRepository.SaveChangesAsync();

            var discount = GetDiscountPercent(user.StudentStatus);
            await _emailService.SendStudentVerificationEmailAsync(user.Email, user.StudentStatus, user.PreferredLanguage, discount);
            _logger.LogInformation("Користувача {UserId} верифіковано як {Status} (знижка {Discount}%)",
                user.UserId, user.StudentStatus, discount);
        }

        // Мапінг response.studentStatus + GPA → наш enum
        private static string MapToStudentStatus(UniversityStudentInfo info)
        {
            if (info.GPA >= HighAchieverGpa) return "HIGH_ACHIEVER";
            var apiStatus = (info.StudentStatus ?? string.Empty).Trim().ToUpperInvariant();
            return apiStatus.Contains("SCHOLAR") ? "SCHOLARSHIP" : "REGULAR";
        }

        // Додає (або оновлює) запис черги на повторну верифікацію через заданий інтервал
        private async Task EnqueueForRetryAsync(int userId, string email, string error)
        {
            var retryDelayMinutes = _configuration.GetValue<int?>("University:QueueRetryDelayMinutes") ?? DefaultRetryDelayMinutes;
            var nextAttemptAt = DateTime.UtcNow.AddMinutes(retryDelayMinutes);

            var existing = await _queueRepository.Query()
                .FirstOrDefaultAsync(v => v.UserId == userId && (v.Status == "Pending" || v.Status == "Processing"));
            if (existing != null)
            {
                existing.Attempts += 1;
                existing.NextAttemptAt = nextAttemptAt;
                existing.LastError = Truncate(error, 500);
            }
            else
            {
                await _queueRepository.AddAsync(new VerificationQueue
                {
                    UserId = userId,
                    Email = email,
                    Attempts = 1,
                    NextAttemptAt = nextAttemptAt,
                    LastError = Truncate(error, 500),
                    Status = "Pending"
                });
            }
            await _queueRepository.SaveChangesAsync();
        }

        private static string Truncate(string value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
    }

    // Виняток, що сигналізує про недоступність University API (немає налаштувань, мережа, timeout, 5xx)
    public class UniversityApiUnavailableException : Exception
    {
        public UniversityApiUnavailableException(string message) : base(message) { }
    }

    public class UniversityStudentInfo
    {
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal GPA { get; set; }
        public string StudentStatus { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
    }

    // Структура відповіді зовнішнього University API
    public class UniversityApiResponse
    {
        [JsonPropertyName("studentStatus")]
        public string? StudentStatus { get; set; }
        [JsonPropertyName("gpa")]
        public decimal? Gpa { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        [JsonPropertyName("faculty")]
        public string? Faculty { get; set; }
    }
}
