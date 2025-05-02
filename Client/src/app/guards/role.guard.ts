import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
@Injectable({
    providedIn: 'root'
  })
  export class roleGuard implements CanActivate {
    constructor(private authService: AuthService, private router: Router) {}
  
    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
      const currentUser = this.authService.currentUserValue;
      const requiredRole = route.data['role'];
      
      // If user is not logged in, store the URL and redirect to login
      if (!currentUser) {
        this.authService.storeRedirectState(state.url, null);
        this.router.navigate(['/login']);
        return false;
      }
      
      // If user is logged in but doesn't have the required role
      if (currentUser.role !== requiredRole) {
        this.router.navigate(['/forbidden']);
        return false;
      }
      
      return true;
    }
  }