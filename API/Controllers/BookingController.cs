using System.Security.Claims;
using AirBnb.BL.Dtos.BookingDtos;
using API.DTOs.Promotion;
using API.Models;
using API.Services.BookingRepo;
using API.Services.NotificationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly INotificationRepository _notificationRepo;

        public BookingController(IBookingRepository bookingRepo, INotificationRepository notificationRepository)
        {
            _bookingRepo = bookingRepo;
            _notificationRepo = notificationRepository;
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


        [HttpGet("allbookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                var hostId = GetCurrentUserId();
                var bookings = await _bookingRepo.GetAllBookingsAsync(hostId);
                var dtos = new List<BookingDetailsDTO>();
                foreach (var booking in bookings)
                {
                    var property = await _bookingRepo.getPropertyByIdAsync(booking.PropertyId);
                    var guest = await _bookingRepo.GetUserBookingetails(booking.Id);
                    dtos.Add(new BookingDetailsDTO
                    {
                        Id = booking.Id,
                        PropertyId = booking.PropertyId,
                        PropertyTitle = property.Title,
                        GuestName = guest.Guest.FirstName + " " + guest.Guest.LastName,
                        Payments = booking.Payments.Select(p => new PaymentDTO
                        {
                            Id = p.Id,
                            Amount = p.Amount,
                            PaymentMethodType = p.PaymentMethodType,
                            Status = p.Status,
                            CreatedAt = p.CreatedAt
                        }).ToList(),
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
        [Authorize(Roles = "Host")]
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
        [Authorize(Roles = "Host")]
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
        [Authorize(Roles = "Host")]
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
                var notification = new Notification
                {
                    UserId = booking.GuestId,
                    SenderId = hostId,
                    Message = $"Your booking for property {booking.Property.Title} has been confirmed.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepo.CreateNotificationAsync(notification);

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
        [Authorize(Roles = "Guest")]
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

        [HttpPost("{bookingId}/apply-promotion")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> ApplyPromotion(int bookingId, [FromBody] ApplyPromotionDto input)
        {
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null)
                    return NotFound("Booking not found.");

                var guestId = GetCurrentUserId();
                if (booking.GuestId != guestId)
                    return Forbid("You are not authorized to apply a promotion to this booking.");

                var promotionid = await _bookingRepo.GetPromotionIdByCodeAsync(input.PromoCode);
                if (booking.PromotionId == promotionid)
                    return BadRequest("A promotion has already been applied to this booking.");

                var isPromotionValid = await _bookingRepo.IsPromotionValidForBookingAsync(promotionid, guestId, booking.StartDate);
                if (!isPromotionValid)
                    return BadRequest("The promotion is invalid or cannot be applied.");

                var promotion = await _bookingRepo.GetPromotionByIdAsync(promotionid);
                if (promotion == null)
                    return BadRequest("Invalid promotion ID.");

                var originalPrice = booking.TotalAmount;
                var discountedPrice = originalPrice;

                if (promotion.DiscountType == "fixed")
                    discountedPrice -= promotion.Amount;
                else if (promotion.DiscountType == "percentage")
                    discountedPrice -= (originalPrice * promotion.Amount / 100);

                discountedPrice = Math.Max(discountedPrice, 0);

                booking.TotalAmount = discountedPrice;
                booking.PromotionId = promotion.Id;
                booking.UpdatedAt = DateTime.UtcNow;

                await _bookingRepo.UpdateAsync(booking);

                var usedPromotion = new UserUsedPromotion
                {
                    PromotionId = promotion.Id,
                    BookingId = booking.Id,
                    UserId = guestId,
                    DiscountedAmount = originalPrice - discountedPrice,
                    UsedAt = DateTime.UtcNow
                };

                await _bookingRepo.AddUserUsedPromotionAsync(usedPromotion);

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
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
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

            if (lastavailableDate == null || input.StartDate > lastavailableDate || input.EndDate > lastavailableDate.Value.AddDays(1))
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

                var stayDuration = (input.EndDate - input.StartDate).TotalDays;
                
                var basePrice = (property.Result.PricePerNight + property.Result.CleaningFee + property.Result.ServiceFee) * (decimal)stayDuration;

                Promotion promotion=null ;
                if (input.PromotionId>0)
                {

                 promotion = await _bookingRepo.GetPromotionByIdAsync(input.PromotionId);
                }
                decimal discountedPrice = (decimal)basePrice;

                if (input.PromotionId > 0)
                {
                    var isPromotionValid = await _bookingRepo.IsPromotionValidForBookingAsync(input.PromotionId, guestId, input.StartDate);
                    if (!isPromotionValid)
                        return BadRequest("The promotion is invalid or cannot be applied.");

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

                var notification1 = new Notification
                {
                    UserId = (int)property.Result.HostId,
                    SenderId = guestId,
                    Message = $"A new booking has been made for your property {property.Result.Title}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                var notification2 = new Notification
                {
                    UserId = guestId,
                    SenderId = (int)property.Result.HostId,
                    Message = $"Your booking for property {property.Result.Title} has been confirmed.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };


                if (property.Result.InstantBook == true)
                {
                    booking.Status = BookingStatus.Confirmed.ToString();
                    await _notificationRepo.CreateNotificationAsync(notification2);
                }
                else
                {
                    notification2.Message = $"Your booking for property {property.Result.Title} is pending confirmation.";
                    await _notificationRepo.CreateNotificationAsync(notification2);
                }

                await _bookingRepo.CreateBookingAndUpdateAvailabilityAsync(booking);
                await _notificationRepo.CreateNotificationAsync(notification1);

                var usedPromotion = new UserUsedPromotion
                {
                    PromotionId = promotion != null ? promotion.Id : 0,
                    UserId = guestId,
                    BookingId = booking.Id,
                    DiscountedAmount = (decimal)(basePrice - discountedPrice),
                    UsedAt = DateTime.UtcNow
                };
                if (promotion != null)
                {
                    await _bookingRepo.AddUserUsedPromotionAsync(usedPromotion);
                    var notification3 = new Notification
                    {
                        UserId = guestId,
                        Message = $"You got {discountedPrice} discount on your booking on property {property.Result.Title}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepo.CreateNotificationAsync(notification3);
                }



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
        [Authorize(Roles = "Guest")]
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
                var booking = await _bookingRepo.getBookingByIdWithData(id);
                if (booking == null)
                    return NotFound("Booking not found.");
                
                var userId = GetCurrentUserId();
                var hostId = booking.Property.HostId;
                

                if (booking.GuestId != userId)
                    return Forbid("You are not authorized to delete this booking.");

                await _bookingRepo.DeleteBookingAndUpdateAvailabilityAsync(id);
                var notification1 = new Notification
                {
                    UserId = userId,
                    Message = $"Your booking for property {booking.Property.Title} has been canceled.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                var notification2 = new Notification
                {
                    UserId = hostId,
                    Message = $"Booking for your property {booking.Property.Title} has been canceled.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepo.CreateNotificationAsync(notification1);
                await _notificationRepo.CreateNotificationAsync(notification2);


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

        [HttpPost("create-payment-intent")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] int bookingId)
        {
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null)
                    return NotFound("Booking not found.");
                var paymentIntent = await _bookingRepo.CreatePaymentIntentAsync(booking.TotalAmount);
                return Ok(new { clientSecret = paymentIntent.ClientSecret, Id = paymentIntent.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }


        //[HttpPost("create-payment-intent")]
        //[Authorize(Roles = "Guest")]
        //public async Task<IActionResult> CreatePaymentIntent([FromBody] int bookingId)
        //{
        //    try
        //    {
        //        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        //        if (booking == null)
        //            return NotFound("Booking not found.");

        //        // Calculate the final amount after applying the promotion
        //        var finalAmount = booking.TotalAmount;
        //        if (booking.PromotionId != 0)
        //        {
        //            var promotion = await _bookingRepo.GetPromotionByIdAsync(booking.PromotionId);
        //            if (promotion != null)
        //            {
        //                if (promotion.DiscountType == "fixed")
        //                    finalAmount -= promotion.Amount;
        //                else if (promotion.DiscountType == "percentage")
        //                    finalAmount -= (booking.TotalAmount * promotion.Amount / 100);

        //                finalAmount = Math.Max(finalAmount, 0); // Ensure the amount is not negative
        //            }
        //        }

        //        // Create the payment intent with the discounted amount
        //        var paymentIntentService = new PaymentIntentService();
        //        var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
        //        {
        //            Amount = (long)(finalAmount * 100), // Convert to cents
        //            Currency = "usd",
        //            PaymentMethodTypes = new List<string> { "card" },
        //            Metadata = new Dictionary<string, string>
        //    {
        //        { "bookingId", bookingId.ToString() },
        //        { "promotionId", booking.PromotionId.ToString() ?? "none" }
        //    }
        //        });

        //        return Ok(new { clientSecret = paymentIntent.ClientSecret, Id = paymentIntent.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        //    }
        //}
    }
}
