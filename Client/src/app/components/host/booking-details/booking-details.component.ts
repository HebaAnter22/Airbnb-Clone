import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HostService, BookingDetails } from '../../../services/host-service.service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-booking-details',
  templateUrl: './booking-details.component.html',
  styleUrls: ['./booking-details.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule
  ]
})
export class BookingDetailsComponent implements OnInit {
  bookingId: number = 0;
  bookingDetails: BookingDetails | null = null;
  loading: boolean = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private hostService: HostService
  ) { }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.bookingId = +params['bookingId'];
      this.loadBookingDetails();
    });
  }

  loadBookingDetails(): void {
    this.loading = true;
    this.error = null;
    
    // Since there's no direct method to get booking details by ID,
    // we'll get all bookings and filter for the one we need
    this.hostService.getAllBookings().subscribe({
      next: (bookings: BookingDetails[]) => {
        this.bookingDetails = bookings.find(booking => booking.id === this.bookingId) || null;
        if (!this.bookingDetails) {
          this.error = 'Booking not found.';
        }
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load booking details. Please try again later.';
        this.loading = false;
        console.error('Error loading booking details:', err);
      }
    });
  }

  confirmBooking(): void {
    if (!this.bookingId) return;
    
    this.hostService.confirmBooking(this.bookingId).subscribe({
      next: () => {
        this.loadBookingDetails(); // Reload the details after confirmation
      },
      error: (err: any) => {
        this.error = 'Failed to confirm booking. Please try again later.';
        console.error('Error confirming booking:', err);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/bookings']);
  }
} 