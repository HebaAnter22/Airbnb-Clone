import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RedirectService {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  /**
   * Redirect to login page while preserving form data
   * @param url The URL to redirect back to after login
   * @param formData The form data to preserve
   */
  redirectToLogin(url: string, formData: any): void {
    this.authService.storeRedirectState(url, formData);
    this.router.navigate(['/login']);
  }

  /**
   * Check if there's form data for the current URL and restore it
   * @param currentUrl The current URL to check for saved data
   * @returns The saved form data or null if none exists
   */
  getSavedFormData(currentUrl: string): any {
    const redirectUrl = this.authService.getRedirectUrl();
    const redirectData = this.authService.getRedirectData();
    
    if (redirectUrl && redirectUrl.includes(currentUrl) && redirectData) {
      // Clear the redirect state to prevent reusing it
      this.authService.clearRedirectState();
      return redirectData;
    }
    
    return null;
  }
} 