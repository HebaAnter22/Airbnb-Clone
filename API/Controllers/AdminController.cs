using AirBnb.BL.Dtos.BookingDtos;
using API.DTOs;
using API.DTOs.Admin;
using API.DTOs.HostVerification;
using API.DTOs.property;
using API.Models;
using API.Services.AdminRepo;
using API.Services.BookingRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Tsp;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] 
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IBookingRepository _bookingRepository;
        public AdminController(IAdminRepository adminRepository, IBookingRepository bookingRepository)
        {
            _adminRepository = adminRepository;
            _bookingRepository = bookingRepository;
        }

        [HttpGet("GetVerificationsByHostId/{hostId}")]
        public async Task<IActionResult> GetVerificationsByHostId(int hostId)
        {
            var verification = await _adminRepository.GetVerificationByhostsAsync(hostId);
            if (verification == null)
                return NotFound();
            var dto = new HostVerificationOutputDTO
            {
                Id = verification.Id,
                HostId = verification.HostId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl1 = verification.DocumentUrl1,
                VerificationDocumentUrl2 = verification.DocumentUrl2,
                SubmittedAt = verification.SubmittedAt
            };
            return Ok(dto);
        }

        public class VerifyHostRequest
        {
            public bool IsVerified { get; set; }
        }

        [HttpPut("hosts/{hostId}/verify")]
        public async Task<IActionResult> VerifyHost(int hostId, [FromBody] VerifyHostRequest request)
        {
            try
            {
                var verification = await _adminRepository.GetVerificationByhostsAsync(hostId);
                if (verification == null)
                    return NotFound("Verification not found.");
                var result = await _adminRepository.ConfirmHostVerificationAsync(verification.Id, hostId);
                if (!result)
                    return NotFound();
                var success = await _adminRepository.ApproveHostAsync(hostId, request.IsVerified);
                if (!success)
                    return NotFound("Host not found.");

                return Ok(new { Message = $"Host {(request.IsVerified ? "verified" : "unverified")} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("hosts/top-paid")]
        public async Task<IActionResult> GetTopPaidHosts(int count = 5)
        {
            try
            {
                var hosts = await _adminRepository.GetAllHostsAsync();
                var dtos = new List<HostDto>();

                foreach (var h in hosts)
                {
                    var totalIncome = await _bookingRepository.GetTotalIncomeForHostAsync(h.Id);
                    
                    // Skip hosts with no income
                    if (totalIncome <= 0) continue;
                    
                    var rating = h.Host?.Properties?
                                            .Where(p => p != null)
                                            .SelectMany(p => p.Bookings ?? Enumerable.Empty<Booking>())
                                            .Where(b => b?.Review != null)
                                            .Select(b => b.Review.Rating)
                                            .ToList() ?? new List<int>(); 
                    var averageRating = rating.Any() ? Math.Round(rating.Average(), 2) : 0;

                    dtos.Add(new HostDto
                    {
                        Id = h.Id,
                        FirstName = h.FirstName,
                        LastName = h.LastName,
                        Email = h.Email,
                        PhoneNumber = h.PhoneNumber,
                        Role = h.Role,
                        IsVerified = h.Host?.IsVerified ?? false,
                        ProfilePictureUrl = h.ProfilePictureUrl ?? string.Empty,
                        StartDate = h.CreatedAt,
                        TotalReviews = rating.Count,
                        Rating = (decimal)averageRating,
                        PropertiesCount = h.Host?.Properties?.Count ?? 0,
                        TotalIncome = totalIncome
                    });
                }

                // Order by total income and take requested count
                var topPaidHosts = dtos
                    .OrderByDescending(h => h.TotalIncome)
                    .Take(count)
                    .ToList();

                return Ok(topPaidHosts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("guests/top-spending")]
        public async Task<IActionResult> GetTopSpendingGuests(int count = 5)
        {
            try
            {
                var guests = await _adminRepository.GetAllGuestsAsync();
                var dtos = new List<GuestDto>();
                foreach (var g in guests)
                {
                    var totalSpent = await _bookingRepository.GetTotalSpentByGuestAsync(g.Id);
                    
                    // Skip guests with no spending
                    if (totalSpent <= 0) continue;
                    
                    dtos.Add(new GuestDto
                    {
                        Id = g.Id,
                        FirstName = g.FirstName,
                        LastName = g.LastName,
                        Email = g.Email,
                        PhoneNumber = g.PhoneNumber,
                        ProfilePictureUrl = g.ProfilePictureUrl,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt,
                        LastLogin = g.LastLogin,
                        AccountStatus = g.AccountStatus,
                        Role = g.Role,
                        DateOfBirth = g.DateOfBirth,
                        BookingsCount = g.Bookings.Count,
                        TotalSpent = totalSpent
                    });
                }
                
                // Order by total spent and take requested count
                var topSpendingGuests = dtos
                    .OrderByDescending(g => g.TotalSpent)
                    .Take(count)
                    .ToList();
                    
                return Ok(topSpendingGuests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("hosts")]
        public async Task<IActionResult> GetAllHosts()
        {
            try
            {
                var hosts = await _adminRepository.GetAllHostsAsync();
                var dtos = new List<HostDto>();

                foreach (var h in hosts)
                {
                    var totalIncome = await _bookingRepository.GetTotalIncomeForHostAsync(h.Id);
                    var rating = h.Host?.Properties?
                                            .Where(p => p != null)
                                            .SelectMany(p => p.Bookings ?? Enumerable.Empty<Booking>())
                                            .Where(b => b?.Review != null)
                                            .Select(b => b.Review.Rating)
                                            .ToList() ?? new List<int>(); 
                    var averageRating = rating.Any() ? Math.Round(rating.Average(), 2) : 0;

                    dtos.Add(new HostDto
                    {
                        Id = h.Id,
                        FirstName = h.FirstName,
                        LastName = h.LastName,
                        Email = h.Email,
                        PhoneNumber = h.PhoneNumber,
                        Role = h.Role,
                        IsVerified = h.Host?.IsVerified ?? false,
                        ProfilePictureUrl = h.ProfilePictureUrl ?? string.Empty,
                        StartDate = h.CreatedAt,
                        TotalReviews = rating.Count,
                        Rating = (decimal)averageRating,
                        PropertiesCount = h.Host?.Properties?.Count ?? 0,
                        TotalIncome = totalIncome
                    });
                }

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("guests")]
        public async Task<IActionResult> GetAllGuests()
        {
            try
            {
                var guests = await _adminRepository.GetAllGuestsAsync();
                var dtos = new List<GuestDto>();
                foreach (var g in guests)
                {
                    var totalSpent = await _bookingRepository.GetTotalSpentByGuestAsync(g.Id);
                    dtos.Add(new GuestDto
                    {
                        Id = g.Id,
                        FirstName = g.FirstName,
                        LastName = g.LastName,
                        Email = g.Email,
                        PhoneNumber = g.PhoneNumber,
                        ProfilePictureUrl = g.ProfilePictureUrl,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt,
                        LastLogin = g.LastLogin,
                        AccountStatus = g.AccountStatus,
                        Role = g.Role,
                        DateOfBirth = g.DateOfBirth,
                        BookingsCount = g.Bookings.Count,
                        TotalSpent = totalSpent
                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("hosts/verified")]
        public async Task<IActionResult> GetVerifiedHosts()
        {
            try
            {
                var hosts = await _adminRepository.GetAllHostsAsync();
                var verifiedHosts = hosts.Where(h => h.Host?.IsVerified == true).ToList();
                //var verifiedHosts = await _adminRepository.GetVerifiedHostsAsync();
                var dtos = new List<HostDto>();
                foreach (var host in verifiedHosts)
                {
                    var totalIncome = await _bookingRepository.GetTotalIncomeForHostAsync(host.Id);
                    
                    // Add null checks to prevent exceptions
                    var rating = host.Host?.Properties?
                        .Where(p => p != null)
                        .SelectMany(p => p.Bookings ?? Enumerable.Empty<Booking>())
                        .Where(b => b?.Review != null)
                        .Select(b => b.Review.Rating)
                        .ToList() ?? new List<int>();
                        
                    var averageRating = rating.Any() ? Math.Round(rating.Average(), 2) : 0;
                    
                    dtos.Add(new HostDto
                    {
                        Id = host.Id,
                        FirstName = host.FirstName,
                        LastName = host.LastName,
                        Email = host.Email,
                        PhoneNumber = host.PhoneNumber,
                        Role = host.Role,
                        IsVerified = host.Host?.IsVerified ?? false,
                        ProfilePictureUrl = host.ProfilePictureUrl,
                        StartDate = host.CreatedAt,
                        TotalReviews = rating.Count(),
                        Rating = (decimal)averageRating,
                        PropertiesCount = host.Host?.Properties?.Count ?? 0,
                        TotalIncome = totalIncome
                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("hosts/not-verified")]
        public async Task<IActionResult> GetNotVerifiedHosts()
        {
            try
            {
                var hosts = await _adminRepository.GetAllHostsAsync();
                var notVerifiedHosts = hosts.Where(h => h.Host?.IsVerified == false).ToList();
                //var notVerifiedHosts = await _adminRepository.GetNotVerifiedHostsAsync();
                var dtos = new List<HostDto>();
                foreach (var host in notVerifiedHosts) 
                {
                    var totalIncome = await _bookingRepository.GetTotalIncomeForHostAsync(host.Id);
                    
                    // Add null checks to prevent exceptions
                    var rating = host.Host?.Properties?
                        .Where(p => p != null)
                        .SelectMany(p => p.Bookings ?? Enumerable.Empty<Booking>())
                        .Where(b => b?.Review != null)
                        .Select(b => b.Review.Rating)
                        .ToList() ?? new List<int>();
                        
                    var averageRating = rating.Any() ? Math.Round(rating.Average(), 2) : 0;
                    
                    dtos.Add(new HostDto
                    {
                        Id = host.Id,
                        FirstName = host.FirstName,
                        LastName = host.LastName,
                        Email = host.Email,
                        PhoneNumber = host.PhoneNumber,
                        Role = host.Role,
                        IsVerified = host.Host?.IsVerified ?? false,
                        ProfilePictureUrl = host.ProfilePictureUrl,
                        StartDate = host.CreatedAt,
                        TotalReviews = rating.Count(),
                        Rating = (decimal)averageRating,
                        PropertiesCount = host.Host?.Properties?.Count ?? 0,
                        TotalIncome = totalIncome
                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPut("users/{userId}/block")]
        public async Task<IActionResult> BlockUser(int userId, [FromBody] BlockUserDto input)
        {
            try
            {
                var success = await _adminRepository.BlockUserAsync(userId, input.IsBlocked);
                if (!success)
                    return NotFound("User not found.");

                return Ok(new { Message = $"User {(input.IsBlocked ? "blocked" : "unblocked")} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        #region Property Management

        [HttpPut("properties/{propertyId}/approve")]
        public async Task<IActionResult> ApproveProperty(int propertyId, [FromBody] ApprovePropertyDto input)
        {
            try
            {
                var success = await _adminRepository.ApprovePropertyAsync(propertyId, input.IsApproved);
                if (!success)
                    return NotFound("Property not found.");

                return Ok(new { Message = $"Property {(input.IsApproved ? "approved" : "suspended")} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        
        [HttpGet("properties/pending")]
        public async Task<IActionResult> GetPendingProperties()
        {
            try
            {
                var properties = await _adminRepository.GetAllPendingPropertiesAsync();
                var images = properties.Where(p => p.PropertyImages != null).SelectMany(p => p.PropertyImages).ToList();

                var dtos = new List<PropertyDTOAdmin>();
                foreach (var property in properties)
                {
                    dtos.Add(new PropertyDTOAdmin
                    {
                        Id = property.Id,
                        HostId = property.HostId,
                        HostName = property.Host.User.FirstName + " " + property.Host.User.LastName,
                        Title = property.Title,
                        Description = property.Description,
                        PropertyType = property.PropertyType,
                        Address = property.Address,
                        City = property.City,
                        Images = property.PropertyImages
    .OrderByDescending(img => img.IsPrimary)
    .Select(i => i.ImageUrl)
    .ToList()
               ,
                        PostalCode = property.PostalCode,
                        Latitude = property.Latitude,
                        Longitude = property.Longitude,
                        PricePerNight = property.PricePerNight,
                        CleaningFee = property.CleaningFee,
                        ServiceFee = property.ServiceFee,
                        MinNights = property.MinNights,
                        MaxNights = property.MaxNights,
                        Bedrooms = property.Bedrooms,
                        Bathrooms = property.Bathrooms,
                        MaxGuests = property.MaxGuests,
                        Status = property.Status,
                        CreatedAt = property.CreatedAt,
                        UpdatedAt = property.UpdatedAt,

                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("properties/approved")]
        public async Task<IActionResult> GetApprovedProperties()
        {
            try
            {
                var properties = await _adminRepository.GetAllApprovedPropertiesAsync();
                var dtos = new List<PropertyDTOAdmin>();


                foreach (var property in properties)
                {
                    dtos.Add(new PropertyDTOAdmin
                    {
                        Id = property.Id,
                        HostId = property.HostId,
                        HostName = property.Host.User.FirstName + " " + property.Host.User.LastName,
                        Title = property.Title,
                        Description = property.Description,
                        PropertyType = property.PropertyType,
                        Address = property.Address,
                        City = property.City,
                        Country = property.Country,
                        PostalCode = property.PostalCode,
                        Latitude = property.Latitude,
                        Longitude = property.Longitude,
                        PricePerNight = property.PricePerNight,
                        Images = property.PropertyImages
                       .OrderByDescending(img => img.IsPrimary)
                       .Select(i => i.ImageUrl)
                       .ToList(),
                        CleaningFee = property.CleaningFee,
                        ServiceFee = property.ServiceFee,
                        MinNights = property.MinNights,
                        MaxNights = property.MaxNights,
                        Bedrooms = property.Bedrooms,
                        Bathrooms = property.Bathrooms,
                        MaxGuests = property.MaxGuests,
                        Status = property.Status,
                        CreatedAt = property.CreatedAt,
                        UpdatedAt = property.UpdatedAt,
                        AverageRating = property.Bookings.Select(b => b.Review).Where(r => r != null).Any()
                            ? property.Bookings.Select(b => b.Review).Where(r => r != null).Average(r => r.Rating)
                            : 0.0,
                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPut("properties/{propertyId}/suspend")]
        public async Task<IActionResult> SuspendProperty(int propertyId, [FromBody] SusspendPropertyDTO input)
        {

            var success = await _adminRepository.SuspendPropertyAsync(propertyId, input.IsSuspended);
            if (!success)
                return NotFound("Property not found.");
            return Ok(new { Message = "Property suspended successfully." });
        }
        #endregion

        #region Booking Management

        [HttpGet("bookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                var bookings = await _adminRepository.GetAllBookingsAsync();
                var dtos = new List<BookingOutputDTO>();
                foreach (var booking in bookings)
                {
                    var property = await _bookingRepository.getPropertyByIdAsync(booking.PropertyId);
                    var guest = await _bookingRepository.GetUserBookingetails(booking.Id);
                    dtos.Add(new BookingOutputDTO
                    {
                        Id = booking.Id,
                        PropertyId = booking.PropertyId,
                        GuestId = booking.GuestId,
                        StartDate = booking.StartDate,
                        EndDate = booking.EndDate,
                        Status = booking.Status,
                        CheckInStatus = booking.CheckInStatus,
                        CheckOutStatus = booking.CheckOutStatus,
                        TotalAmount = booking.TotalAmount,
                        PromotionId = booking.PromotionId,
                        CreatedAt = booking.CreatedAt,
                        UpdatedAt = booking.UpdatedAt
                    });
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPut("bookings/{bookingId}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusDto input)
        {
            try
            {
                var success = await _adminRepository.UpdateBookingStatusAsync(bookingId, input.Status);
                if (!success)
                    return NotFound("Booking not found.");

                return Ok(new { Message = $"Booking status updated to {input.Status} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        #endregion
    }
}
