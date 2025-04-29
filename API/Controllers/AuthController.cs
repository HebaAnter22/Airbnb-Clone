using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Google;
using System.Web;
using API.DTOs.Auth;

namespace API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        IAuthService _authService;
        public AuthController(IAuthService authService)
        {

            _authService = authService;

        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
        {
            var user = await _authService.Register(userDto);
            if (user == null)
            {
                return BadRequest("User already exists");
            }
            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto userDto)
        {
            try
            {

                var result = await _authService.Login(userDto);
                if (result == null)
                {
                    return Unauthorized("Invalid username or password");
                }
                return Ok(result);

            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 1. Debug token claims
                Console.WriteLine($"Token claims received:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"{claim.Type}: {claim.Value}");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                {
                    Console.WriteLine("Logout failed: NameIdentifier claim not found");
                    return Unauthorized("Invalid token");
                }

                Console.WriteLine($"Attempting logout for user ID: {userId}");

                // 2. Debug user lookup
                var user = await _authService.GetUserById(userId);
                if (user == null)
                {
                    Console.WriteLine($"Logout failed: User not found for ID {userId}");
                    return Unauthorized("Invalid token");
                }

                Console.WriteLine($"User found: {user.Email}");

                // 3. Debug refresh token cleanup
                Console.WriteLine($"Current refresh token: {user.RefreshToken}");
                Console.WriteLine($"Current refresh token expiry: {user.RefreshTokenExpiryTime}");

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;


                // 4. Debug user update
                var updatedUser = await _authService.UpdateUser(user);
                if (updatedUser == null)
                {
                    Console.WriteLine("Logout failed: User update failed");
                    return StatusCode(500, "An error occurred during logout");
                }
                Console.WriteLine($"User updated: {updatedUser.Email}");

                Console.WriteLine($"Post-update refresh token: {updatedUser?.RefreshToken}");

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout exception: {ex}");
                return StatusCode(500, "An error occurred during logout");
            }
        }
        [HttpPost("refresh-token")]
        [AllowAnonymous] // only for now >>>>>>>>>>>>>>>>>>>>>
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto tokenDto)
        {
            var result = await _authService.RefreshTokensAsync(tokenDto);
            if (result == null || result.AccessToken == null || result.RefreshToken == null)
            {
                return Unauthorized("Invalid refresh token");
            }
            return Ok(result);
        }


        [Authorize]
        [HttpGet]
        public IActionResult AutinticatedOnlyEndpoint()
        {
            return Ok("This is an authenticated only endpoint");
        }

        [Authorize(Roles = "Host")]
        [HttpGet("AddProp")]
        public IActionResult HostOnlyEndpoint()
        {
            return Ok("This is a host only endpoint");
        }


        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            var user = await _authService.GetUserById(userId);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                user.Email,
                user.FirstName,
                user.LastName,
            });
        }

        [HttpPost("google-auth")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest googleUser)
        {
            try
            {
                // Get or create user from Google information
                var user = await _authService.GetOrCreateGoogleUser(
                    googleUser.Email,
                    googleUser.FirstName,
                    googleUser.LastName
                );

                // Generate tokens for the user
                var tokenResponse = await _authService.CreateTokenResponse(user);

                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return BadRequest("Google authentication failed");

            var claims = authenticateResult.Principal.Claims;

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email claim not found");

            // Check if user exists
            var user = await _authService.GetUserByEmail(email);

            if (user == null)
            {
                // Create new user from Google info
                var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";
                var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";

                user = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "Guest",
                    PasswordHash = "" // No password for Google users
                };

                user = await _authService.CreateUser(user);
            }

            // Generate tokens
            var tokenResponse = await _authService.CreateTokenResponse(user);

            // Return tokens to the frontend
            return Redirect($"http://localhost:4200/login?access_token={tokenResponse.AccessToken}&refresh_token={tokenResponse.RefreshToken}");
        }


        [HttpPost("switch-to-host")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> SwitchToHost()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _authService.GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }
            user.Role = "Host";
            
            var updatedUser =  await _authService.UpdateUser(user);
            if (updatedUser == null)
            {
                return BadRequest("Failed to update user role");
            }

            var host =await _authService.CreateHost(user.Id);
            if (host == null)
            {
                return BadRequest("Failed to create host profile");
            }

            // Generate tokens
            var tokenResponse = await _authService.CreateTokenResponse(user);
            if (tokenResponse == null)
            {
                return BadRequest("Failed to generate tokens");
            }
            // Return tokens to the frontend
            return Ok(tokenResponse);




            //return Ok(new
            //            {
            //                user.Email,
            //                user.PasswordHash,
            //            }
            //    );
        }
        
    }

}
