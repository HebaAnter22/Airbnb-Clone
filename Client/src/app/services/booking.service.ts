// booking.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Booking {
  id: number;
  propertyId: number;
  guestId: number;
  startDate: Date;
  endDate: Date;
  checkInStatus: string;
  checkOutStatus: string;
  status: string;
  totalAmount: number;
  promotionId: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface Property {
  id: number;
  title: string;
  location: string;
  country: string;
  city: string;
  imageUrl: string;
}

export interface BookingResponse {
  bookings: Booking[];
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getUserBookings(page: number = 1, pageSize: number = 10): Observable<BookingResponse> {
    return this.http.get<BookingResponse>(`${this.apiUrl}/Booking/userBookings?page=${page}&pageSize=${pageSize}`);
  }

  getPropertyById(propertyId: number): Observable<Property> {
    return this.http.get<Property>(`${this.apiUrl}/Properties/${propertyId}`);
  }

  cancelBooking(bookingId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/Booking/${bookingId}`);
  }
  loadUserReviews(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/profile/guest/reviews`);
  }
}