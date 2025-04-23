using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;

        public BookingController(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        [HttpGet("GetAllBookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                var hostId = GetCurrentUserId();
                var bookings = await _bookingRepo.GetAllBookingsAsync(hostId);
                var dtos = new List<BookingOutputDTO>();
                foreach (var booking in bookings)
                {
                    var property = await _bookingRepo.getPropertyByIdAsync(booking.PropertyId);
                    var guest = await _bookingRepo.GetUserBookingetails(booking.Id);
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
    }
} 