using System.Net;
using System.Net.Mail;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order, User user);
        Task SendPaymentStatusAsync(Order order, string status, string? comment = null);
        Task SendStudentVerificationAsync(User user, string status);
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

        public async Task SendOrderConfirmationAsync(Order order, User user)
        {
            bool isEn = user.Language?.ToLower() == "en";
            
            string subject = isEn 
                ? $"Order #{order.OrderNumber} received" 
                : $"Замовлення #{order.OrderNumber} прийнято";

            string body = isEn ? $@"
                <h1>Thank you for your order, {user.FirstName}!</h1>
                <p>Your order <strong>#{order.OrderNumber}</strong> has been successfully created.</p>
                <p>Amount to pay: <strong>{order.TotalAmount:F2} UAH</strong></p>
                <p>Status: {order.Status}</p>
                <hr/>
                <p>Best regards, KSU Souvenir Shop Team</p>"
                : $@"
                <h1>Дякуємо за замовлення, {user.FirstName}!</h1>
                <p>Ваше замовлення <strong>#{order.OrderNumber}</strong> успішно створено.</p>
                <p>Сума до сплати: <strong>{order.TotalAmount:F2} грн</strong></p>
                <p>Статус: {order.Status}</p>
                <hr/>
                <p>З повагою, Команда KSU Souvenir Shop</p>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPaymentStatusAsync(Order order, string status, string? comment = null)
        {
            if (order.User == null) return;
            
            bool isEn = order.User.Language?.ToLower() == "en";

            string subject = isEn
                ? $"Payment status update for order #{order.OrderNumber}"
                : $"Оновлення статусу оплати замовлення #{order.OrderNumber}";

            string body = isEn ? $@"
                <h2>Your payment status: {status}</h2>
                <p>Order: #{order.OrderNumber}</p>
                {(!string.IsNullOrEmpty(comment) ? $"<p>Comment: {comment}</p>" : "")}
                <hr/>
                <p>Thank you for being with us!</p>"
                : $@"
                <h2>Статус вашої оплати: {status}</h2>
                <p>Замовлення: #{order.OrderNumber}</p>
                {(!string.IsNullOrEmpty(comment) ? $"<p>Коментар: {comment}</p>" : "")}
                <hr/>
                <p>Дякуємо, що ви з нами!</p>";

            await SendEmailAsync(order.User.Email, subject, body);
        }

        public async Task SendStudentVerificationAsync(User user, string status)
        {
            bool isEn = user.Language?.ToLower() == "en";

            string subject = isEn
                ? "Your student status update"
                : "Оновлення вашого студентського статусу";

            string body = isEn ? $@"
                <h2>Congratulations, {user.FirstName}!</h2>
                <p>Your student status at KSU Souvenir Shop has been updated to: <strong>{status}</strong>.</p>
                <p>You now have access to special student discounts.</p>
                <hr/>
                <p>Happy shopping!</p>"
                : $@"
                <h2>Вітаємо, {user.FirstName}!</h2>
                <p>Ваш студентський статус у KSU Souvenir Shop було оновлено на: <strong>{status}</strong>.</p>
                <p>Тепер вам доступні спеціальні знижки для студентів.</p>
                <hr/>
                <p>Приємних покупок!</p>";

            await SendEmailAsync(user.Email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                
                // Якщо налаштування порожні (за замовчуванням), просто логуємо відправку
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
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
                // Ми не викидаємо виключення, щоб збій пошти не зупиняв бізнес-логіку (згідно з принципом graceful fallback)
            }
        }
    }
}