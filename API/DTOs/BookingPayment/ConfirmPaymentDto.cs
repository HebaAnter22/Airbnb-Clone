namespace API.DTOs.BookingPayment
{
    public class ConfirmPaymentDto
    {
        public int BookingId { get; set; }
        public string PaymentIntentId { get; set; }
        public string PaymentMethodId { get; set; }
    }
}
