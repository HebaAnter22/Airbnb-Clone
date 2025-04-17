import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../services/profile.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  isCurrentUserProfile: boolean = false;
  backendBaseUrl = 'https://localhost:7228';
  userProfile: any;
  hostProfile: any;
  reviews: any[] = [] ;
  isLoading = true;
  errorMessage = '';
  activeTab: string = 'about';
  
  selectedFile: File | null = null;
  uploadProgress: number = 0;
  isUploading: boolean = false;

  Listings: any[] = [];

  constructor(
    private profileService: ProfileService, 
    private router: Router,
    private authService: AuthService,
    private http: HttpClient,
    private route: ActivatedRoute
  ) {}
  
  ngOnInit(): void {
    const profileUserId: string = this.route.snapshot.paramMap.get('id') || '';

    const currentUserId: string = this.authService.userId || '';
    this.isCurrentUserProfile = profileUserId === currentUserId;
    
    this.loadUserProfile(profileUserId);
    this.loadUserReviews(profileUserId);
    this.loadUserListings(profileUserId);
  }

  goToEditProfile(): void {
    this.router.navigate(['/editProfile', this.userProfile.id]);
  }
  loadUserListings(userId:string): void {
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

  
  loadUserReviews(userId:string): void {
    this.profileService.getUserReviews(userId).subscribe({
      next: (reviews:any[]) => {
        this.reviews = reviews;
      },
      error: (err) => {
        console.error('Reviews load error:', err);
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
      
      return this.backendBaseUrl + this.userProfile.profilePictureUrl;
    }
    return 'assets/default-avatar.jpg';
  }
  
  loadUserProfile(userId:string): void {
    this.profileService.getUserProfile(userId).subscribe({
      next: (profile) => {
        this.userProfile = profile;
        this.loadHostProfile(userId);
      },
      error: (err) => {
        this.errorMessage = 'Failed to load profile';
        this.isLoading = false;
        console.error('Profile load error:', err);
      }
    });
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




  loadHostProfile(userId:string): void {
    this.profileService.getHostProfile(userId).subscribe({
      next: (host) => {
        this.hostProfile = host;
        // Set default values for any missing profile data
        this.hostProfile.rating = this.calculateUserRatingFromReviews();
        this.hostProfile.totalReviews = this.reviews.length;

        if (!this.hostProfile.rating) this.hostProfile.rating = 0;
        if (!this.hostProfile.totalReviews) this.hostProfile.totalReviews = 0;
        if (!this.hostProfile.aboutMe) this.hostProfile.aboutMe = '';
        if (!this.hostProfile.work) this.hostProfile.work = '';
        if (!this.hostProfile.education) this.hostProfile.education = '';
        if (!this.hostProfile.languages) this.hostProfile.languages = '';
        
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Host profile load error:', err);
        this.isLoading = false;
      }
    });
  }
  calculateUserRatingFromReviews(): number {
    const totalRating = this.reviews.reduce((acc, review) => acc + review.rating, 0);

    return totalRating / this.reviews.length;
  }
  
  getHostingYears(): number {
    if (!this.hostProfile?.startDate) return 2; // Default value
    const startDate = new Date(this.hostProfile.startDate);
    const currentDate = new Date();
    return currentDate.getFullYear() - startDate.getFullYear();
  }
  
  setActiveTab(tab: string): void {
    //scroll up
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