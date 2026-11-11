using KhduSouvenirShop.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Фоновий сервіс, що раз на добу деактивує протерміновані студентські статуси
    /// та надсилає нагадування про наближення терміну їх закінчення.
    /// </summary>
    public class StudentStatusExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<StudentStatusExpiryBackgroundService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<StudentStatusExpiryBackgroundService> _logger = logger;
        private const int DefaultIntervalHours = 24;
        // Нагадування надсилається, коли до закінчення статусу залишається не більше тижня
        private const int ReminderDaysBeforeExpiry = 7;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalHours = _configuration.GetValue<int?>("University:ExpiryCheckIntervalHours") ?? DefaultIntervalHours;
            if (intervalHours < 1) intervalHours = DefaultIntervalHours;
            var interval = TimeSpan.FromHours(intervalHours);
            _logger.LogInformation("Сервіс перевірки терміну дії студентських статусів запущено (інтервал {Hours} год)", intervalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час перевірки терміну дії студентських статусів");
                }
                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task ProcessAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var today = now.Date;

            // 1) Деактивація протермінованих статусів
            var expired = await userRepository.Query()
                .Where(u => u.StudentStatus != "NONE"
                            && u.StudentExpiresAt != null
                            && u.StudentExpiresAt.Value.Date < today)
                .ToListAsync(cancellationToken);
            foreach (var user in expired)
            {
                user.StudentStatus = "NONE";
                _logger.LogInformation("Студентський статус користувача {UserId} деактивовано (термін закінчився)", user.UserId);
            }
            if (expired.Count > 0)
            {
                await userRepository.SaveChangesAsync(cancellationToken);
            }

            // 2) Нагадування за тиждень до закінчення (одноразово для кожного терміну)
            var reminderWindowEnd = today.AddDays(ReminderDaysBeforeExpiry);
            var toRemind = await userRepository.Query()
                .Where(u => u.StudentStatus != "NONE"
                            && u.StudentExpiresAt != null
                            && u.StudentExpiresAt.Value.Date >= today
                            && u.StudentExpiresAt.Value.Date <= reminderWindowEnd
                            && u.StudentExpiryReminderSentAt == null)
                .ToListAsync(cancellationToken);
            foreach (var user in toRemind)
            {
                var daysLeft = (int)Math.Ceiling((user.StudentExpiresAt!.Value.Date - today).TotalDays);
                await emailService.SendStudentExpiryReminderEmailAsync(user.Email, daysLeft);
                user.StudentExpiryReminderSentAt = now;
                _logger.LogInformation("Нагадування про закінчення студентського статусу надіслано користувачу {UserId}", user.UserId);
            }
            if (toRemind.Count > 0)
            {
                await userRepository.SaveChangesAsync(cancellationToken);
            }

            if (expired.Count == 0 && toRemind.Count == 0)
            {
                _logger.LogDebug("Перевірка студентських статусів: змін немає");
            }
        }
    }
}
