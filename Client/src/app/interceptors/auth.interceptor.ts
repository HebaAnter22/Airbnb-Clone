import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, finalize, switchMap, take, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
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

    // Process the request with token and handle errors
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle 401 Unauthorized errors by refreshing the token
        if (error instanceof HttpErrorResponse && error.status === 401 && token) {
          return this.handle401Error(request, next);
        }

        return throwError(() => error);
      })
    );
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

      // Get refresh token from storage
      const refreshToken = this.authService.currentUserValue?.refreshToken;
      const userId = this.authService.userId;

      if (!refreshToken || !userId) {
        // No refresh token available, must login again
        this.handleAuthError();
        return throwError(() => new Error('No refresh token available'));
      }

      return this.authService.refreshToken().pipe(
        switchMap(tokenResponse => {
          this.isRefreshing = false;
          if (tokenResponse) {
            this.refreshTokenSubject.next(tokenResponse);

            // Retry the original request with the new token
            return next.handle(this.addToken(request, tokenResponse.accessToken));
          } else {
            this.handleAuthError();
            return throwError(() => new Error('Token response is null'));
          }
        }),
        catchError(error => {
          this.isRefreshing = false;
          this.handleAuthError();
          return throwError(() => error);
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

  private handleAuthError(): void {
    // Clear auth data and redirect to login
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}