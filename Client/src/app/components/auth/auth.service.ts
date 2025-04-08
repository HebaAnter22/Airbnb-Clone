// src/app/components/auth/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { User, TokenResponse } from '../../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'https://localhost:7228/api/auth/';
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser: Observable<User | null>;

  constructor(private http: HttpClient, private router: Router) {
    const storedUser = localStorage.getItem('currentUser');
    this.currentUserSubject = new BehaviorSubject<User | null>(
      storedUser ? JSON.parse(storedUser) : null
    );
    this.currentUser = this.currentUserSubject.asObservable();
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  login(email: string, password: string): Observable<User> {
    return this.http.post<any>(`${this.baseUrl}login`, { email, password }).pipe(
      map((response: TokenResponse) => {
        const decoded = this.decodeToken(response.accessToken);
        const user: User = {
          email: decoded.unique_name,
          role: decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],          accessToken: response.accessToken,
          refreshToken: response.refreshToken
        };
        localStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUserSubject.next(user);
        return user;
      })
    );
  }





  register(
    email: string, firstName: string, lastName: string,  password: string
  ) {
    return this.http.post(`${this.baseUrl}register`, {  
      email,
      firstName,
      lastName,
      password });
  }
  logout() {
    // First get the token to ensure it's available for the interceptor
    const token = this.currentUserValue?.accessToken;
    
    if (token) {
      console.log('Logging out with token:', token);
      this.http.post(`${this.baseUrl}logout`, {}).subscribe({
        next: () => {
          localStorage.removeItem('currentUser');
          this.currentUserSubject.next(null);
          this.router.navigate(['/login']);
        },
        error: (error) => {
          console.error('Logout failed', error);
          // Even if the server call fails, clear local data
          localStorage.removeItem('currentUser');
          this.currentUserSubject.next(null);
          this.router.navigate(['/login']);
        }
      });
    } else {
      // No token, just clear local data
      console.log('No token available, clearing local data only');
      localStorage.removeItem('currentUser');
      this.currentUserSubject.next(null);
      this.router.navigate(['/login']);
    }
  }
  refreshToken(): Observable<TokenResponse | null> {
    const user = this.currentUserValue;
    if (!user?.refreshToken) return of(null);

    return this.http.post<TokenResponse>(`${this.baseUrl}refresh-token`, {
      userId: this.getUserIdFromToken(user.accessToken),
      refreshToken: user.refreshToken
    }).pipe(
      map((response: TokenResponse) => {
        const decoded = this.decodeToken(response.accessToken);
        const updatedUser: User = {
          email: decoded.unique_name,
          role: decoded.role,
          accessToken: response.accessToken,
          refreshToken: response.refreshToken
        };
        localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        this.currentUserSubject.next(updatedUser);
        return response;
      }),
      catchError(err => {
        this.logout();
        return throwError(() => err);
      })
    );
  }
  

  public decodeToken(token: string): any {
    const decoded = JSON.parse(atob(token.split('.')[1]));
    return {
        ...decoded,
        nameid: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        unique_name: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    };
}

  private getUserIdFromToken(token: string): string {
    const decoded = this.decodeToken(token);
    return decoded.nameid;
  }

  handleError(error: any) {
    if (error.status === 401 || error.status === 403) {
      this.logout();
      this.router.navigate(['/forbidden']);
    }
    return throwError(() => error);
  }
getUserProfile(): Observable<any> {
  return this.http.get(`${this.baseUrl}profile`).pipe(
    catchError(error => {
      if (error.status === 401) {
        this.logout();
        this.router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
}
}
