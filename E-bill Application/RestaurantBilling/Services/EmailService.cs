using System.Net;
using System.Net.Mail;

namespace RestaurantBilling.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body, string? attachmentPath = null)
        {
            var host = _configuration["Smtp:Host"] ?? "localhost";
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"] ?? "";
            var password = _configuration["Smtp:Password"] ?? "";
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@restaurant.com";
            var fromName = _configuration["Smtp:FromName"] ?? "RestoPOS";

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                message.To.Add(to);

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    message.Attachments.Add(new Attachment(attachmentPath));
                }

                _logger.LogInformation($"Sending real email to {to} via {host}...");
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                // We rethrow or handle? For a background service, logging is usually enough, 
                // but let's rethrow so the caller knows it failed.
                throw;
            }
        }
    }
}
