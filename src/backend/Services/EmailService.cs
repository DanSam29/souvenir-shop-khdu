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
            string subject = $"Замовлення #{order.OrderNumber} прийнято";

            string body = $@"
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
            
            string subject = $"Оновлення статусу оплати замовлення #{order.OrderNumber}";

            string body = $@"
                <h2>Статус вашої оплати: {status}</h2>
                <p>Замовлення: #{order.OrderNumber}</p>
                {(!string.IsNullOrEmpty(comment) ? $"<p>Коментар: {comment}</p>" : "")}
                <hr/>
                <p>Дякуємо, що ви з нами!</p>";

            await SendEmailAsync(order.User.Email, subject, body);
        }

        public async Task SendStudentVerificationAsync(User user, string status)
        {
            string subject = "Оновлення вашого студентського статусу";

            string body = $@"
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