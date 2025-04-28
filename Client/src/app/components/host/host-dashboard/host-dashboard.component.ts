import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PayoutsComponent } from '../payouts/payouts.component';
import { HostPropertiesComponent } from '../host-proprties/host-properties.component';
import { BookingComponent } from '../bookings/bookings.component';
import { EarningsChartComponent } from './earnings';
import { HostPayoutComponent } from '../../../components/host-payout/host-payout.component';
import { AuthService } from '../../../services/auth.service';
import { ProfileService } from '../../../services/profile.service';

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
  userProfile: any;

  constructor(
    private router: Router,
    private authService: AuthService,
    private profileService: ProfileService
  ) { }

  ngOnInit() {
    // Initialize user data
    this.authService.getUserProfile().subscribe((userProfile: any) => {
      console.log('User profile received:', userProfile); // Log the entire object

      // Set user data from the profile
      this.userName = userProfile.userName || userProfile.email || '';
      this.userFirstName = userProfile.firstName || '';
    });

    // Get user profile from profile service
    const userId = this.authService.userId;
    if (userId) {
      this.profileService.getUserProfile(userId).subscribe({
        next: (profile) => {
          this.userProfile = profile;
          this.imageUrl = this.userProfile.profilePictureUrl;
          console.log('User Profile from profile service:', this.userProfile);
        },
        error: (err) => {
          console.error('Error loading user profile:', err);
        }
      });
    }

    // Additional fallback - get values from current user if available
    const currentUser = this.authService.currentUserValue;
    if (currentUser) {
      if (!this.userFirstName) {
        this.userFirstName = currentUser.firstName || '';
      }
      if (!this.imageUrl) {
        this.imageUrl = currentUser.imageUrl || '';
      }
    }
  }

  getProfileImageUrl(): string {
    if (this.userProfile?.profilePictureUrl) {
      return this.userProfile.profilePictureUrl;
    }
    return '';
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
    this.router.navigate(['/profile/' + this.authService.userId]);
  }

  logout() {
    this.authService.logout();
    // Implement logout
  }
} 