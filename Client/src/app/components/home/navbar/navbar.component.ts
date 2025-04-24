import { Component, HostListener, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { ProfileService } from '../../../services/profile.service';

interface Notification {
  id: number;
  message: string;
  time: string;
  read: boolean;
}
@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})

export class NavbarComponent implements OnInit {
  @Input() isHeaderScrolled: boolean = false;
  isDropdownOpen = false;
  loggedIn: boolean = false; 
  imageUrl: string = ''; // Default image URL
  userFirstName:string='';
  IsUserGuest:boolean=false;

 
  // Add these properties to your component class
  isNotificationOpen: boolean = false;
  notificationCount: number = 0;
  notifications: Notification[] = [];
  
 




  toggleDropdown(event: Event) {
    event.stopPropagation(); // Prevent the click from bubbling up to the document
    this.isDropdownOpen = !this.isDropdownOpen;
  }
  constructor(private authService:AuthService,
    private profileService:ProfileService,
    private router:Router
  ) {
    if(this.authService.userId){
      this.loggedIn = true;
    }
  }

  ngOnInit() {
    this.profileService.getUserProfile(this.authService.userId?this.authService.userId:''  ).subscribe(
      
      (profile: any) => {
        this.userFirstName = profile.firstName || 'User'; // Default to 'User' if first name is not available
        this.imageUrl = profile.profilePictureUrl? 'https://localhost:7228'+ profile.profilePictureUrl :this.imageUrl// Use default image if profile picture URL is not available
      },
      (error) => {
        console.error('Error loading profile:', error);
      }
    );
    this.initializeNotifications();

    this.IsUserGuest = this.authService.isUserAGuest();
  }
  
  hostYourHomeClicked() {
    if (this.authService.isUserAGuest()) {

    }
  }








   // Add this to your ngOnInit or constructor
   initializeNotifications() {
    // Example notifications - replace with your actual data source
    this.notifications = [
      {
        id: 1,
        message: 'Your booking request for Villa Garden has been accepted',
        time: '2 hours ago',
        read: false
      },
      {
        id: 2,
        message: 'Welcome discount: 15% off your next booking!',
        time: '1 day ago',
        read: false
      },
      {
        id: 3,
        message: 'Complete your profile to unlock special offers',
        time: '3 days ago',
        read: true
      }
    ];
    
    // Count unread notifications
    this.updateNotificationCount();
  }
  
  // Method to toggle notification dropdown
  toggleNotifications(event: Event) {
    event.stopPropagation();
    this.isNotificationOpen = !this.isNotificationOpen;
    
    if (this.isDropdownOpen) {
      this.isDropdownOpen = false;
    }
  }
  
  // Update the notification count
  updateNotificationCount() {
    this.notificationCount = this.notifications.filter(notification => !notification.read).length;
  }
  
  // Mark all notifications as read
  markAllAsRead() {
    this.notifications.forEach(notification => {
      notification.read = true;
    });
    this.updateNotificationCount();
  }
  
  // Close notifications when clicking elsewhere
  // Add this to your existing document click handler or create one
  @HostListener('document:click', ['$event'])
  clickOutside(event: Event) {
    // Close notification dropdown when clicking outside
    const target = event.target as HTMLElement | null;
    if (this.isNotificationOpen && target && !target.closest('.notification-container')) {
      this.isNotificationOpen = false;
    }
    
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