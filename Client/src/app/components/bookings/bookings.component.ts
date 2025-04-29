// booking.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { FormsModule } from '@angular/forms';
import { BookingService } from '../../services/booking.service';
import { RefundCalculation } from '../../models/cancellation-policy.model';

interface Booking {
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
  review?: {
    rating: number;
    comment: string;
  };
}

interface Property {
  id: number;
  title: string;
  location: string;
  country: string;
  city: string;
  images: image[];
  cancellationPolicy?: {
    id: number;
    name: string;
    description: string;
    refundPercentage: number;
  };
}

interface image{
  id: number;
  propertyId: number;
  imageUrl: string;
}

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, HttpClientModule, RouterModule, FormsModule],
  templateUrl: './bookings.component.html',
  styleUrls: ['./bookings.component.css']
})
export class BookingComponent implements OnInit {
  bookings: Booking[] = [];
  loading = true;
  error: string | null = null;
  
  // Make Math accessible in template
  Math = Math;
  
  // Pagination properties
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  
  // Review modal properties
  showReviewModal = false;
  currentBooking: Booking | null = null;
  reviewRating = 0;
  reviewComment = '';

  // Cancellation modal properties
  showCancellationModal = false;
  selectedBooking: Booking | null = null;
  cancellationLoading = false;
  refundInfo: RefundCalculation | null = null;

  // Toast notification properties
  showToast = false;
  toastMessage = '';
  toastType = 'success'; // 'success', 'error', etc.
  userReviews: any[] = []; // Array to hold user reviews

  constructor(private http: HttpClient, private bookingService: BookingService) {}

  ngOnInit(): void {
    this.getBookings();
    this.loadUserReviews();
  }

  // Add these methods to your BookingComponent class
  addReview(booking: Booking): void {
    this.currentBooking = booking;
    this.showReviewModal = true;
    this.reviewRating = 0;
    this.reviewComment = '';
  }

  closeReviewModal(): void {
    this.showReviewModal = false;
    this.currentBooking = null;
  }

  setRating(rating: number): void {
    this.reviewRating = rating;
  }

  loadUserReviews(): void {
    this.bookingService.loadUserReviews().subscribe({
      next: (reviews) => {
        this.userReviews = reviews;
      },
      error: (err) => {
        this.userReviews = []; // Set to empty array on error
      }
    });
  }

  submitReview(): void {
    if (!this.currentBooking) {
      this.showErrorToast('No booking selected for review.');
      return;
    }
    
    if (!this.reviewRating || this.reviewRating < 1 || this.reviewRating > 5) {
      this.showErrorToast('Please select a rating between 1 and 5 stars.');
      return;
    }
    
    if (!this.reviewComment.trim()) {
      this.showErrorToast('Please provide a comment for your review.');
      return;
    }

    const reviewData = {
      bookingId: this.currentBooking.id,
      rating: this.reviewRating,
      comment: this.reviewComment,
    };

    this.http.post(`${environment.apiUrl}/profile/guest/review`, reviewData)
      .subscribe({
        next: (response) => {
          // Update the current booking to reflect that it has a review
          if (this.currentBooking) {
            this.currentBooking.review = {
              rating: this.reviewRating,
              comment: this.reviewComment,
            };
            
            // Update the review in our userReviews array too
            this.loadUserReviews();
          }
          
          // Close the modal
          this.closeReviewModal();
          this.showSuccessToast('Thank you for your review!');
        },
        error: (err) => {
          console.error('Error submitting review', err);
          let errorMessage = 'Failed to submit review. Please try again.';
          
          // Check for specific error messages from the server
          if (err.error && typeof err.error === 'object' && err.error.message) {
            errorMessage = err.error.message;
          } else if (err.error && typeof err.error === 'string') {
            errorMessage = err.error;
          } else if (err.status === 400) {
            errorMessage = 'You have already submitted a review for this booking.';
          } else if (err.status === 404) {
            errorMessage = 'Booking not found. It may have been deleted.';
          } else if (err.status === 500) {
            errorMessage = 'Server error. Please try again later.';
          }
          
          this.showErrorToast(errorMessage);
        }
      });
  }

