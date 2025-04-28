import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  // Fix for Angular 19 error by using definite assignment assertion
  private isRefreshing: boolean = false;

  constructor(private authService: AuthService) { }
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.currentUserValue?.accessToken;

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    console.log('Interceptor triggered for:', request.url);
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        console.log('Full error response:', {
          status: error.status,
          headers: error.headers,
          error: error.error,
          url: error.url
        });
        if (error.status === 401 && !this.isRefreshing) {
          // Token expired, try to refresh
          console.log('Token expired, refreshing...');
          this.isRefreshing = true;

          return this.authService.refreshToken().pipe(
            switchMap(tokenResponse => {
              this.isRefreshing = false;

              if (tokenResponse) {
                console.log('Token refreshed successfully:', tokenResponse);
                // If token refresh succeeded, retry request with new token
                const newRequest = request.clone({
                  setHeaders: {
                    Authorization: `Bearer ${tokenResponse.accessToken}`
                  }
                });
                return next.handle(newRequest);
              } else {
                // If refresh failed, redirect to login
                this.authService.logout();
                return throwError(() => error);
              }
            }),
            catchError(refreshError => {
              this.isRefreshing = false;
              this.authService.logout();
              return throwError(() => refreshError);
            })
          );
        }

        return throwError(() => error);
      })
    );
  }
}