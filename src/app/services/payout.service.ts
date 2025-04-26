import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PayoutService {
  private apiUrl = `${environment.apiUrl}/api/payout`;

  constructor(private http: HttpClient) { }

  // Get host balance
  getHostBalance(hostId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/host/balance/${hostId}`);
  }

  // Create Stripe Connect account
  createStripeConnectAccount(hostId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/stripe/connect`, { hostId });
  }

  // Get Stripe Connect account link
  getStripeConnectAccountLink(hostId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stripe/connect/link?hostId=${hostId}`);
  }

  // Check Stripe account status
  checkStripeAccountStatus(hostId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stripe/account/status?hostId=${hostId}`);
  }

  // Request a payout
  requestPayout(hostId: number, amount: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/request`, { hostId, amount });
  }

  // Get host payouts
  getHostPayouts(hostId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/host/${hostId}`);
  }

  // Get payout details
  getPayoutDetails(payoutId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${payoutId}`);
  }
} 