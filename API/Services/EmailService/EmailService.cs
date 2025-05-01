using API.Models;
using System.Net.Mail;
using System.Net;

namespace API.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpHost = configuration["EmailSettings:SmtpHost"];
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _smtpUsername = configuration["EmailSettings:SmtpUsername"];
            _smtpPassword = configuration["EmailSettings:SmtpPassword"];
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _fromName = configuration["EmailSettings:FromName"];
        }

        public async Task SendPasswordResetEmailAsync(User user, string resetToken)
        {
            // Create a URL-safe token for the reset link
            var encodedToken = WebUtility.UrlEncode(resetToken);
            var resetLink = $"http://localhost:4200/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";
            
            var subject = "Reset Your Password";
            var body = $@"
                <html>
                <body>
                    <h2>Reset Your Password</h2>
                    <p>Hello {user.FirstName},</p>
                    <p>We received a request to reset your password. Click the link below to set a new password:</p>
                    <p><a href='{resetLink}'>Reset Password</a></p>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                    <p>This link will expire in 1 hour for security reasons.</p>
                    <p>Thank you,<br/>The Airbnb Team</p>
                </body>
                </html>
            ";
            await SendEmailAsync(user.Email, subject, body, true);
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                
                message.To.Add(to);
                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                // Log the exception
                Console.WriteLine($"SMTP error: {ex.Message}, Status code: {ex.StatusCode}");
                throw; // Re-throw to be handled by caller
            }
        }
    }
}