  // Cancellation modal methods
  openCancellationModal(booking: Booking): void {
    console.log('[DEBUG] Opening cancellation modal for booking:', booking.id);
    console.log('[DEBUG] Initial booking data:', booking);
    console.log('[DEBUG] Cancellation policy on booking:', booking.property?.cancellationPolicy);
    
    this.selectedBooking = booking;
    this.showCancellationModal = true;
    this.cancellationLoading = true;
    this.refundInfo = null;
    
    // Create a safety fallback in case we can't load property details
    const safetyFallback = setTimeout(() => {
      if (this.cancellationLoading) {
        console.warn('[DEBUG] Cancellation policy calculation is taking too long, showing fallback');
        this.cancellationLoading = false;
        this.refundInfo = {
          refundPercentage: 0,
          refundAmount: 0,
          isEligibleForRefund: false,
          policyName: 'Unknown',
          policyDescription: 'This booking may be eligible for a refund based on the property\'s cancellation policy. If you proceed with cancellation, any applicable refund will be processed automatically.',
          daysUntilCheckIn: 0
        };
      }
    }, 8000); // 8 second timeout
    
    // First, ensure we have the property details with cancellation policy
    if (!booking.property || !booking.property.cancellationPolicy) {
      console.log('[DEBUG] Property or cancellation policy missing, fetching details first');
      // Try to get complete property details first
      this.http.get<Property>(`${environment.apiUrl}/Properties/${booking.propertyId}`)
        .subscribe({
          next: (property) => {
            console.log('[DEBUG] Got property details:', property);
            console.log('[DEBUG] Cancellation policy from API:', property.cancellationPolicy);
            
            // Update the booking with property details
            if (!booking.property) {
              booking.property = property;
            } else if (property.cancellationPolicy) {
              booking.property.cancellationPolicy = property.cancellationPolicy;
            }
            
            console.log('[DEBUG] Updated booking with property details:', booking);
            // Now calculate refund with complete property info
            this.calculateRefund(booking);
            clearTimeout(safetyFallback);
          },
          error: (err) => {
            console.error('[DEBUG] Error fetching property details:', err);
            // Proceed with refund calculation anyway
            this.calculateRefund(booking);
          }
        });
    } else {
      // We already have the cancellation policy, proceed with calculation
      console.log('[DEBUG] Using existing cancellation policy:', booking.property.cancellationPolicy);
      this.calculateRefund(booking);
      clearTimeout(safetyFallback);
    }
  }
  
  // Helper method to calculate refund
  private calculateRefund(booking: Booking): void {
    console.log('[DEBUG] Calculating refund for booking:', booking.id);
    console.log('[DEBUG] Booking data for refund calculation:', booking);
    console.log('[DEBUG] Cancellation policy for refund calculation:', booking.property?.cancellationPolicy);
    
    // If the property doesn't have a cancellation policy, create a default one
    if (booking.property && !booking.property.cancellationPolicy) {
      console.log('[DEBUG] Creating default cancellation policy for property:', booking.property.id);
      booking.property.cancellationPolicy = {
        id: 1,
        name: 'Flexible',
        description: 'Cancel up to 24 hours before check-in for a full refund.',
        refundPercentage: 100
      };
    }
    
    // Calculate potential refund
    this.bookingService.calculateRefundPreview(booking).subscribe({
      next: (refundInfo) => {
        console.log('[DEBUG] Refund calculation result:', refundInfo);
        this.refundInfo = refundInfo;
        this.cancellationLoading = false;
        
        // No need to handle "Unknown" policy name since we're always setting a default policy
        if (refundInfo.policyName === 'Unknown') {
          console.warn('[DEBUG] Still got Unknown policy after setting default');
          this.refundInfo.policyName = 'Flexible';
          this.refundInfo.policyDescription = 'Cancel up to 24 hours before check-in for a full refund.';
        }
      },
      error: (err) => {
        console.error('[DEBUG] Error calculating refund:', err);
        this.cancellationLoading = false;
        this.refundInfo = {
          refundPercentage: 0,
          refundAmount: 0,
          isEligibleForRefund: false,
          policyName: 'Flexible',
          policyDescription: 'Cancel up to 24 hours before check-in for a full refund.',
          daysUntilCheckIn: 0
        };
      }
    });
  }

