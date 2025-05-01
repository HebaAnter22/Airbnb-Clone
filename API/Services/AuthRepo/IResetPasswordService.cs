namespace API.Services.AuthRepo
{
    public interface IResetPasswordService
    {
       
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ValidateResetTokenAsync(string email, string token);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        
    }
}
