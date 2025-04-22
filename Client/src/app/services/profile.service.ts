import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private apiUrl = 'https://localhost:7228/api/Profile';
  
  constructor(private http: HttpClient,
              private router: Router,
              private authService: AuthService
  ) {}

  addOrRemoveToFavourites(listingId: number)
  : Observable<any> {
    const userId= this.authService.userId;
    
    const FavouritedAt = new Date().toISOString();
    const favouriteData = {
      "propertyId": listingId,
    };
  
    return this.http.post(`${this.apiUrl}/favourites`, favouriteData);
  }

  getUserFavorites(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/favourites`);
  }
  
  

  isPropertyInWishlist(propertyId: number): Observable<boolean> {
    const userId = this.authService.userId;
    if (!userId) {
      console.error('No user ID found in AuthService');
      return new Observable<boolean>(observer => observer.next(false));
    }
    return this.http.get<boolean>(`${this.apiUrl}/favourites/${propertyId}`);
  }



  getUserListings(userId:string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/host/Listings/${userId}`);
  }
  
  /**
   * Gets the current user's profile information
   */
  getUserProfile(userId:string): Observable<any> {
    return this.http.get(`${this.apiUrl}/user/${userId}`);
  }
  getUserProfileForEdit(userId:string): Observable<any> {
    return this.http.get(`${this.apiUrl}/editProfile`);
  }
  getUserReviews(userId:string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/host/reviews/${userId}`);
  }
  
  
  uploadProfilePicture(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post(`${this.apiUrl}/upload-profile-picture`, formData, {
      reportProgress: true,
      responseType: 'json'
    });
  }

  navigateToUserProfile(): void {
    const currentUserId= this.authService.userId;
    if (currentUserId) {
      this.router.navigate(['/profile', currentUserId]);
    } else {
      console.error('No user ID found in AuthService');
    }
  }

  
  
  /**
   * Gets the host profile information for a user
   * @param userId The user ID
   */
  getHostProfile(userId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/host/${userId}`);
  }



  updateProfile( profileData: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/editProfile`, profileData);
  }
}