import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

// Interfaces for request and response DTOs
export interface AIRequestDTO {
  Query: string;
  RequestType: 'chat' | 'property' | 'booking' | 'availability';
}

export interface AIResponseDTO {
  response: string;
  success: boolean;
  errorMessage?: string;
  timestamp: string;
  requestId: string;
}

@Injectable({
  providedIn: 'root'
})
export class AIService {
  private readonly apiUrl = 'https://localhost:7228/api/ai'; // Matches AuthService baseUrl

  constructor(private http: HttpClient) {}

  /**
   * Sends an AI request to the backend and returns the response.
   * @param query The user's query string
   * @param requestType The type of AI request (chat, property, booking, availability)
   * @returns Observable<AIResponseDTO> containing the AI response or an error
   */
  sendAIRequest(
    query: string,
    requestType: AIRequestDTO['RequestType']
  ): Observable<AIResponseDTO> {
    if (!query.trim()) {
      return throwError(() => new Error('Query cannot be empty'));
    }

    const payload: AIRequestDTO = {
      Query: query,
      RequestType: requestType
    };

    return this.http.post<AIResponseDTO>(this.apiUrl, payload).pipe(
      map(response => {
        console.log('Raw API response:', response); // Log for debugging
        if (!response || typeof response.success === 'undefined') {
          throw new Error('Invalid response format from server');
        }
        if (!response.success) {
          throw new Error(response.errorMessage || 'AI request failed: No error message provided');
        }
        return response;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Sends a general chat request.
   * @param query The user's query
   * @returns Observable<AIResponseDTO>
   */
  getChatResponse(query: string): Observable<AIResponseDTO> {
    return this.sendAIRequest(query, 'chat');
  }

  /**
   * Sends a property recommendation request.
   * @param query The user's query
   * @returns Observable<AIResponseDTO>
   */
  getPropertyRecommendations(query: string): Observable<AIResponseDTO> {
    return this.sendAIRequest(query, 'property');
  }

  /**
   * Sends a booking assistance request.
   * @param query The user's query
   * @returns Observable<AIResponseDTO>
   */
  getBookingAssistance(query: string): Observable<AIResponseDTO> {
    return this.sendAIRequest(query, 'booking');
  }

  /**
   * Sends an availability insights request.
   * @param query The user's query
   * @returns Observable<AIResponseDTO>
   */
  getAvailabilityInsights(query: string): Observable<AIResponseDTO> {
    return this.sendAIRequest(query, 'availability');
  }

  /**
   * Handles HTTP errors and returns a user-friendly error message.
   * @param error The HTTP error response
   * @returns Observable that throws an error
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    console.log('Full error object:', error); // Log full error for debugging
    let errorMessage = 'An error occurred. Please try again later.';

    if (error.error instanceof ErrorEvent) {
      // Client-side error (e.g., network failure)
      errorMessage = `Client error: ${error.error.message}`;
    } else {
      // Server-side error
      const status = error.status || 'Unknown';
      if (error.status === 400 && error.error?.error) {
        errorMessage = error.error.error; // e.g., "User input cannot be empty."
      } else if (error.error?.errorMessage) {
        errorMessage = error.error.errorMessage; // AIResponseDTO error
      } else if (error.status === 401) {
        errorMessage = 'Unauthorized: Please log in again.';
      } else if (error.status === 403) {
        errorMessage = 'Forbidden: You do not have permission to access this resource.';
      } else if (error.status === 0) {
        errorMessage = 'Network error: Unable to reach the server. Please check your connection.';
      } else {
        errorMessage = `Server error: ${status} - ${error.message || 'Unknown error'}`;
      }
    }

    console.error('AI Service Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}