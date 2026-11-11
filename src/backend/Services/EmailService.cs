using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null);
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
        // Проста перевірка формату email: локальна частина @ домен . зона
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task SendOrderConfirmationAsync(string email, string orderNumber, string lang = "ua")
        {
            string subject = lang == "en" ? "Order Confirmation" : "Підтвердження замовлення";
            string body = lang == "en" 
                ? $"<h1>Thank you for your order!</h1><p>Your order number: <b>{orderNumber}</b></p>"
                : $"<h1>Дякуємо за ваше замовлення!</h1><p>Номер вашого замовлення: <b>{orderNumber}</b></p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendOrderCancellationAsync(string email, string orderNumber, string? reason = null, string lang = "ua")
        {
            string subject = lang == "en" ? "Order Cancelled" : "Замовлення скасовано";
            string reasonText = string.IsNullOrWhiteSpace(reason)
                ? (lang == "en" ? "not specified" : "не вказана")
                : reason!;
            string body = lang == "en"
                ? $"<h1>Your order has been cancelled</h1><p>Order number: <b>{orderNumber}</b></p><p>Reason: {reasonText}</p>"
                : $"<h1>Ваше замовлення скасовано</h1><p>Номер замовлення: <b>{orderNumber}</b></p><p>Причина: {reasonText}</p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendPaymentConfirmationAsync(string email, string orderNumber, decimal amount, string lang = "ua")
        {
            string subject = lang == "en" ? "Payment Received" : "Оплата отримана";
            string body = lang == "en"
                ? $"<h1>Payment successful!</h1><p>We received your payment of {amount} UAH for order {orderNumber}.</p>"
                : $"<h1>Оплата успішна!</h1><p>Ми отримали вашу оплату в розмірі {amount} грн за замовлення {orderNumber}.</p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendPaymentFailedAsync(string email, string orderNumber, string lang = "ua")
        {
            string subject = lang == "en" ? "Payment Failed" : "Оплата не вдалася";
            string body = lang == "en"
                ? $"<h1>Payment failed</h1><p>Unfortunately, the payment for your order {orderNumber} was not successful. The order has been cancelled. You can try again.</p>"
                : $"<h1>Оплата не вдалася</h1><p>На жаль, оплата за замовлення {orderNumber} не пройшла. Замовлення скасовано. Ви можете спробувати ще раз.</p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendStudentVerificationEmailAsync(string email, string status, string lang = "ua", int? discountPercent = null)
        {
            string subject = lang == "en" ? "Student Status Verified" : "Студентський статус підтверджено";
            // Рядок про призначену знижку додається лише коли відсоток відомий
            string discountUa = discountPercent.HasValue ? $"<p>Вам призначено студентську знижку <b>{discountPercent}%</b>.</p>" : string.Empty;
            string discountEn = discountPercent.HasValue ? $"<p>You have been granted a student discount of <b>{discountPercent}%</b>.</p>" : string.Empty;
            string body = lang == "en"
                ? $"<h1>Congratulations!</h1><p>Your student status has been verified as: <b>{status}</b>.</p>{discountEn}"
                : $"<h1>Вітаємо!</h1><p>Ваш статус студента підтверджено: <b>{status}</b>.</p>{discountUa}";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendStudentExpiryReminderEmailAsync(string email, int daysLeft, string lang = "ua")
        {
            string subject = lang == "en" ? "Student Status Expiring Soon" : "Студентський статус скоро завершиться";
            string body = lang == "en"
                ? $"<h1>Heads up!</h1><p>Your student status will expire in <b>{daysLeft}</b> day(s). Please refresh it in your profile to keep your student discount.</p>"
                : $"<h1>Нагадування!</h1><p>Ваш студентський статус завершиться через <b>{daysLeft}</b> дн. Будь ласка, поновіть його у профілі, щоб зберегти студентську знижку.</p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string lang = "ua")
        {
            var clientUrl = _configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:3000";
            var resetLink = $"{clientUrl}/reset-password?token={resetToken}";
            string subject = lang == "en" ? "Password Reset" : "Скидання паролю";
            string body = lang == "en"
                ? $"<h1>Password reset</h1><p>Follow the link to set a new password: <a href=\"{resetLink}\">Reset password</a></p><p>The link expires in 1 hour. If you didn't request this, ignore this email.</p>"
                : $"<h1>Скидання паролю</h1><p>Перейдіть за посиланням, щоб встановити новий пароль: <a href=\"{resetLink}\">Скинути пароль</a></p><p>Посилання дійсне 1 годину. Якщо ви не робили цей запит, просто проігноруйте лист.</p>";
            await SendEmailAsync(email, subject, body);
        }
        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? cc = null, string? bcc = null)
        {
            // Валідація формату адреси отримувача: некоректні листи не відправляються
            if (!IsValidEmail(to))
            {
                _logger.LogWarning("Email not sent: invalid recipient address '{To}'", to);
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
                    return;
                }
                catch (Exception ex)
                {
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
