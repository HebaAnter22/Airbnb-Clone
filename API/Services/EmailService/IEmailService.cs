using API.Models;

namespace API.Services.EmailService
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(User user, string resetToken);
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    }
}
