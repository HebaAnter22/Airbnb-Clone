// src/app/components/auth/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, catchError, map, of, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { User, TokenResponse } from '../models/user.model';
import { SocialAuthService, SocialUser } from "@abacritt/angularx-social-login";
import { GoogleLoginProvider } from "@abacritt/angularx-social-login";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'https://localhost:7228/api/Auth/';
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser: Observable<User | null>;




  constructor(private http: HttpClient, private router: Router,
    private socialAuthService: SocialAuthService

  ) {
    const storedUser = localStorage.getItem('currentUser');
    this.currentUserSubject = new BehaviorSubject<User | null>(
      storedUser ? JSON.parse(storedUser) : null
    );
    this.currentUser = this.currentUserSubject.asObservable();
    this.socialAuthService.authState.subscribe((user: SocialUser) => {
      if (user) {
        this.handleGoogleLogin(user);
      }
    });
  }
  signInWithGoogle(): void {
    this.socialAuthService.signIn(GoogleLoginProvider.PROVIDER_ID);
}

// auth.service.ts
switchToHosting(): Observable<TokenResponse> {
  return this.http.post<TokenResponse>(`${this.baseUrl}switch-to-host`, {}).pipe(
    tap((response) => {
      // Decode the new token and update user state
      const decoded = this.decodeToken(response.accessToken);
      const updatedUser: User = {
        email: decoded.unique_name,
        role: decoded.role,
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        firstName: this.currentUserValue?.firstName, // Preserve existing fields
        lastName: this.currentUserValue?.lastName,
        imageUrl: this.currentUserValue?.imageUrl,
      };
      
      // Update localStorage and BehaviorSubject
      localStorage.setItem('currentUser', JSON.stringify(updatedUser));
      this.currentUserSubject.next(updatedUser);
    }),
    catchError((err) => {
      console.error('Role switch failed', err);
      return throwError(() => err);
    })
  );
}


isUserAGuest(): boolean {
  const user = this.currentUserSubject.value;
  return user ? user.role === 'Guest' : false;
}
private handleGoogleLogin(googleUser: SocialUser): void {
    this.http.post(`${this.baseUrl}google-auth`, {
        email: googleUser.email,
        firstName: googleUser.firstName,
        lastName: googleUser.lastName,
        idToken: googleUser.idToken
    }).subscribe({
        next: (response: any) => {
            const decoded = this.decodeToken(response.accessToken);
            const user: User = {
                email: decoded.unique_name,
                role: decoded.role,
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
                firstName: googleUser.firstName,
                lastName: googleUser.lastName,
                imageUrl: googleUser.photoUrl
            };
            
            localStorage.setItem('currentUser', JSON.stringify(user));
            this.currentUserSubject.next(user);
            this.router.navigate(['/dashboard']);
        },
        error: (err) => {
            console.error('Google login error:', err);
            this.logout();
        }
    });
}


public get userId(): string | null {
  const user = this.currentUserSubject.value;
  return user ? this.getUserIdFromToken(user.accessToken) : null;
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
          role: decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],     
               accessToken: response.accessToken,
          refreshToken: response.refreshToken
        };
        localStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUserSubject.next(user);
        return user;
      })
    );
  }



   checkEmailVerificationStatus():
   
  Observable<boolean> {
    return this.http.get<{isEmailVerified: boolean}>(`https://localhost:7228/api/Profile/user/email-verification-status`).pipe(
      map(response => response.isEmailVerified),
      catchError(error => {
        console.error('Error checking email verification status:', error);
        return of(false); // Return false in case of error
      })
    );
  }



  register(
    email: string, firstName: string, lastName: string,  password: string,role: string
  ) {
    return this.http.post(`${this.baseUrl}register`, {  
      email,
      firstName,
      lastName,
      password,role });
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
  getCurrentUserId(): number {
    if (!this.currentUser) {
      throw new Error('No user is currently logged in');
    }
    return Number(this.userId) || 0;
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
