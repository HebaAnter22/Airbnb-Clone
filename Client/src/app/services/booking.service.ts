// booking.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, mergeMap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CancellationPolicy, RefundCalculation } from '../models/cancellation-policy.model';
import { catchError, map, tap } from 'rxjs/operators';

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
  property?: Property;
}

export interface Property {
  id: number;
  title: string;
  location: string;
  country: string;
  city: string;
  images?: { id: number, propertyId: number, imageUrl: string }[];
  imageUrl?: string; // Keep for backwards compatibility
  cancellationPolicy?: {
    id: number;
    name: string;
    description: string;
    refundPercentage: number;
  };
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

  getBookingWithDetails(bookingId: number): Observable<Booking> {
    return this.http.get<Booking>(`${this.apiUrl}/Booking/${bookingId}/details`);
  }

  /**
   * Calculate refund information based on booking details
   * This is a preview calculation - actual refund will be calculated server-side
   */
  calculateRefundPreview(booking: Booking): Observable<RefundCalculation> {
    console.log('[DEBUG-SERVICE] Starting refund calculation for booking:', booking.id);
    console.log('[DEBUG-SERVICE] Full booking data:', booking);
    console.log('[DEBUG-SERVICE] Cancellation policy from booking:', booking.property?.cancellationPolicy);
    
    // Calculate days until check-in for use in fallback refund
    const daysUntilCheckIn = this.calculateDaysUntilCheckIn(booking.startDate);
    
    // Create fallback refund info in case of errors
    const fallbackRefund: RefundCalculation = {
      refundPercentage: 0,
      refundAmount: 0,
      isEligibleForRefund: false,
      policyName: 'Flexible',  // Default to Flexible policy
      policyDescription: 'Cancel up to 24 hours before check-in for a full refund.',
      daysUntilCheckIn: daysUntilCheckIn
    };
    
    // Check if we need to apply a default policy for missing data
    if (booking.property && !booking.property.cancellationPolicy) {
      console.log('[DEBUG-SERVICE] Property exists but no cancellation policy, adding default');
      
      // Create a default "Flexible" policy
      booking.property.cancellationPolicy = {
        id: 1,
        name: 'Flexible',
        description: 'Cancel up to 24 hours before check-in for a full refund.',
        refundPercentage: 100
      };
      
      // Since the backend doesn't include cancellation policy in DTOs,
      // we'll calculate refund based on the default policy
      const updatedResult = this.computeRefundWithDefaultPolicy(booking, daysUntilCheckIn);
      console.log('[DEBUG-SERVICE] Used default policy, result:', updatedResult);
      return of(updatedResult);
    }
    
    // If we have property but no policy data
    if (!booking.property) {
      console.log('[DEBUG-SERVICE] No property data, using fallback');
      return of(fallbackRefund);
    }
    
    console.log(`[DEBUG-SERVICE] Using existing cancellation policy for booking ${booking.id}:`, booking.property.cancellationPolicy);
    const result = this.computeRefund(booking);
    console.log('[DEBUG-SERVICE] Final refund calculation result:', result);
    return of(result);
  }

  /**
   * Calculate refund using a default policy when API doesn't provide one
   */
  private computeRefundWithDefaultPolicy(booking: Booking, daysUntilCheckIn: number): RefundCalculation {
    console.log('[DEBUG-SERVICE] Computing refund with default policy');
    
    // Apply flexible cancellation policy by default
    // (cancel up to 24 hours before check-in for full refund)
    const isEligibleForRefund = daysUntilCheckIn >= 1;
    const refundPercentage = isEligibleForRefund ? 100 : 0;
    const refundAmount = booking.totalAmount * (refundPercentage / 100);
    
    return {
      refundPercentage,
      refundAmount,
      isEligibleForRefund,
      policyName: 'Flexible',
      policyDescription: 'Cancel up to 24 hours before check-in for a full refund.',
      daysUntilCheckIn
    };
  }

  /**
   * Fetch a property with its cancellation policy
   */
  private getPropertyWithPolicy(propertyId: number): Observable<Property> {
    console.log(`[DEBUG-SERVICE] Fetching property ${propertyId} for cancellation policy`);
    return this.http.get<Property>(`${this.apiUrl}/Properties/${propertyId}`).pipe(
      tap(property => {
        console.log(`[DEBUG-SERVICE] Property ${propertyId} details received:`, property);
        // Add default cancellation policy if none exists
        if (!property.cancellationPolicy) {
          console.log(`[DEBUG-SERVICE] Adding default cancellation policy to property ${propertyId}`);
          property.cancellationPolicy = {
            id: 1,
            name: 'Flexible',
            description: 'Cancel up to 24 hours before check-in for a full refund.',
            refundPercentage: 100
          };
        }
      })
    );
  }

  /**
   * Calculate days until check-in
   */
  private calculateDaysUntilCheckIn(startDate: Date): number {
    const start = new Date(startDate);
    const now = new Date();
    return Math.ceil((start.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  }

  private computeRefund(booking: Booking): RefundCalculation {
    console.log('[DEBUG-SERVICE] Computing refund with booking:', booking.id);
    console.log('[DEBUG-SERVICE] Policy used for computation:', booking.property?.cancellationPolicy);
    
    // Create default policy if none exists
    if (booking.property && !booking.property.cancellationPolicy) {
      console.log('[DEBUG-SERVICE] Creating default cancellation policy for computation');
      booking.property.cancellationPolicy = {
        id: 1,
        name: 'Flexible',
        description: 'Cancel up to 24 hours before check-in for a full refund.',
        refundPercentage: 100
      };
    }
    
    const policy = booking.property?.cancellationPolicy;
    const daysUntilCheckIn = this.calculateDaysUntilCheckIn(booking.startDate);
    
    let isEligibleForRefund = false;
    let refundPercentage = 0;

    if (policy) {
      console.log(`[DEBUG-SERVICE] Using policy name: "${policy.name}" with refund percentage: ${policy.refundPercentage}`);
      switch (policy.name.toLowerCase()) {
        case 'flexible':
          isEligibleForRefund = daysUntilCheckIn >= 1;
          refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
          break;
        case 'moderate':
          isEligibleForRefund = daysUntilCheckIn >= 5;
          refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
          break;
        case 'strict':
          isEligibleForRefund = daysUntilCheckIn >= 7;
          refundPercentage = isEligibleForRefund ? policy.refundPercentage : 0;
          break;
        case 'non_refundable':
          isEligibleForRefund = false;
          refundPercentage = 0;
          break;
        default:
          console.log(`[DEBUG-SERVICE] Unknown policy name: "${policy.name}", defaulting to Flexible policy`);
          isEligibleForRefund = daysUntilCheckIn >= 1;
          refundPercentage = isEligibleForRefund ? 100 : 0;
      }
    } else {
      console.log('[DEBUG-SERVICE] No policy found, defaulting to Flexible policy');
      isEligibleForRefund = daysUntilCheckIn >= 1;
      refundPercentage = isEligibleForRefund ? 100 : 0;
    }

    const refundAmount = booking.totalAmount * (refundPercentage / 100);
    
    const result = {
      refundPercentage,
      refundAmount,
      isEligibleForRefund,
      policyName: policy?.name || 'Flexible',
      policyDescription: policy?.description || 'Cancel up to 24 hours before check-in for a full refund.',
      daysUntilCheckIn
    };
    
    console.log('[DEBUG-SERVICE] Final refund calculation:', result);
    return result;
  }
}