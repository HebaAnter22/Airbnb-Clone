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
      console.log('RoleGuard - Current Role:', currentUser?.role, 'Required Role:', requiredRole);
      
      if (currentUser && currentUser.role === requiredRole) {
        return true;
      }
      
      this.router.navigate(['/forbidden']);
      return false;
    }
  }