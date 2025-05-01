using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services.AuthRepo;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Services.AuthRepo
{
    public class AuthService : IAuthService
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration configuration;
        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            this.configuration = configuration;
        }

        public async Task<TokenResponseDto?> Login(LoginUserDto userDto)
        {

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == userDto.Email);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userDto.Password)
                == PasswordVerificationResult.Success)
            {
                TokenResponseDto response = await CreateTokenResponse(user);
                return response;
            }
            return null;

        }



        public async Task<TokenResponseDto> CreateTokenResponse(User? user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshToken(user)
            };
        }


        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task<User> Register(RegisterUserDto userDto)
        {
            if (await _context.Users.AnyAsync(x => x.Email == userDto.Email))
            {
                //email already exist
                throw new Exception("Email already exists");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User();

                var hashedPassword = new PasswordHasher<User>().HashPassword(user, userDto.Password);

                user.PasswordHash = hashedPassword;
                user.Email = userDto.Email;
                user.FirstName = userDto.FirstName;
                user.LastName = userDto.LastName;
                user.Role = "Guest";
                user.ProfilePictureUrl = "https://localhost:7228/uploads/profile-pictures/default-profile.jpg";

                _context.Users.Add(user);
                await _context.SaveChangesAsync();


                //if (user.Role == "Host")
                //{

                //    var host = new Models.Host
                //    {
                //        HostId = user.Id,
                //    };

                //    _context.HostProfules.Add(host);
                //    await _context.SaveChangesAsync();
                //}


                await transaction.CommitAsync();
                return user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Handle the exception as needed
                Console.WriteLine($"Error saving user: {ex.Message}");
                throw new Exception("Error saving user to the database.", ex);
            }


        }
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("Jwt:Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDecriptor = new JwtSecurityToken
            (
                issuer: configuration.GetValue<string>("Jwt:Issuer"),
                audience: configuration.GetValue<string>("Jwt:Audience"),
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tokenDecriptor);
        }

        public async Task<User?> GetUserById(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id.ToString() == id);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            return user;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }

        }
        private async Task<string> GenerateAndSaveRefreshToken(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
        private async Task<User?> validateRefreshTokenAsync(int userId, string refreshToken)
        {

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                throw new Exception("Invalid refresh token");
            }
            return user;
        }
        public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await validateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            return await CreateTokenResponse(user);
        }
        public async Task<User> UpdateUser(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;


        }
        public async Task<User?> GetOrCreateGoogleUser(string email, string firstName, string lastName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "Guest",
                    PasswordHash = "",// No password for Google users,
                    ProfilePictureUrl = "https://localhost:7228/uploads/profile-pictures/default-profile.jpg"

                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();


            }
            return user;
        }

        public async Task<Models.Host> CreateHost(int id)
        {
            var host = new Models.Host
            {
                HostId = id,
                StartDate = DateTime.UtcNow,
                AboutMe = "",
                Work = "",
                Rating = 0,
                TotalReviews = 0,
                Education = "",
                Languages = "",
                IsVerified = false,
                LivesIn = "",
                DreamDestination = "",
                FunFact = "",
                Pets = "",
                ObsessedWith = "",
                SpecialAbout = "",

            };
            _context.HostProfules.Add(host);
            await _context.SaveChangesAsync();
            return host;
        }

        public async Task UpdatePasswordResetTimestamp(string email, DateTime resetTime)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {

                    throw new Exception("User not found");
                }


                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}