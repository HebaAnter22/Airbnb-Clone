import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
@Injectable({
  providedIn: 'root'
})
export class authGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const currentUser = this.authService.currentUserValue;
    console.log("currentUser", currentUser);

    if (currentUser) {
      return true;
    }

    // Store the attempted URL for redirection after login
    // Use null for the data since we don't have form inputs in this case
    this.authService.storeRedirectState(state.url, null);
    this.router.navigate(['/login']);
    return false;
  }
}