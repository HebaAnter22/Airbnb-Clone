import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../components/auth/auth.service';
@Injectable({
    providedIn: 'root'
  })
  export class authGuard implements CanActivate {
    constructor(private authService: AuthService, private router: Router) {}
  
    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
      const currentUser = this.authService.currentUserValue;
      
      if (currentUser) {
        return true;
      }
      
      this.router.navigate(['/login']);
      return false;
    }
  }