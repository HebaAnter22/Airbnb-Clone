import { Injectable } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { AuthService } from './auth.service';
import { filter } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AdminRedirectService {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {
    // Listen to route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      // Check if we're navigating to home and user is admin
      if ((event.url === '/home' || event.url === '/') && this.authService.isAdmin()) {
        this.router.navigate(['/admin']);
      }
    });
  }

  // Initialize to check if admin is trying to access home
  initializeRedirect() {
    if (this.authService.isAdmin() && 
        (this.router.url === '/home' || this.router.url === '/')) {
      this.router.navigate(['/admin']);
    }
  }
} 