  closeCancellationModal(): void {
    this.showCancellationModal = false;
    this.selectedBooking = null;
    this.refundInfo = null;
  }

  confirmCancellation(): void {
    if (!this.selectedBooking) {
      console.error('Cannot cancel booking: No booking selected');
      this.showErrorToast('An error occurred while trying to cancel the booking.');
      return;
    }
    
    this.cancellationLoading = true;
    const bookingId = this.selectedBooking.id;
    
    this.bookingService.cancelBooking(bookingId).subscribe({
      next: () => {
        this.bookings = this.bookings.filter(b => b.id !== bookingId);
        this.cancellationLoading = false;
        this.closeCancellationModal();
        
        // Show success message with refund info if applicable
        if (this.refundInfo?.isEligibleForRefund) {
          this.showSuccessToast(`Booking cancelled. You will receive a refund of $${this.refundInfo.refundAmount.toFixed(2)}.`);
        } else {
          this.showSuccessToast('Booking cancelled successfully.');
        }
      },
      error: (err) => {
        console.error('Error cancelling booking', err);
        this.cancellationLoading = false;
        this.showErrorToast('Failed to cancel booking. Please try again.');
      }
    });
  }

  // Toast notification methods
  showSuccessToast(message: string): void {
    this.toastMessage = message;
    this.toastType = 'success';
    this.showToast = true;
    setTimeout(() => this.showToast = false, 5000); // Hide after 5 seconds
  }

  showErrorToast(message: string): void {
    this.toastMessage = message;
    this.toastType = 'error';
    this.showToast = true;
    setTimeout(() => this.showToast = false, 5000); // Hide after 5 seconds
  }

  getBookings(): void {
    this.loading = true;
    this.http.get<{bookings: Booking[], totalCount: number}>(`${environment.apiUrl}/Booking/userBookings?page=${this.currentPage}&pageSize=${this.pageSize}`)
      .subscribe({
        next: (response) => {
          this.bookings = response.bookings;
          this.totalCount = response.totalCount;
          // Fetch property details for each booking
          this.bookings.forEach(booking => {
            this.getPropertyDetails(booking);
          });
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to load bookings. Please try again later.';
          this.loading = false;
          console.error('Error fetching bookings', err);
        }
      });
  }

  // Pagination methods
  nextPage(): void {
    const maxPage = Math.ceil(this.totalCount / this.pageSize);
    if (this.currentPage < maxPage) {
      this.currentPage++;
      this.getBookings();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.getBookings();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= Math.ceil(this.totalCount / this.pageSize)) {
      this.currentPage = page;
      this.getBookings();
    }
  }

  getFirstImageUrl(booking: Booking): string | null {
    return booking.property?.images?.[0]?.imageUrl || null;
  }

  getPropertyDetails(booking: Booking): void {
    this.http.get<Property>(`${environment.apiUrl}/Properties/${booking.propertyId}`)
      .subscribe({
        next: (property) => {
          booking.property = property;
          if (booking.property?.images?.length) {
            console.log('First image URL:', booking.property.images[0].imageUrl);
          }
        },
        error: (err) => {
          console.error(`Error fetching property ${booking.propertyId}`, err);
        }
      });
  }

  formatLocation(property: Property | undefined): string {
    if (!property) return '';
    return `${property.city}, ${property.country}`;
  }

  formatDateRange(startDate: Date | null | undefined, endDate: Date | null | undefined): string {
    if (!startDate || !endDate) {
      return '';
    }
    
    const start = new Date(startDate);
    const end = new Date(endDate);
    const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    
    return `${start.toLocaleDateString('en-US', options)} – ${end.toLocaleDateString('en-US', options)}`;
  }

  isPastBooking(booking: any): boolean {
    const today = new Date();
    const endDate = new Date(booking.endDate);
    return endDate < today;
  }

  hasReview(booking: any): boolean {
    return !!booking.review;
  }

  goToPropertyPage(booking: any): void {
    window.location.href = `/property/${booking.propertyId}`;
  }
}

