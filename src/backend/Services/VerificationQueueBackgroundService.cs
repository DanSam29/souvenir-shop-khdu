using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Services
{
    // Фоновий сервіс, що періодично обробляє чергу повторної верифікації студентів.
    // Раз на заданий інтервал бере записи зі статусом Pending, час яких настав, і повторює верифікацію.
    public class VerificationQueueBackgroundService : BackgroundService
    {
        private const int DefaultIntervalMinutes = 5;
        private const int MaxAttempts = 5;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerificationQueueBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public VerificationQueueBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<VerificationQueueBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            var minutes = _configuration.GetValue<int?>("University:QueueProcessIntervalMinutes") ?? DefaultIntervalMinutes;
            _interval = TimeSpan.FromMinutes(minutes <= 0 ? DefaultIntervalMinutes : minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час обробки черги верифікації студентів");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var queueRepository = scope.ServiceProvider.GetRequiredService<IRepository<VerificationQueue>>();
            var universityService = scope.ServiceProvider.GetRequiredService<IUniversityService>();

            var now = DateTime.UtcNow;
            var pending = await queueRepository.Query()
                .Where(v => v.Status == "Pending" && v.NextAttemptAt <= now)
                .OrderBy(v => v.NextAttemptAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0) return;

            var retryDelayMinutes = _configuration.GetValue<int?>("University:QueueRetryDelayMinutes") ?? 60;

            foreach (var item in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                item.Status = "Processing";

                var outcome = await universityService.TryVerifyStudentByEmailAsync(item.Email, item.UserId);

                switch (outcome)
                {
                    case StudentVerificationOutcome.Verified:
                        item.Status = "Completed";
                        item.LastError = null;
                        break;

                    case StudentVerificationOutcome.Queued:
                        // University API досі недоступний — повертаємо запис у чергу
                        item.Status = "Pending";
                        item.Attempts += 1;
                        item.NextAttemptAt = DateTime.UtcNow.AddMinutes(retryDelayMinutes);
                        item.LastError = "University API недоступний";
                        break;

                    default:
                        // Студента не знайдено або домен не дозволений
                        item.Attempts += 1;
                        item.Status = item.Attempts >= MaxAttempts ? "Failed" : "Pending";
                        item.NextAttemptAt = DateTime.UtcNow.AddMinutes(retryDelayMinutes);
                        item.LastError = "Студента не знайдено або він неактивний";
                        break;
                }
            }

            await queueRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Оброблено {Count} записів черги верифікації студентів", pending.Count);
        }
    }
}
