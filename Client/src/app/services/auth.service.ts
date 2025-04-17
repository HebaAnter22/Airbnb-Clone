import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = 'https://localhost:7228/api';
  private currentUserId: number | null = null;

  constructor(private http: HttpClient) {
    // Load user ID from token or local storage on service initialization
    this.loadCurrentUser();
  }

  private loadCurrentUser(): void {
    // For now, we'll use a hardcoded user ID for testing
    // TODO: Implement proper user authentication and ID retrieval
    this.currentUserId = 1;
  }

  getCurrentUserId(): number {
    if (!this.currentUserId) {
      throw new Error('No user is currently logged in');
    }
    return this.currentUserId;
  }

  // Add other auth-related methods here
} 