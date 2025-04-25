using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Models;

namespace AirBnb.BL.Dtos.BookingDtos
{
    public class BookingInputDTO
    {
        public int PropertyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PromotionId { get; set; } = 0; 
    }

    public class BookingOutputDTO
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int GuestId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CheckInStatus { get; set; }
        public string CheckOutStatus { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int PromotionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BookingDetailsDTO
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int GuestId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CheckInStatus { get; set; }
        public string CheckOutStatus { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int PromotionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Related Data
        public string GuestName { get; set; }
        public string PropertyTitle { get; set; }
        public List<PaymentDTO> Payments { get; set; } = new List<PaymentDTO>();
    }

    public class PaymentDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethodType { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentIntentDto
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }

    public class BookingGetAllDto
	{
		public int Id { get; set; }
		public DateTime CheckInDate { get; set; }
		public DateTime CheckOutDate { get; set; }
		public int TotalPrice { get; set; }
		public string BookingStatus { get; set; }
		public int propertyId { get; set; }
		public string propImage {  get; set; }
		public string propTitle { get; set; }

	}
	public class BookingUpdateDto
	{
		public string BookingStatus { get; set; }

	}
	public class BookingGetDetailsUserDtos
	{
		public int Id { get; set; }
		public DateTime CheckInDate { get; set; }
		public DateTime CheckOutDate { get; set; }
		public int TotalPrice { get; set; }
		public int propertyId { get; set; }
		public string PropertyName { get; set; }

	}
	public class BookingGetDetailsHostDto
	{
		public int Id { get; set; }
		public DateTime CheckInDate { get; set; }
		public DateTime CheckOutDate { get; set; }

		public int TotalPrice { get; set; }
		public string PropertyName { get; set; }
		public string UserName { get; set; }
		public int UserAge { get; set; }
		public string UserImage {  get; set; }
		public string UserPhone { get; set; }
		public int Status { get; set; }
	}
	public class BookingAddDto
	{
		public int PropertyId { get; set; }

		public string UserId { get; set; } = string.Empty;
		public DateTime CheckInDate { get; set; }
		public DateTime CheckOutDate { get; set; }
		public int TotalPrice { get; set; }
		public string BookingStatus { get; set; } 
	}
	public class AvailabilityUpdateDto
	{
		public DateTime From { get; set; }
		public DateTime To { get; set; }
		public bool IsAvailable { get; set; }
	}
	public class BookingDataForPayment
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public int TotalPrice { get; set; }
		public string ClientSecret { get; set; }
		public string PaymentIntentId { get; set; }

	}


}
