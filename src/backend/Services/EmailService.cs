using System.Net;
using System.Net.Mail;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    public interface IEmailService
    {
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
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                // Якщо налаштування порожні (за замовчуванням) - логується відправка
                if (smtpSettings["Username"] == "your-email@gmail.com")
                {
                    _logger.LogInformation("[EMAIL STUB] To: {To}, Subject: {Subject}", toEmail, subject);
                    return;
                }
                using var client = new SmtpClient(smtpSettings["Server"])
                {
                    Port = int.Parse(smtpSettings["Port"] ?? "587"),
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = true,
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["SenderEmail"]!, smtpSettings["SenderName"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {To}", toEmail);
            }
            catch (Exception ex)
            {
                // Збереження виключень, щоб збій пошти не зупиняв бізнес-логіку (принцип graceful fallback)
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            }
        }
    }
}
