// booking.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { FormsModule } from '@angular/forms';
import { BookingService } from '../../services/booking.service';

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
}
interface image{
  id: number;
  propertyId: number;
  imageUrl: string;
  

}

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, HttpClientModule, RouterModule,FormsModule],
  templateUrl: './bookings.component.html',
  styleUrls: ['./bookings.component.css']
})
export class BookingComponent implements OnInit {
  bookings: Booking[] = [];
  loading = true;
  error: string | null = null;
  
  showReviewModal = false;
currentBooking: Booking | null = null;
reviewRating = 0;
reviewComment = '';
showToast = false;
toastMessage = '';
toastType = 'success'; // 'success', 'error', etc.
userReviews : any[] = []; // Array to hold user reviews
  constructor(private http: HttpClient,private bookingService :BookingService) {}

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
  if (!this.currentBooking || !this.reviewRating || !this.reviewComment.trim()) {
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
        }
        
        // Close the modal
        this.closeReviewModal();
        this.showSuccessToast('Thank you for your review!');
        // Show success message
      },
      error: (err) => {
        console.error('Error submitting review', err);
        alert('Failed to submit review. Please try again.');
      }
    });
}
showSuccessToast(message: string): void {
  this.toastMessage = message;
  this.toastType = 'success';
  this.showToast = true;
  setTimeout(() => this.showToast = false, 3000); // Hide after 3 seconds
}

showErrorToast(message: string): void {
  this.toastMessage = message;
  this.toastType = 'error';
  this.showToast = true;
  setTimeout(() => this.showToast = false, 3000); // Hide after 3 seconds
}
  getBookings(): void {
    this.loading = true;
    this.http.get<{bookings: Booking[], totalCount: number}>(`${environment.apiUrl}/Booking/userBookings`)
      .subscribe({
        next: (response) => {
          this.bookings = response.bookings;
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

  cancelBooking(bookingId: number): void {
    if (confirm('Are you sure you want to cancel this booking?')) {
      this.http.delete(`${environment.apiUrl}/Booking/${bookingId}`)
        .subscribe({
          next: () => {
            this.bookings = this.bookings.filter(b => b.id !== bookingId);
          },
          error: (err) => {
            alert('Failed to cancel booking. Please try again.');
            console.error('Error cancelling booking', err);
          }
        });
    }
  }

  // Helper method to format location string like "Europe, Spain"
  formatLocation(property: Property | undefined): string {
    if (!property) return '';
    return `${property.country}, ${property.city}`;
  }

  // Helper method to format date range
  formatDateRange(startDate: Date, endDate: Date): string {
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    
    const startMonth = months[start.getMonth()];
    const endMonth = months[end.getMonth()];
    
    return `${startMonth} ${start.getDate()}, ${start.getFullYear()} - ${endMonth} ${end.getDate()}, ${end.getFullYear()}`;
  }
  isPastBooking(booking: any): boolean {
    // Check if the end date is in the past
    return new Date(booking.endDate) < new Date();
  }
  
  hasReview(booking: any): boolean {
    // Check if this booking already has a review
    return this.userReviews.some((review) => review.bookingId === booking.id);
  }
  goToPropertyPage(booking:any): void {
    // Navigate to the property page using the property ID
    const propertyId = booking.propertyId;
    window.location.href = `/property/${propertyId}`;
  }
 
}

