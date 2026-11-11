using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null, string? templateType = null, string? referenceId = null);
        // Фактичне надсилання листа (викликається фоновим сервісом черги)
        Task SendNowAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null, string? templateType = null, string? referenceId = null);
        Task SendOrderConfirmationAsync(string email, string orderNumber, string lang = "ua");
        Task SendOrderCancellationAsync(string email, string orderNumber, string? reason = null, string lang = "ua");
        Task SendPaymentConfirmationAsync(string email, string orderNumber, decimal amount, string lang = "ua");
        Task SendPaymentFailedAsync(string email, string orderNumber, string lang = "ua");
        Task SendStudentVerificationEmailAsync(string email, string status, string lang = "ua", int? discountPercent = null);
        Task SendStudentExpiryReminderEmailAsync(string email, int daysLeft, string lang = "ua");
        Task SendPasswordResetEmailAsync(string email, string resetToken, string lang = "ua");
    }
    public class EmailService : IEmailService
    {
        // Кількість спроб відправлення листа при тимчасових збоях SMTP
        private const int MaxAttempts = 3;
        // Статуси запису журналу відправлень
        private const string StatusSent = "Sent";
        private const string StatusFailed = "Failed";
        // Проста перевірка формату email: локальна частина @ домен . зона
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        // Рендерер HTML-шаблонів листів із папки EmailTemplates
        private readonly IEmailTemplateRenderer _templateRenderer;
        // Окремий scope для запису в журнал SentEmails (не втручається в поточний UnitOfWork)
        private readonly IServiceScopeFactory _scopeFactory;
        // Черга фонового відправлення листів
        private readonly IEmailQueue _queue;
        // Чи надсилати листи асинхронно через фоновий сервіс
        private readonly bool _useQueue;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IEmailTemplateRenderer templateRenderer, IServiceScopeFactory scopeFactory, IEmailQueue queue)
        {
            _configuration = configuration;
            _logger = logger;
            _templateRenderer = templateRenderer;
            _scopeFactory = scopeFactory;
            _queue = queue;
            // За замовчуванням листи ставляться в чергу (можна вимкнути через Features:EmailQueueEnabled=false)
            _useQueue = configuration.GetValue<bool?>("Features:EmailQueueEnabled") ?? true;
        }
        public async Task SendOrderConfirmationAsync(string email, string orderNumber, string lang = "ua")
        {
            string subject = lang == "en" ? "Order Confirmation" : "Підтвердження замовлення";
            string body = _templateRenderer.Render("OrderConfirmation", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["OrderNumber"] = orderNumber,
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "OrderConfirmation", referenceId: orderNumber);
        }
        public async Task SendOrderCancellationAsync(string email, string orderNumber, string? reason = null, string lang = "ua")
        {
            string subject = lang == "en" ? "Order Cancelled" : "Замовлення скасовано";
            string reasonText = string.IsNullOrWhiteSpace(reason)
                ? (lang == "en" ? "not specified" : "не вказана")
                : reason!;
            string body = _templateRenderer.Render("OrderCancelled", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["OrderNumber"] = orderNumber,
                ["Reason"] = reasonText,
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "OrderCancelled", referenceId: orderNumber);
        }
        public async Task SendPaymentConfirmationAsync(string email, string orderNumber, decimal amount, string lang = "ua")
        {
            string subject = lang == "en" ? "Payment Received" : "Оплата отримана";
            string body = _templateRenderer.Render("OrderPaymentSuccess", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["OrderNumber"] = orderNumber,
                ["Amount"] = amount.ToString("0.00"),
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "OrderPaymentSuccess", referenceId: orderNumber);
        }
        public async Task SendPaymentFailedAsync(string email, string orderNumber, string lang = "ua")
        {
            string subject = lang == "en" ? "Payment Failed" : "Оплата не вдалася";
            string body = _templateRenderer.Render("OrderPaymentFailed", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["OrderNumber"] = orderNumber,
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "OrderPaymentFailed", referenceId: orderNumber);
        }
        public async Task SendStudentVerificationEmailAsync(string email, string status, string lang = "ua", int? discountPercent = null)
        {
            string subject = lang == "en" ? "Student Status Verified" : "Студентський статус підтверджено";
            // Блок про призначену знижку додається лише коли відсоток відомий (мовозалежний)
            string discountBlock = discountPercent.HasValue
                ? (lang == "en"
                    ? $"<p>You have been granted a student discount of <b>{discountPercent}%</b>.</p>"
                    : $"<p>Вам призначено студентську знижку <b>{discountPercent}%</b>.</p>")
                : string.Empty;
            string body = _templateRenderer.Render("StudentVerified", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["Status"] = status,
                ["DiscountBlock"] = discountBlock,
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "StudentVerified", referenceId: email);
        }
        public async Task SendStudentExpiryReminderEmailAsync(string email, int daysLeft, string lang = "ua")
        {
            string subject = lang == "en" ? "Student Status Expiring Soon" : "Студентський статус скоро завершиться";
            string body = _templateRenderer.Render("StudentStatusExpiringSoon", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["DaysLeft"] = daysLeft.ToString(),
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "StudentStatusExpiringSoon", referenceId: email);
        }
        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string lang = "ua")
        {
            string subject = lang == "en" ? "Password Reset" : "Скидання паролю";
            string body = _templateRenderer.Render("PasswordReset", new Dictionary<string, string>
            {
                ["Title"] = subject,
                ["ResetToken"] = resetToken,
            }, lang);
            await SendEmailAsync(email, subject, body, templateType: "PasswordReset", referenceId: resetToken);
        }
        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null, string? templateType = null, string? referenceId = null)
        {
            // Асинхронний режим: лист ставиться в чергу, а відправляє його фоновий сервіс
            if (_useQueue)
            {
                _queue.Enqueue(new EmailMessage
                {
                    To = to,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    Cc = cc,
                    Bcc = bcc,
                    TemplateType = templateType,
                    ReferenceId = referenceId,
                });
                _logger.LogInformation("Email queued for {To} (subject: '{Subject}')", to, subject);
                return;
            }
            await SendNowAsync(to, subject, htmlBody, cc, bcc, templateType, referenceId);
        }
        // Фактичне надсилання листа по SMTP із записом у журнал (викликається за потреби або фоновим сервісом)
        public async Task SendNowAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null, string? templateType = null, string? referenceId = null)
        {
            // Валідація формату адреси отримувача: некоректні листи не відправляються
            if (!IsValidEmail(to))
            {
                _logger.LogWarning("Email not sent: invalid recipient address '{To}'", to);
                return;
            }
            // Ідемпотентність: якщо лист цього типу для цієї події вже надіслано - пропускаємо
            var trackingEnabled = !string.IsNullOrWhiteSpace(templateType) && !string.IsNullOrWhiteSpace(referenceId);
            if (trackingEnabled && await HasAlreadyBeenSentAsync(templateType!, referenceId!))
            {
                _logger.LogInformation("Duplicate email skipped (TemplateType: {TemplateType}, ReferenceId: {ReferenceId})", templateType, referenceId);
                return;
            }
            var smtpSettings = _configuration.GetSection("Smtp");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            // Якщо SMTP не налаштований (порожній або дефолтний логін) - лише логування (graceful fallback)
            if (string.IsNullOrWhiteSpace(username) || username == "your-email@gmail.com")
            {
                _logger.LogInformation("[EMAIL STUB] To: {To}, Subject: {Subject}", to, subject);
                return;
            }
            var host = smtpSettings["Server"];
            var port = int.TryParse(smtpSettings["Port"], out var parsedPort) ? parsedPort : 587;
            var enableSsl = !bool.TryParse(smtpSettings["EnableSsl"], out var parsedSsl) || parsedSsl;
            var sent = false;
            string? lastError = null;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var client = new SmtpClient(host)
                    {
                        Port = port,
                        Credentials = new NetworkCredential(username, password),
                        EnableSsl = enableSsl,
                    };
                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpSettings["SenderEmail"]!, smtpSettings["SenderName"]),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(to);
                    AddRecipients(mailMessage.CC, cc);
                    AddRecipients(mailMessage.Bcc, bcc);
                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent to {To} (subject: '{Subject}') on attempt {Attempt}", to, subject, attempt);
                    sent = true;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    // Повторна спроба при тимчасовому збої SMTP (експоненційна пауза)
                    if (attempt < MaxAttempts)
                    {
                        _logger.LogWarning(ex, "Attempt {Attempt}/{Max} failed to send email to {To}", attempt, MaxAttempts, to);
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                    }
                    else
                    {
                        // Останній збій: збереження виключення, щоб пошта не зупиняла бізнес-логіку
                        _logger.LogError(ex, "Failed to send email to {To} after {Max} attempts", to, MaxAttempts);
                    }
                }
            }
            // Запис у журнал для захисту від дублювань
            if (trackingEnabled)
            {
                await RecordSentEmailAsync(templateType!, referenceId!, to, subject, sent, lastError);
            }
        }
        // Перевіряє, чи лист цього типу для цієї події вже успішно надіслано
        private async Task<bool> HasAlreadyBeenSentAsync(string templateType, string referenceId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.SentEmails.AnyAsync(e => e.TemplateType == templateType
                                                         && e.ReferenceId == referenceId
                                                         && e.Status == StatusSent);
            }
            catch (Exception ex)
            {
                // Збій журналу не повинен блокувати відправлення
                _logger.LogError(ex, "Failed to check SentEmails log ({TemplateType}/{ReferenceId})", templateType, referenceId);
                return false;
            }
        }
        // Записує результат відправлення у журнал (оновлює наявний запис, якщо попередня спроба була невдалою)
        private async Task RecordSentEmailAsync(string templateType, string referenceId, string recipient, string subject, bool sent, string? error)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entry = await db.SentEmails.FirstOrDefaultAsync(e => e.TemplateType == templateType && e.ReferenceId == referenceId);
                if (entry == null)
                {
                    entry = new SentEmail { TemplateType = templateType, ReferenceId = referenceId };
                    db.SentEmails.Add(entry);
                }
                entry.Recipient = recipient;
                entry.Subject = subject;
                entry.Status = sent ? StatusSent : StatusFailed;
                entry.ErrorMessage = error;
                entry.SentAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Збій журналу не повинен зупиняти бізнес-логіку
                _logger.LogError(ex, "Failed to record SentEmail ({TemplateType}/{ReferenceId})", templateType, referenceId);
            }
        }
        // Додає список адрес (через кому або крапку з комою), пропускаючи некоректні
        private static void AddRecipients(MailAddressCollection collection, string? addresses)
        {
            if (string.IsNullOrWhiteSpace(addresses))
            {
                return;
            }
            foreach (var address in addresses.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (IsValidEmail(address))
                {
                    collection.Add(address);
                }
            }
        }
        private static bool IsValidEmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
        }
    }
}
