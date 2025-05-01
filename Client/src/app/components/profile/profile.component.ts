import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../services/profile.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { MainNavbarComponent } from '../main-navbar/main-navbar.component';
import { MessageUserButtonComponent } from '../chatting/components/message-user-button/message-user-button.component';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  standalone: true,
  imports: [CommonModule, MainNavbarComponent, MessageUserButtonComponent],
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  isCurrentUserProfile: boolean = false;
  backendBaseUrl = 'https://localhost:7228';
  userProfile: any;
  hostProfile: any;
  reviews: any[] = [];
  isLoading = true;
  errorMessage = '';
  activeTab: string = 'about';
  currentUserId = 0;


  profileId: number = 0;

  selectedFile: File | null = null;
  uploadProgress: number = 0;
  isUploading: boolean = false;
  emailVerified: boolean = false;
  idVerified: boolean = true;
  Listings: any[] = [];

  constructor(
    private profileService: ProfileService,
    private router: Router,
    private authService: AuthService,
    private http: HttpClient,
    private route: ActivatedRoute,
  ) { }

  ngOnInit(): void {
    // Get current user ID from auth service
    this.currentUserId = parseInt(this.authService.userId || '0');

    // Get profile user ID from route params
    const profileUserId: string = this.route.snapshot.paramMap.get('id') || '';
    this.profileId = Number(profileUserId);

    // Compare numeric IDs to determine if this is the current user's profile
    this.isCurrentUserProfile = this.currentUserId === this.profileId;

    console.log('Current user ID:', this.currentUserId);
    console.log('Profile ID:', this.profileId);
    console.log('Is current user profile:', this.isCurrentUserProfile);

    this.loadUserProfile(profileUserId);
    this.loadUserReviews(profileUserId);
    this.loadUserListings(profileUserId);
    this.assignVerificationStatus();
  }
  assignVerificationStatus(): void {
    this.authService.checkEmailVerificationStatus().subscribe({
      next: (isVerified: boolean) => {
        this.emailVerified = isVerified;
        console.log('Email verification status:', isVerified);
      },
      error: (err: any) => {
        console.error('Error checking email verification status:', err);
      }
    });
  }

  goToVerificationPage(): void {
    this.router.navigate(['/verification']);
  }

  goToEditProfile(): void {
    this.router.navigate(['/editProfile', this.userProfile.id]);
  }

  loadUserListings(userId: string): void {
    this.profileService.getUserListings(userId).subscribe({
      next: (listings: any[]) => {
        this.Listings = listings.map(listing => ({
          ...listing,
          // Ensure images array exists
          images: listing.images || []
        }));
      },
      error: (err) => {
        this.Listings = []; // Set empty array on error
      }
    });
  }

  loadUserReviews(userId: string): void {
    this.profileService.getUserReviews(userId).subscribe({
      next: (reviews: any[]) => {
        this.reviews = reviews;
      },
      error: (err) => {
        console.error('Reviews load error:', err);
      },
      complete: () => {
        // If we've loaded reviews but still don't have a host profile,
        // calculate rating for the guest profile
        if (this.hostProfile) {
          this.hostProfile.rating = this.calculateUserRatingFromReviews();
          this.hostProfile.totalReviews = this.reviews.length;
        }
      }
    });
  }

  shouldShowCreateProfile(): boolean {
    if (!this.hostProfile) return true;
    const fieldsToCheck = [
      this.hostProfile.work,
      this.hostProfile.education,
      this.hostProfile.dreamDestination,
      this.hostProfile.specialAbout,
      this.hostProfile.languages,
      this.hostProfile.livesIn,
      this.hostProfile.funFact,
      this.hostProfile.obsessedWith,
      this.hostProfile.pets
    ];

    const emptyCount = fieldsToCheck.filter(field => !field).length;
    return emptyCount > 7;
  }

  getProfileImageUrl(): string {
    if (this.userProfile?.profilePictureUrl) {
      return this.userProfile.profilePictureUrl;
    }
    return this.userProfile.profilePictureUrl; // Default image if none is set
  }

  loadUserProfile(userId: string): void {
    this.profileService.getUserProfile(userId).subscribe({
      next: (profile) => {
        this.userProfile = profile;
        console.log('User Profile:', this.userProfile);

        if (this.userProfile.role != 'Guest') {
          this.loadHostProfile(userId);
        } else {
          // Create a default hostProfile for guests
          this.router.navigate(['**']);
          this.createDefaultHostProfile();
          this.isLoading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'Failed to load profile';
        this.isLoading = false;
        console.error('Profile load error:', err);
      }
    });
  }

  // Create a default host profile structure for guests
  createDefaultHostProfile(): void {
    this.hostProfile = {
      rating: 0,
      totalReviews: 0,
      aboutMe: '',
      work: '',
      education: '',
      languages: '',
      dreamDestination: '',
      specialAbout: '',
      livesIn: '',
      funFact: '',
      obsessedWith: '',
      pets: '',
      isVerified: false,
      properties: []
    };

    // Once reviews are loaded, update the rating
    if (this.reviews.length > 0) {
      this.hostProfile.rating = this.calculateUserRatingFromReviews();
      this.hostProfile.totalReviews = this.reviews.length;
    }
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/png', 'image/gif'];
      if (!allowedTypes.includes(file.type)) {
        this.errorMessage = 'Only JPG, PNG, and GIF images are allowed';
        return;
      }

      // Validate file size (5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.errorMessage = 'File size exceeds 5MB limit';
        return;
      }

      this.selectedFile = file;
      this.errorMessage = '';
    }
  }

  uploadProfilePicture(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadProgress = 0;

    this.profileService.uploadProfilePicture(this.selectedFile).subscribe({
      next: (response: any) => {
        this.userProfile.profilePictureUrl = response.fileUrl;
        this.isUploading = false;
        this.selectedFile = null;
      },
      error: (err) => {
        this.errorMessage = 'Failed to upload profile picture';
        this.isUploading = false;
        console.error('Upload error:', err);
      }
    });
  }

  loadHostProfile(userId: string): void {
    this.profileService.getHostProfile(userId).subscribe({
      next: (host) => {
        this.hostProfile = host;
        // Set default values for any missing profile data
        if (!this.hostProfile.rating) this.hostProfile.rating = 0;
        if (!this.hostProfile.totalReviews) this.hostProfile.totalReviews = 0;
        if (!this.hostProfile.aboutMe) this.hostProfile.aboutMe = '';
        if (!this.hostProfile.work) this.hostProfile.work = '';
        if (!this.hostProfile.education) this.hostProfile.education = '';
        if (!this.hostProfile.languages) this.hostProfile.languages = '';
        if (!this.hostProfile.obsessedWith) this.hostProfile.obsessedWith = '';
        if (!this.hostProfile.dreamDestination) this.hostProfile.dreamDestination = '';
        if (!this.hostProfile.specialAbout) this.hostProfile.specialAbout = '';

        // Update with review data if available
        if (this.reviews.length > 0) {
          this.hostProfile.rating = this.calculateUserRatingFromReviews();
          this.hostProfile.totalReviews = this.reviews.length;
        }

        this.isLoading = false;
      },
      error: (err) => {
        // If hostProfile fails to load, create a default one
        // console.error('Host profile load error:', err);
        this.createDefaultHostProfile();
        this.isLoading = false;
      }
    });
  }

  calculateUserRatingFromReviews(): number {
    if (this.reviews.length === 0) return 0;
    const totalRating = this.reviews.reduce((acc, review) => acc + review.rating, 0);
    return totalRating / this.reviews.length;
  }

  getHostingYears(): number {
    if (!this.hostProfile?.startDate) return 1; // Default value for guests or new hosts
    const startDate = new Date(this.hostProfile.startDate);
    const currentDate = new Date();
    return currentDate.getFullYear() - startDate.getFullYear();
  }

  setActiveTab(tab: string): void {
    window.scrollTo(0, 0);
    this.activeTab = tab;
  }

  goToPropertyPage(listingId: string): void {
    this.router.navigate(['/property', listingId]);
  }


  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
