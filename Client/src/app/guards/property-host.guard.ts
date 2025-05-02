import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CreatePropertyService } from '../services/property-crud.service';
import { Observable, map, of, catchError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PropertyHostGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private propertyService: CreatePropertyService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    const propertyId = +route.paramMap.get('id')!;
    const currentUserId = this.authService.userId;

    // If no user is logged in, allow access
    if (!currentUserId) {
      return of(true);
    }

    // Check if the current user is the property host
    return this.propertyService.getPropertyById(propertyId).pipe(
      map(property => {
        // If the logged-in user is the property host, redirect to edit page
        if (property && property.hostId && currentUserId === property.hostId.toString()) {
          this.router.navigate(['/host/edit', propertyId]);
          return false;
        }
        // Otherwise allow access
        return true;
      }),
      catchError(() => {
        // If there's an error fetching the property, allow access
        // The component will handle property not found errors
        return of(true);
      })
    );
  }
} 