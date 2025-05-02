import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HostService, BookingDetails } from '../../../services/host-service.service';
import { ProfileService } from '../../../services/profile.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatBadgeModule } from '@angular/material/badge';
import { MatListModule } from '@angular/material/list';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSliderModule } from '@angular/material/slider';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTreeModule } from '@angular/material/tree';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRippleModule } from '@angular/material/core';
import { MatBottomSheetModule } from '@angular/material/bottom-sheet';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-property-booking-details',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressBarModule,
    MatChipsModule,
    MatTooltipModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatPaginatorModule,
    MatSortModule,
    MatTabsModule,
    MatExpansionModule,
    MatBadgeModule,
    MatListModule,
    MatGridListModule,
    MatMenuModule,
    MatSidenavModule,
    MatToolbarModule,
    MatAutocompleteModule,
    MatCheckboxModule,
    MatRadioModule,
    MatSlideToggleModule,
    MatSliderModule,
    MatStepperModule,
    MatTreeModule,
    MatButtonToggleModule,
    MatProgressSpinnerModule,
    MatRippleModule,
    MatBottomSheetModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './property-booking-details.component.html',
  styleUrls: ['./property-booking-details.component.css']
})
export class PropertyBookingDetailsComponent implements OnInit {
  propertyId: number = 0;
  bookings: BookingDetails[] = [];
  loading: boolean = true;
  error: string | null = null;
  
  // Analytics data
  totalBookings: number = 0;
  totalRevenue: number = 0;
  averageBookingDuration: number = 0;
  averageRevenuePerBooking: number = 0;
  bookingStatusCounts: { [key: string]: number } = {};
  monthlyBookings: { [key: string]: number } = {};
  monthlyRevenue: { [key: string]: number } = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private hostService: HostService,
    private snackBar: MatSnackBar,
    private profileService: ProfileService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.propertyId = +params['id'];
      if (this.propertyId) {
        this.loadBookingDetails();
      } else {
        this.error = 'Invalid property ID';
        this.loading = false;
      }
    });
  }

  loadBookingDetails(): void {
    this.loading = true;
    this.hostService.getPropertyBookingDetails(this.propertyId).subscribe({
      next: (bookings: BookingDetails[]) => {
        this.bookings = bookings;
        this.calculateAnalytics();
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load booking details';
        this.loading = false;
        this.showError('Failed to load booking details');
        console.error('Error loading booking details:', err);
      }
    });
  }

  calculateAnalytics(): void {
    if (!this.bookings || this.bookings.length === 0) {
      return;
    }

    // Total bookings
    this.totalBookings = this.bookings.length;

    // Total revenue
    this.totalRevenue = this.bookings.reduce((sum, booking) => sum + booking.totalAmount, 0);

    // Average booking duration
    const totalDays = this.bookings.reduce((sum, booking) => {
      const startDate = new Date(booking.startDate);
      const endDate = new Date(booking.endDate);
      const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
      return sum + diffDays;
    }, 0);
    this.averageBookingDuration = totalDays / this.totalBookings;

    // Average revenue per booking
    this.averageRevenuePerBooking = this.totalRevenue / this.totalBookings;

    // Booking status counts
    this.bookingStatusCounts = this.bookings.reduce((counts, booking) => {
      const status = booking.status || 'Unknown';
      counts[status] = (counts[status] || 0) + 1;
      return counts;
    }, {} as { [key: string]: number });

    // Monthly bookings and revenue
    this.monthlyBookings = {};
    this.monthlyRevenue = {};
    
    this.bookings.forEach(booking => {
      const startDate = new Date(booking.startDate);
      const monthYear = `${startDate.getFullYear()}-${String(startDate.getMonth() + 1).padStart(2, '0')}`;
      
      this.monthlyBookings[monthYear] = (this.monthlyBookings[monthYear] || 0) + 1;
      this.monthlyRevenue[monthYear] = (this.monthlyRevenue[monthYear] || 0) + booking.totalAmount;
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return 'accent';
      case 'pending':
        return 'warn';
      case 'cancelled':
        return 'error';
      case 'completed':
        return 'primary';
      default:
        return '';
    }
  }

  showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar']
    });
  }

  goBack(): void {
    if (this.profileService.getUserRole() === 'Host') {
      this.router.navigate(['/host']);
    } else {
      this.router.navigate(['/admin']);
    }
  }
} 