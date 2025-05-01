// src/app/services/password-reset.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class PasswordResetService {
    private baseUrl = 'https://localhost:7228/api/Auth/';  // Updated to match controller route

    constructor(private http: HttpClient) { }

    forgotPassword(email: string): Observable<any> {
        return this.http.post(`${this.baseUrl}forgot-password`, { email });
    }

    validateResetToken(email: string, token: string): Observable<any> {
        return this.http.post(`${this.baseUrl}validate-token`, { email, token });
    }

    resetPassword(email: string, token: string, newPassword: string): Observable<any> {
        return this.http.post(`${this.baseUrl}reset-password`, {
            email,
            token,
            newPassword
        });
    }
}