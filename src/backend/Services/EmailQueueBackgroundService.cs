namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Фоновий сервіс, що витягує листи з черги та надсилає їх,
    /// щоб не блокувати HTTP-відповідь на запит користувача.
    /// </summary>
    public class EmailQueueBackgroundService(
        IEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Фонова черга листів запущена");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Чекаємо появи хоч одного листа, не навантажуючи процесор
                    await queue.WaitForMessageAsync(stoppingToken);
                    // Вичитуємо все, що накопичилося в черзі
                    while (queue.TryDequeue(out var message))
                    {
                        await ProcessAsync(message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальне завершення при зупинці застосунку
            }
            logger.LogInformation("Фонова черга листів зупинена");
        }

        private async Task ProcessAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendNowAsync(message.To, message.Subject, message.HtmlBody,
                    message.Cc, message.Bcc, message.TemplateType, message.ReferenceId);
            }
            catch (Exception ex)
            {
                // Помилка одного листа не повинна зупиняти обробку черги
                logger.LogError(ex, "Помилка фонового відправлення листа до {To}", message.To);
            }
        }
    }
}
