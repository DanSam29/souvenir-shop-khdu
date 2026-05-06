using System.Net;
using System.Net.Mail;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string email, string orderNumber, string lang = "ua");
        Task SendPaymentConfirmationAsync(string email, string orderNumber, decimal amount, string lang = "ua");
        Task SendStudentVerificationEmailAsync(string email, string status, string lang = "ua");
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

        public async Task SendPaymentConfirmationAsync(string email, string orderNumber, decimal amount, string lang = "ua")
        {
            string subject = lang == "en" ? "Payment Received" : "Оплата отримана";
            string body = lang == "en"
                ? $"<h1>Payment successful!</h1><p>We received your payment of {amount} UAH for order {orderNumber}.</p>"
                : $"<h1>Оплата успішна!</h1><p>Ми отримали вашу оплату в розмірі {amount} грн за замовлення {orderNumber}.</p>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendStudentVerificationEmailAsync(string email, string status, string lang = "ua")
        {
            string subject = lang == "en" ? "Student Status Verified" : "Студентський статус підтверджено";
            string body = lang == "en"
                ? $"<h1>Congratulations!</h1><p>Your student status has been verified as: <b>{status}</b>. You can now use your discounts.</p>"
                : $"<h1>Вітаємо!</h1><p>Ваш статус студента підтверджено: <b>{status}</b>. Тепер ви можете користуватися своїми знижками.</p>";

            await SendEmailAsync(email, subject, body);
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