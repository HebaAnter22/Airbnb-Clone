import { Component, Input, OnInit } from '@angular/core';
import { HostService, BookingDetails } from '../../../services/host-service.service';
import { CommonModule } from '@angular/common';
import { CeilPipe } from '../../../pipes/ceil.pipe';
import { Router } from '@angular/router';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, CeilPipe],
  templateUrl: './bookings.component.html',
  styleUrls: ['./bookings.component.css']
})
export class BookingComponent implements OnInit {
  @Input() status: string = '';
  bookings: BookingDetails[] = [];
  filteredBookings: BookingDetails[] = [];
  isLoading: boolean = false;
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;

  constructor(
    private hostService: HostService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    if (!this.status) return;

    this.isLoading = true;
    this.hostService.getAllBookings().subscribe({
      next: (bookings: BookingDetails[]) => {
        this.bookings = bookings;
        this.filterBookings();
        this.isLoading = false;
        console.log(this.bookings); 
      },
      error: (error) => {
        console.error('Error fetching bookings:', error);
        this.isLoading = false;
        // Optionally display an error message to the user
      }
    });
  }

  filterBookings(): void {
    const now = new Date();
    this.filteredBookings = this.bookings.filter(booking => {
      const startDate = new Date(booking.startDate);
      const endDate = new Date(booking.endDate);

      switch (this.status.toLowerCase()) {
        case 'pending':
          return booking.status.toLowerCase() === 'pending';
        case 'current':
          return booking.status.toLowerCase() === 'confirmed' && startDate <= now && endDate >= now;
        case 'upcoming':
          return booking.status.toLowerCase() === 'confirmed' && startDate > now;
        case 'past':
          return endDate < now;
        default:
          return false;
      }
    });

    this.totalCount = this.filteredBookings.length;
    this.applyPagination();
  }

  confirmBooking(bookingId: number): void {
    this.hostService.confirmBooking(bookingId).subscribe({
      next: () => {
        this.loadBookings();
      }
    });
  }
  applyPagination(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    this.filteredBookings = this.filteredBookings.slice(startIndex, startIndex + this.pageSize);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.applyPagination();
  }

  viewDetails(bookingId: number): void {
    this.router.navigate(['/booking', bookingId]);
  }
}