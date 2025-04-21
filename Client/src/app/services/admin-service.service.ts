import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface HostDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  role: string;
  isVerified: boolean;
  profilePictureUrl: string;
  startDate: Date;
  totalReviews: number;
  Rating: number;
  propertiesCount: number;
  totalIncome: number;
}

export interface GuestDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  profilePictureUrl: string;
  createdAt: Date;
  updatedAt: Date;
  lastLogin: Date;
  accountStatus: string;
  role: string;
  dateOfBirth: Date;
  bookingsCount: number;
  totalSpent: number;
}

export interface PropertyDto {
  id: number;
  hostId: number;
  hostName: string;
  title: string;
  description: string;
  propertyType: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  pricePerNight: number;
  cleaningFee: number;
  serviceFee: number;
  minNights: number;
  maxNights: number;
  bedrooms: number;
  bathrooms: number;
  maxGuests: number;
  status: string;
  createdAt: Date;
  updatedAt: Date;
  images: PropertyImageDto[];
  amenities: string[];
  reviewsCount: number;
  rating: number;
  totalIncome: number;
  bookingsCount: number;
  isVerified: boolean;
  isSuspended: boolean;
  isApproved: boolean;
  averageRating: number;
  totalReviews: number;
  totalBookings: number;
  totalIncomeGenerated: number;

}

export interface PropertyImageDto {
  id: number;
  imageUrl: string;  // Changed from 'url' to match backend
  isPrimary: boolean;
  category?: string;  // Added to match backend
}

export interface BookingDto {
  id: number;
  propertyId: number;
  guestId: number;
  startDate: Date;
  endDate: Date;
  status: string;
  checkInStatus: string;
  checkOutStatus: string;
  totalAmount: number;
  promotionId?: number;
  createdAt: Date;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AdminServiceService {
  private readonly API_URL = 'https://localhost:7228/api/admin';
  private readonly Base_url = 'https://localhost:7228/api/proprties'; // Replace with your actual API URL

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // Host Management
  getAllHosts(): Observable<HostDto[]> {
    return this.http.get<HostDto[]>(`${this.API_URL}/hosts`);
  }

  getVerifiedHosts(): Observable<HostDto[]> {
    return this.http.get<HostDto[]>(`${this.API_URL}/hosts/verified`);
  }

  getNotVerifiedHosts(): Observable<HostDto[]> {
    return this.http.get<HostDto[]>(`${this.API_URL}/hosts/not-verified`);
  }

  gethostverfication(hostid: number): Observable<any> {
    return this.http.get<any>(`${this.API_URL}/GetVerificationsByHostId/${hostid}`);
  }

  verifyHost(hostid: number): Observable<any> {
    return this.http.put<any>(`${this.API_URL}/hosts/${hostid}/verify`, {});
  }

  // Guest Management
  getAllGuests(): Observable<GuestDto[]> {
    return this.http.get<GuestDto[]>(`${this.API_URL}/guests`);
  }

  // User Management
  blockUser(userId: number, isBlocked: boolean): Observable<any> {
    return this.http.put<any>(`${this.API_URL}/users/${userId}/block`, { isBlocked });
  }

  // Property Management
  approveProperty(propertyId: number, isApproved: boolean): Observable<any> {
    return this.http.put(`${this.API_URL}/properties/${propertyId}/approve`, { isApproved });
  }

  suspendProperty(propertyId: number, isSuspended: boolean): Observable<any> {
    return this.http.put(`${this.API_URL}/properties/${propertyId}/suspend`, { isSuspended });
  }

  getPendingProperties(): Observable<PropertyDto[]> {
    return this.http.get<PropertyDto[]>(`${this.API_URL}/properties/pending`);
  }

  getApprovedProperties(): Observable<PropertyDto[]> {
    return this.http.get<PropertyDto[]>(`${this.API_URL}/properties/approved`);
  }

  getAllProperties(): Observable<PropertyDto[]> {
    return this.http.get<PropertyDto[]>(`${this.API_URL}/properties`);
  }

  // Booking Management
  getAllBookings(): Observable<BookingDto[]> {
    return this.http.get<BookingDto[]>(`${this.API_URL}/bookings`);
  }

  updateBookingStatus(bookingId: number, status: string): Observable<any> {
    return this.http.put(`${this.API_URL}/bookings/${bookingId}/status`, { status });
  }

  currentUserValue(): Promise<number> {
    const user = this.authService.currentUserValue;
    if (!user?.accessToken) {
      return Promise.reject(new Error('No user is currently logged in'));
    }
    const decoded = this.authService.decodeToken(user.accessToken);
    return Promise.resolve(parseInt(decoded.nameid));
  }
}
