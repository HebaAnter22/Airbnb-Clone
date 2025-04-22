using System.Security.Claims;
using AirBnb.BL.Dtos.BookingDtos;
using API.Models;
using API.Services.BookingRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;

        public BookingController(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
            }
            return userId;
        }

        #region Host Methods

        // Get all bookings for a specific property
        [HttpGet("bookingsbypropertyid/{propertyId}")]
        [Authorize]
        public async Task<IActionResult> GetAllBookingForProperty(int propertyId, int page = 1, int pageSize = 10)
        {
            var result = await _bookingRepo.GetAllBookingForProperty(propertyId, page, pageSize);

            var dtos = result.bookings.Select(b => new BookingOutputDTO
            {
                Id = b.Id,
                PropertyId = b.PropertyId,
                GuestId = b.GuestId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                CheckInStatus = b.CheckInStatus,
                CheckOutStatus = b.CheckOutStatus,
                Status = b.Status,
                TotalAmount = b.TotalAmount,
                PromotionId = b.PromotionId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });

            return Ok(new { bookings = dtos, totalCount = result.totalCount });
        }

      
        // Get detailed bookings for a property.
        [HttpGet("property/details/{propertyId}")]
        [Authorize(Roles = "host")]
        public async Task<IActionResult> GetPropertyBookingDetails(int propertyId)
        {
            var bookings = await _bookingRepo.GetPropertyBookingDetails(propertyId);
            var hostId = GetCurrentUserId();
            foreach (var booking in bookings)
            {
                var IsHostAuthorized = await _bookingRepo.IsBookingOwnedByHostAsync(booking.Id, hostId);
                if (!IsHostAuthorized)
                    return Forbid("You are not authorized to view this booking.");
            }
            

            var dtos = bookings.Select(b => new BookingDetailsDTO
            {
                Id = b.Id,
                PropertyId = b.PropertyId,
                GuestId = b.GuestId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                CheckInStatus = b.CheckInStatus,
                CheckOutStatus = b.CheckOutStatus,
                Status = b.Status,
                TotalAmount = b.TotalAmount,
                PromotionId = b.PromotionId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                GuestName = $"{b.Guest.FirstName} {b.Guest.LastName}",
                PropertyTitle = b.Property.Title,
                Payments = b.Payments.Select(p => new PaymentDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentMethodType = p.PaymentMethodType,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                }).ToList()
            });

            return Ok(dtos);
        }


        //Get a booking by user ID and property ID.
        [HttpGet("user-properties/{guestId}/{propertyId}")]
        [Authorize(Roles = "host")]
        public async Task<IActionResult> GetBookingByGuestAndProperty(int guestId, int propertyId)
        {
            try
            {
                if (propertyId <= 0)
                    return BadRequest("Property ID must be greater than zero.");

                var bookings = await _bookingRepo.GetBookingsByGuestAndPropertyAsync(guestId.ToString(), propertyId);
                foreach (var booking in bookings)
                {
                    var hostId = GetCurrentUserId();
                    var IsAuthorized = await _bookingRepo.IsBookingOwnedByHostAsync(booking.Id, hostId);
                    if (!IsAuthorized)
                        return Forbid("You are not authorized to view this booking.");
                }
                

                if (bookings == null)
                    return NotFound("Bookings not found.");

                var dtos = bookings.Select(booking => new BookingOutputDTO
                {
                    Id = booking.Id,
                    PropertyId = booking.PropertyId,
                    GuestId = booking.GuestId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    CheckInStatus = booking.CheckInStatus,
                    CheckOutStatus = booking.CheckOutStatus,
                    Status = booking.Status,
                    TotalAmount = booking.TotalAmount,
                    PromotionId = booking.PromotionId,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                });

                return Ok(dtos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred while fetching the booking.");
            }
        }

        [HttpPut("confirm/{bookingId}")]
        [Authorize(Roles = "host")]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            try
            {
                var booking = await _bookingRepo.getBookingByIdWithData(bookingId);
                if (booking == null)
                    return NotFound("Booking not found.");

                if (booking.Status != BookingStatus.Pending.ToString())
                    return BadRequest("Booking is already confirmed.");

                var hostId = GetCurrentUserId();

                var isHostAuthorized = await _bookingRepo.IsBookingOwnedByHostAsync(bookingId, hostId);
                if (!isHostAuthorized)
                    return Forbid("You are not authorized to confirm this booking.");

                var success = await _bookingRepo.UpdateBookingStatusAsync(bookingId, BookingStatus.Confirmed.ToString());
                if (!success)
                    return BadRequest("Failed to confirm the booking.");

                return Ok(new { Message = "Booking confirmed successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        #endregion



        #region Guest Methods
        // Get all bookings made by a specific user (paginated).
        [HttpGet("userBookings")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetAllUserBooking(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _bookingRepo.GetAllUserBooking(userId.ToString(), page, pageSize);

            var dtos = result.bookings.Select(b => new BookingOutputDTO
            {
                Id = b.Id,
                PropertyId = b.PropertyId,
                GuestId = b.GuestId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                CheckInStatus = b.CheckInStatus,
                CheckOutStatus = b.CheckOutStatus,
                Status = b.Status,
                TotalAmount = b.TotalAmount,
                PromotionId = b.PromotionId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });

            return Ok(new { bookings = dtos, totalCount = result.totalCount });
        }


        [HttpGet("user-property/{propertyId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingsByProperty(int propertyId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var bookings = await _bookingRepo.GetBookingsByGuestAndPropertyAsync(currentUserId.ToString(), propertyId);

                var dtos = bookings.Select(b => new BookingOutputDTO
                {
                    Id = b.Id,
                    PropertyId = b.PropertyId,
                    GuestId = b.GuestId,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    CheckInStatus = b.CheckInStatus,
                    CheckOutStatus = b.CheckOutStatus,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    PromotionId = b.PromotionId,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                });

                return Ok(dtos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred while fetching bookings.");
            }
        }

        // Get detailed information about a specific booking
        [HttpGet("{bookingId}/details")]
        [Authorize(Roles = "guest")]
        public async Task<IActionResult> GetUserBookingDetails(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _bookingRepo.GetUserBookingetails(bookingId);

            if (booking == null)
                return NotFound("Booking not found.");
            if (booking.GuestId != userId)
                return Forbid("You are not authorized to view this booking.");

            var dto = new BookingDetailsDTO
            {
                Id = booking.Id,
                PropertyId = booking.PropertyId,
                GuestId = booking.GuestId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                CheckInStatus = booking.CheckInStatus,
                CheckOutStatus = booking.CheckOutStatus,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                PromotionId = booking.PromotionId,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                GuestName = $"{booking.Guest.FirstName} {booking.Guest.LastName}",
                PropertyTitle = booking.Property.Title,
                Payments = booking.Payments.Select(p => new PaymentDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentMethodType = p.PaymentMethodType,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                }).ToList()
            };

            return Ok(dto);
        }


        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingInputDTO input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (input.StartDate >= input.EndDate)
                return BadRequest("Start date must be before end date.");
            if (input.PropertyId <= 0)
                return BadRequest("Property ID must be greater than zero.");
            if (input.PromotionId < 0)
                return BadRequest("Promotion ID must be greater than or equal to zero.");

            if (input.StartDate < DateTime.UtcNow || input.EndDate < DateTime.UtcNow)
                return BadRequest("Booking dates must be in the future.");

            var lastavailableDate = await _bookingRepo.GetLastAvailableDateForPropertyAsync(input.PropertyId);
            
            if (lastavailableDate==null ||  input.StartDate > lastavailableDate || input.EndDate > lastavailableDate.Value.AddDays(1))
                return BadRequest("Booking dates exceed the property's availability.");


            try
            {
                var guestId = GetCurrentUserId();

                var property = _bookingRepo.getPropertyByIdAsync(input.PropertyId);
                if (property.Result == null)
                    return NotFound("Property not found.");
                if (property.Result.Status != "Active")
                    return BadRequest("Property is not available for booking.");

                var minnights = property.Result.MinNights;
                var maxnights = property.Result.MaxNights;
                if (input.EndDate.Subtract(input.StartDate).TotalDays + 1 < minnights)
                    return BadRequest($"Minimum booking duration is {minnights} nights.");
                var isAvailable = await _bookingRepo.IsPropertyAvailableForBookingAsync(input.PropertyId, input.StartDate, input.EndDate);
                if (!isAvailable)
                    return BadRequest("The property is not available for the selected dates.");

                var stayDuration = (input.EndDate - input.StartDate).TotalDays + 1;
                var basePrice = (property.Result.PricePerNight + property.Result.CleaningFee + property.Result.ServiceFee)  * (decimal)stayDuration;

                decimal discountedPrice = (decimal)basePrice;
                if (input.PromotionId > 0)
                {
                    var promotion = await _bookingRepo.GetPromotionByIdAsync(input.PromotionId);
                    if (promotion == null)
                        return BadRequest("Invalid promotion ID.");


                    if (promotion.DiscountType == "fixed")
                    {
                        discountedPrice -= promotion.Amount;
                    }
                    else if (promotion.DiscountType == "percentage")
                    {
                        discountedPrice -= (decimal)(basePrice * promotion.Amount / 100);
                    }

                    discountedPrice = Math.Max(discountedPrice, 0);
                }

                var booking = new Booking
                {
                    PropertyId = input.PropertyId,
                    GuestId = guestId,
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    TotalAmount = discountedPrice,
                    PromotionId = input.PromotionId,
                    Status = BookingStatus.Pending.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if(property.Result.InstantBook== true)
                {
                    booking.Status = BookingStatus.Confirmed.ToString();
                }

                await _bookingRepo.CreateBookingAndUpdateAvailabilityAsync(booking);

                var dto = new BookingOutputDTO
                {
                    Id = booking.Id,
                    PropertyId = booking.PropertyId,
                    GuestId = booking.GuestId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    CheckInStatus = booking.CheckInStatus,
                    CheckOutStatus = booking.CheckOutStatus,
                    Status = booking.Status,
                    TotalAmount = booking.TotalAmount,
                    PromotionId = booking.PromotionId,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                };

                return CreatedAtAction(nameof(GetUserBookingDetails), new { bookingId = booking.Id }, dto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred while creating the booking.");
            }

        }


        // Update an existing booking.
        [HttpPut("{id}")]
        [Authorize (Roles ="guest")] 
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] BookingInputDTO input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var booking = await _bookingRepo.GetByIdAsync(id);
                if (booking == null)
                    return NotFound("Booking not found.");

                var userId = GetCurrentUserId();

                if (booking.GuestId != userId)
                    return StatusCode(403, "You are not authorized to update this booking.");

                var oldStartDate = booking.StartDate;
                var oldEndDate = booking.EndDate;

                var lastAvailableDate = await _bookingRepo.GetLastAvailableDateForPropertyAsync(input.PropertyId);
                if (lastAvailableDate == null || input.StartDate > lastAvailableDate || input.EndDate > lastAvailableDate.Value.AddDays(1))
                {
                    return BadRequest("The requested booking dates exceed the property's availability.");
                }

                var property = _bookingRepo.getPropertyByIdAsync(input.PropertyId);
                if (property == null)
                    return NotFound("Property not found.");
                if (property.Result.Status != "Active")
                    return BadRequest("Property is not available for booking.");

                var minnights = property.Result.MinNights;
                var maxnights = property.Result.MaxNights;
                if (input.EndDate.Subtract(input.StartDate).TotalDays + 1 < minnights)
                {
                    return BadRequest($"Minimum booking duration is {minnights} nights.");
                }

                booking.PropertyId = input.PropertyId;
                booking.StartDate = input.StartDate;
                booking.EndDate = input.EndDate;
                booking.PromotionId = input.PromotionId;
                booking.UpdatedAt = DateTime.UtcNow;

                var isAvailable = await _bookingRepo.IsPropertyAvailableForBookingAsync(input.PropertyId, input.StartDate, input.EndDate);
                if (!isAvailable)
                    return BadRequest("The property is not available for the selected dates.");

                var stayDuration = (input.EndDate - input.StartDate).TotalDays + 1;
                var basePrice = (property.Result.PricePerNight + property.Result.CleaningFee + property.Result.ServiceFee) * (decimal)stayDuration;

                decimal discountedPrice = (decimal)basePrice;
                if (input.PromotionId > 0)
                {
                    var promotion = await _bookingRepo.GetPromotionByIdAsync(input.PromotionId);
                    if (promotion == null)
                        return BadRequest("Invalid promotion ID.");

                    if (promotion.DiscountType == "fixed")
                    {
                        discountedPrice -= promotion.Amount;
                    }
                    else if (promotion.DiscountType == "percentage")
                    {
                        discountedPrice -= (decimal)(basePrice * promotion.Amount / 100);
                    }

                    discountedPrice = Math.Max(discountedPrice, 0);
                }
                booking.TotalAmount = discountedPrice;

                await _bookingRepo.UpdateBookingAndUpdateAvailabilityAsync(booking, oldStartDate, oldEndDate);

                var dto = new BookingOutputDTO
                {
                    Id = booking.Id,
                    PropertyId = booking.PropertyId,
                    GuestId = booking.GuestId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    CheckInStatus = booking.CheckInStatus,
                    CheckOutStatus = booking.CheckOutStatus,
                    Status = booking.Status,
                    TotalAmount = booking.TotalAmount,
                    PromotionId = booking.PromotionId,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                };

                return Ok(dto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred while updating the booking.");
            }
        }

        // Delete a booking.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")] 
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(id);
                if (booking == null)
                    return NotFound("Booking not found.");

                var userId = GetCurrentUserId();

                if (booking.GuestId != userId)
                    return Forbid("You are not authorized to delete this booking.");

                await _bookingRepo.DeleteBookingAndUpdateAvailabilityAsync(id);

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred while deleting the booking.");
            }
        }

        #endregion

    }
}