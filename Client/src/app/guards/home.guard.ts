import { Injectable } from '@angular/core';
import {
    CanActivate,
    ActivatedRouteSnapshot,
    RouterStateSnapshot,
    Router
} from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class HomeGuard implements CanActivate {

    constructor(private authService: AuthService, private router: Router) { }

    canActivate(
        next: ActivatedRouteSnapshot,
        state: RouterStateSnapshot): boolean {

        // If user is admin, redirect to admin dashboard
        if (this.authService.isAdmin()) {
            this.router.navigate(['/admin']);
            return false;
        }

        // Allow non-admin users to access the home page
        return true;
    }
} 