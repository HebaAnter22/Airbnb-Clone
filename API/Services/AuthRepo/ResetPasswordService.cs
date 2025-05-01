using System.Security.Cryptography;
using API.Data;
using API.Models;
using API.Services.EmailService;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AuthRepo
{
    
        public class ResetPasswordService : IResetPasswordService
        {
            private readonly AppDbContext _context;
            private readonly IEmailService _emailService;
            private readonly TimeSpan _tokenLifespan = TimeSpan.FromHours(1);

            public ResetPasswordService(AppDbContext context, IEmailService emailService)
            {
                _context = context;
                _emailService = emailService;
            }

            public async Task<bool> RequestPasswordResetAsync(string email)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist for security reasons
                    return false;
                }

                // Generate a reset token
                var resetToken = GenerateResetToken();

                // Set the reset token and expiry time
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpires = DateTime.UtcNow.Add(_tokenLifespan);

                await _context.SaveChangesAsync();

                // Send the reset email
                await _emailService.SendPasswordResetEmailAsync(user, resetToken);

                return true;
            }

            public async Task<bool> ValidateResetTokenAsync(string email, string token)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.PasswordResetToken == token);

                if (user == null || user.ResetTokenExpires <= DateTime.UtcNow)
                {
                    return false;
                }

                return true;
            }

            public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.PasswordResetToken == token);

                if (user == null || user.ResetTokenExpires <= DateTime.UtcNow)
                {
                    return false;
                }

                // Hash the new password
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, newPassword);

                // Clear the reset token data
                user.PasswordResetToken = null;
                user.ResetTokenExpires = null;

                await _context.SaveChangesAsync();
                return true;
            }

            private string GenerateResetToken()
            {
                var randomBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }

                return Convert.ToBase64String(randomBytes);
            }
        }

       
    
}
