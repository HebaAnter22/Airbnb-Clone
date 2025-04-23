import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface BookingDetails {
  id: number;
  propertyId: number;
  guestId: number;
  startDate: string;
  endDate: string;
  checkInStatus: string;
  checkOutStatus: string;
  status: string;
  totalAmount: number;
  promotionId: number | null;
  createdAt: string;
  updatedAt: string;
  guestName: string;
  propertyTitle: string;
  payments: Payment[];
}

export interface Payment {
  id: number;
  amount: number;
  paymentMethodType: string;
  status: string;
  createdAt: string;
}

export interface Property {
  id: number;
  title: string;
  // Add other property fields as needed
}

@Injectable({
  providedIn: 'root'
})
export class HostService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('jwt');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  // Fetch host's properties
  getHostProperties(): Observable<Property[]> {
    return this.http.get<Property[]>(`${this.apiUrl}/Properties/my-properties`, { 
      headers: this.getHeaders() 
    });
  }

  getAllBookings(): Observable<BookingDetails[]> {
    return this.http.get<BookingDetails[]>(`${this.apiUrl}/Booking/allbookings`, { 
      headers: this.getHeaders() 
    });
  }


  getDetailsForBooking(propertyId: number): Observable<BookingDetails> {
    return this.http.get<BookingDetails>(
      `${this.apiUrl}/Booking/property/details/${propertyId}`,
      { headers: this.getHeaders() }
    );
  }

  // Get detailed booking information for a property
  getPropertyBookingDetails(propertyId: number): Observable<BookingDetails[]> {
    return this.http.get<BookingDetails[]>(
      `${this.apiUrl}/Booking/property/details/${propertyId}`,
      { headers: this.getHeaders() }
    );
  }

  confirmBooking(bookingId: number): Observable<any> {
    return this.http.put<any>(
      `${this.apiUrl}/Booking/confirm/${bookingId}`,
      {},
      { headers: this.getHeaders() }
    );
  }

  // Fetch all bookings for the host's properties
  // getAllHostBookings(): Observable<BookingDetails[]> {
  //   return this.getHostProperties().pipe(
  //     map(properties => properties.map(p => p.id)),
  //     switchMap(propertyIds => {
  //       if (propertyIds.length === 0) {
  //         return of([]); // Return empty array if no properties
  //       }
  //       const bookingRequests = propertyIds.map(id => this.getBookingsForProperty(id));
  //       return forkJoin(bookingRequests).pipe(
  //         map(bookingsArrays => bookingsArrays.flat()) // Flatten BookingDetails[][] to BookingDetails[]
  //       );
  //     })
  //   );
  }
