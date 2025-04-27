import { Component, HostListener, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { ProfileService } from '../../../services/profile.service';
import { ChatDropdownComponent } from '../../chat/chat-dropdown/chat-dropdown.component';
import { NotificationComponent } from '../notification/notification.component';


@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, ChatDropdownComponent, NotificationComponent],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})

export class NavbarComponent implements OnInit {
  @Input() isHeaderScrolled: boolean = false;
  isDropdownOpen = false;
  loggedIn: boolean = false;
  imageUrl: string = ''; // Default image URL
  userFirstName: string = '';
  IsUserGuest: boolean = false;








  toggleDropdown(event: Event) {
    event.stopPropagation(); // Prevent the click from bubbling up to the document
    this.isDropdownOpen = !this.isDropdownOpen;
  }
  constructor(private authService: AuthService,
    private profileService: ProfileService,
    private router: Router
  ) {
    if (this.authService.userId) {
      this.loggedIn = true;
    }
  }

  ngOnInit() {

    this.profileService.getUserProfile(this.authService.userId ? this.authService.userId : '').subscribe(

      (profile: any) => {
        this.userFirstName = profile.firstName || 'User'; // Default to 'User' if first name is not available
        this.imageUrl = profile.profilePictureUrl ? 'https://localhost:7228' + profile.profilePictureUrl : this.imageUrl// Use default image if profile picture URL is not available
      },
      (error) => {
        console.error('Error loading profile:', error);
      }
    );

    this.IsUserGuest = this.authService.isUserAGuest();
  }

  hostYourHomeClicked() {
    if (this.authService.isUserAGuest()) {

    }
  }




  clickOutside(event: Event) {
    // Close notification dropdown when clicking outside
    const target = event.target as HTMLElement | null;

    // If you already have a dropdown close handler, integrate this logic there
    if (this.isDropdownOpen && target && !target.closest('.menu-profile') && !target.closest('.dropdown')) {
      this.isDropdownOpen = false;
    }
  }



  ProfileClicked() {
    this.router.navigate([`/profile/${this.authService.userId}`]);
  }

  editProfileClicked() {

    this.router.navigate([`/editProfile/${this.authService.userId}`]);
  }
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    const target = event.target as HTMLElement;
    const menuProfile = document.querySelector('.menu-profile');
    const dropdown = document.querySelector('.dropdown');

    // Close dropdown if clicking outside the menu-profile and dropdown
    if (
      this.isDropdownOpen &&
      menuProfile &&
      dropdown &&
      !menuProfile.contains(target) &&
      !dropdown.contains(target)
    ) {
      this.isDropdownOpen = false;
    }
  }
  logout() {
    console.log('Logging out...'); // Debugging log
    this.authService.logout();

  }
}