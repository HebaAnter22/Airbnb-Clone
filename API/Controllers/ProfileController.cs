using System.Security.Claims;
using API.DTOs.Profile;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        public ProfileController(
            IProfileService _profileService
            )
        {
            this._profileService = _profileService;

        }


        // Gets base user information
        [HttpGet("user/{userId?}")]
        //[Authorize]
        public async Task<ActionResult<User>> GetUserProfile(string? userId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            string wantedUserId = ""; 
            if(userId!=null&& userId != currentUserId)
            {
                wantedUserId = userId;

            }
            else
            {
                wantedUserId = currentUserId;
            }
            var user= await _profileService.GetUserByIdAsync( wantedUserId );
            if (user == null)
            {
                return BadRequest("user not found");
            }
            return Ok(user);

        }

        // Gets host-specific information for a user
        [HttpGet("host/{userId?}")]
        //[Authorize]
        public async Task<ActionResult<Models.Host>> GetHostProfile(string? userId=null)
        {

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var wantedUserId = "";
            if(userId!=null && userId != currentUserId)
            {
                wantedUserId = userId;
            }
            else
            {

                wantedUserId = currentUserId;
            }



            var host = await _profileService.GetHostByUserIdAsync(int.Parse(wantedUserId));
            if (host == null)
            {
                return NotFound("Host not found");
            }
            return Ok(host);
        }
        [HttpPost("upload-profile-picture")]
        //[Authorize]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file type and size
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                return BadRequest("File size exceeds 5MB limit");
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-pictures");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update user's profile picture URL in database
                var user = await _profileService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Construct the URL that will be accessible from the frontend
                var fileUrl = $"https://localhost:7228/uploads/profile-pictures/{uniqueFileName}";
                user.ProfilePictureUrl = fileUrl;
                await _profileService.UpdateUserAsync(user);

                return Ok(new { fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("host/reviews/{userId?}")]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<HostReviewDto>>> GetHostReviews(string? userId=null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var wantedUserId = "";
            if (userId != null && userId != currentUserId)
            {
                wantedUserId = userId;
            }
            else
            {

                wantedUserId = currentUserId;
            }



            var reviews = await _profileService.GetHostReviewsAsync(int.Parse(wantedUserId));
            return Ok(reviews);
        }

        [HttpGet("host/Listings/{userId?}")]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<HostProfileListingsDto>>> GetHostListings(string? userId=null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var wantedUserId = "";
            if (userId != null && userId != currentUserId)
            {
                wantedUserId = userId;
            }
            else
            {

                wantedUserId = currentUserId;
            }
            var listings = await _profileService.GetHostListingsAsync(int.Parse(wantedUserId));
            return Ok(listings);
        }
        [HttpPost("favourites")]
        [Authorize]
        public async Task<IActionResult> AddToFavourites([FromBody] WhishListDto dto)
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (currentUserId == null)
            {
                return Unauthorized("User not found");
            }
            var favourite = new Favourite
            {
                UserId = currentUserId,
                PropertyId = dto.PropertyId,
                FavoritedAt = DateTime.UtcNow
            };
            var res = await _profileService.AddToFavouritesAsync(favourite);
            return Ok(res);


        }

        [HttpGet("favourites/{propertyId}")]
        [Authorize]
        public async Task<IActionResult> IsPropertyInFavorites(int propertyId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (currentUserId == null)
            {
                return Unauthorized("User not found");
            }

            var isFavorite = await _profileService.IsPropertyInFavoritesAsync(currentUserId, propertyId);
            return Ok(isFavorite);
        }


        [HttpGet("favourites")]
        [Authorize]
        public async Task<IActionResult> GetUserFavorites()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var favorites = await _profileService.GetUserFavoritesAsync(userId);
            return Ok(favorites);
        }

        [HttpGet("editProfile")]
        [Authorize]
        public
        async Task<ActionResult<EditProfileDto>> GetUserForEdit()
        {

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
           
            var user = await _profileService.getUserForEditProfile(currentUserId);
            if (user == null)
            {
                return BadRequest("user not found");
            }
            return Ok(user);
        }

        [HttpPut("editProfile")]
        [Authorize]
        public async Task<IActionResult> EditUserProfile([FromBody] EditProfileDto editProfileDto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _profileService.EditProfileAsync(editProfileDto);
            return Ok(user);
        }


        [HttpPost("guest/review")]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequestDto reviewDto)
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (currentUserId == null)
            {
                return Unauthorized("User not found");
            }
           
            reviewDto.ReviewerId = currentUserId;

            var res = await _profileService.addReview(reviewDto);
            return Ok(res);
        }

        [HttpGet("guest/reviews")]
        [Authorize]
        public async Task<IActionResult> GetUserReviews()
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (currentUserId == null)
            {
                return Unauthorized("User not found");
            }
            var reviews = await _profileService.GetUserReviewsAsync(currentUserId);
            return Ok(reviews);
        }
        [HttpGet("user/email-verification-status/")]
        [Authorize]
        public async Task<IActionResult> GetUserEmailVerificationStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("UserId not found");
            }
            var user = await _profileService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }
            return Ok(new { IsEmailVerified = user.EmailVerified });
        }
        [HttpPut("user/update-email")]
        [Authorize]
        public async Task<IActionResult> UpdateUserEmail([FromBody] EmailUpdateDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }
            var user = await _profileService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }
            user.Email = request.NewEmail;
            //check if newemail already exist
            var existingUser = await _profileService.emailExists(request.NewEmail);

            if (existingUser)
            {
                return BadRequest("Email already exists");
            }


            await _profileService.UpdateUserAsync(user);
            return Ok(new { Message = "Email updated successfully" });
        }
        [HttpPut("user/verify-email")]
        [Authorize]
        public async Task<IActionResult> VerifyUserEmail(EmailVerificationDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not found");
            }
            var user = await _profileService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }
            
            user.EmailVerified = dto.IsVerified;
            await _profileService.UpdateUserAsync(user);
            return Ok(new { Message = "Email verified successfully" });
        }
    }
}
