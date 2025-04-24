export interface PaymentIntentResponse {
    clientSecret: string;
    paymentIntentId: string;
  }
  
  export interface BookingDetails {
    id: number;
    propertyTitle: string;
    checkInDate: string;
    checkOutDate: string;
    totalPrice: number;
    propertyImage: string;
  }