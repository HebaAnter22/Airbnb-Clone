import { Component, OnInit } from '@angular/core';
import { HostPropertiesComponent } from '../host-proprties/host-properties.component';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { ProfileService } from '../../../services/profile.service';
import { BookingComponent } from '../bookings/bookings.component';
import { Router } from '@angular/router';
import { EarningsChartComponent } from './earnings';

@Component({
  selector: 'app-host-dashboard',
  imports: [HostPropertiesComponent, CommonModule , BookingComponent, EarningsChartComponent],
  templateUrl: './host-dashboard.component.html',
  styleUrls: ['./host-dashboard.component.css']
})
export class HostDashboardComponent implements OnInit {
  currentSection: string = 'active-listings';
  isListingsDropdownOpen: boolean = false;
  isBookingsDropdownOpen: boolean = false;
  isEarningsDropdownOpen: boolean = false;
  userProfileImage: string = 'assets/default-profile.png';
  userName: string = '';
  userFirstName: string = '';

  imageUrl: string = ''; 
  constructor(
    private authService: AuthService,
    private profileService: ProfileService,
    private router: Router
  ) { 
    this.imageUrl= this.profileService.getImageUrl();

  }

  ngOnInit(): void {
    // Get user ID from AuthService
    const userId = this.authService.userId;
    if (userId) {
      // Get user profile data
      this.profileService.getUserProfile(userId).subscribe({
        next: (profile: any) => {
          this.userName = profile.firstName && profile.lastName 
            ? `${profile.firstName} ${profile.lastName}`
            : 'Host';
            this.imageUrl = profile.profilePictureUrl? profile.profilePictureUrl:'';
            this.userFirstName = profile.firstName || 'User';
            // Use profile picture if available
          if (profile.profilePictureUrl) {
            this.userProfileImage = profile.profilePictureUrl;
          }
        },
        error: (error) => {
          console.error('Error loading profile:', error);
          this.userName = 'Host';
        }
      });
    }
  }

  goToUserProfile(): void {
    this.profileService.navigateToUserProfile();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  toggleDropdown(type: 'listings' | 'bookings' | 'earnings'): void {
    if (type === 'listings') {
      this.isListingsDropdownOpen = !this.isListingsDropdownOpen;
      this.isBookingsDropdownOpen = false;
      this.isEarningsDropdownOpen = false;
    } else if (type === 'bookings') {
      this.isBookingsDropdownOpen = !this.isBookingsDropdownOpen;
      this.isListingsDropdownOpen = false;
      this.isEarningsDropdownOpen = false;
    } else if (type === 'earnings') {
      this.isEarningsDropdownOpen = !this.isEarningsDropdownOpen;
      this.isListingsDropdownOpen = false;
      this.isBookingsDropdownOpen = false;
    }
  }

  setCurrentSection(section: string): void {
    this.currentSection = section;
    this.isListingsDropdownOpen = false;
    this.isBookingsDropdownOpen = false;
    this.isEarningsDropdownOpen = false;
  }

  navigateToAddProperty(): void {
    this.router.navigate(['/host/add-property']);
    this.isListingsDropdownOpen = false;
  }

  // Close dropdowns when clicking outside
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.dropdown')) {
      this.isListingsDropdownOpen = false;
      this.isBookingsDropdownOpen = false;
      this.isEarningsDropdownOpen = false;
    }
  }
} 