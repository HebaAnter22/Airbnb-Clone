using API.DTOs;
using API.Models;

namespace API.Services
{
    public interface IAuthService
    {
        Task<User?> Register(RegisterUserDto userDto);
        Task<TokenResponseDto?> Login(LoginUserDto userDto);
        Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request);

        Task<User?> GetUserById(string id);
        Task<TokenResponseDto> CreateTokenResponse(User? user);
        Task<User> UpdateUser(User user);
        Task<User?> GetUserByEmail(string email);
        Task<User> CreateUser(User user);



    }
}
