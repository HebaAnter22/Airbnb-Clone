import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, finalize, switchMap, take, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  // URLs that don't require authentication
  private publicUrls = [
    '/api/Auth/login',
    '/api/Auth/register',
    '/api/Auth/forgot-password',
    '/api/Auth/reset-password',
    '/api/Auth/validate-token',
    '/api/Auth/google-auth'
  ];

  // Token refresh flags and subject
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

  constructor(
    private authService: AuthService,
    private router: Router
  ) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {


    // Add token if available
    const token = this.authService.currentUserValue?.accessToken;
    if (token) {
      request = this.addToken(request, token);
    }

    // Process the request with token
    return next.handle(request);
  }

  private isPublicUrl(url: string): boolean {
    return this.publicUrls.some(publicUrl => url.includes(publicUrl));
  }

  private addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refreshToken().pipe(
        switchMap(tokenResponse => {
          if (tokenResponse) {
            this.refreshTokenSubject.next(tokenResponse);
            return next.handle(this.addToken(request, tokenResponse.accessToken));
          }

          // If refresh fails, redirect to login
          this.authService.logout();
          this.router.navigate(['/login']);
          return throwError(() => new Error('Token refresh failed'));
        }),
        catchError(error => {
          // On refresh error, logout and redirect
          this.authService.logout();
          this.router.navigate(['/login']);
          return throwError(() => error);
        }),
        finalize(() => {
          this.isRefreshing = false;
        })
      );
    } else {
      // Wait for the ongoing token refresh to complete
      return this.refreshTokenSubject.pipe(
        filter(token => token !== null),
        take(1),
        switchMap(token => {
          return next.handle(this.addToken(request, token.accessToken));
        })
      );
    }
  }
}