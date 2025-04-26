import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PayoutsComponent } from '../payouts/payouts.component';
import { HostPropertiesComponent } from '../host-proprties/host-properties.component';
import { BookingComponent } from '../bookings/bookings.component';
import { EarningsChartComponent } from './earnings';
import { HostPayoutComponent } from '../../../components/host-payout/host-payout.component';

@Component({
  selector: 'app-host-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    PayoutsComponent,
    HostPropertiesComponent,
    BookingComponent,
    EarningsChartComponent,
    HostPayoutComponent
  ],
  templateUrl: './host-dashboard.component.html',
  styleUrls: ['./host-dashboard.component.css']
})
export class HostDashboardComponent implements OnInit {
  currentSection: string = 'active-listings';
  isListingsDropdownOpen: boolean = false;
  isBookingsDropdownOpen: boolean = false;
  isEarningsDropdownOpen: boolean = false;
  userName: string = '';
  userFirstName: string = '';
  imageUrl: string = '';

  constructor(private router: Router) {}

  ngOnInit() {
    // Initialize user data
  }

  toggleDropdown(dropdown: string) {
    switch (dropdown) {
      case 'listings':
        this.isListingsDropdownOpen = !this.isListingsDropdownOpen;
        this.isBookingsDropdownOpen = false;
        this.isEarningsDropdownOpen = false;
        break;
      case 'bookings':
        this.isBookingsDropdownOpen = !this.isBookingsDropdownOpen;
        this.isListingsDropdownOpen = false;
        this.isEarningsDropdownOpen = false;
        break;
      case 'earnings':
        this.isEarningsDropdownOpen = !this.isEarningsDropdownOpen;
        this.isListingsDropdownOpen = false;
        this.isBookingsDropdownOpen = false;
        break;
    }
  }

  setCurrentSection(section: string) {
    this.currentSection = section;
    this.isListingsDropdownOpen = false;
    this.isBookingsDropdownOpen = false;
    this.isEarningsDropdownOpen = false;
  }

  navigateToAddProperty() {
    // Implement navigation
    this.router.navigate(['/host/add-property']);
  }

  navigateToPayouts() {
    // Navigate to the payouts page
    this.router.navigate(['/host/payouts']);
  }

  goToUserProfile() {
    // Implement navigation
  }

  logout() {
    // Implement logout
  }
} 