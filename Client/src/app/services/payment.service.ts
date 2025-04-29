import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { Stripe, loadStripe } from '@stripe/stripe-js';

export interface AdminRefundDto {
    paymentId: number;
    refundAmount: number;
    violationId: number;
    reason?: string;
    adminNotes?: string;
}

@Injectable({
    providedIn: 'root'
})
export class PaymentService {
    private apiUrl = environment.apiUrl;
    private stripePromise: Promise<Stripe | null>;

    constructor(private http: HttpClient) {
        this.stripePromise = loadStripe(environment.stripePublicKey);
    }

    createPaymentIntent(amount: number, bookingId: number): Observable<any> {
        const url = `${this.apiUrl}/BookingPayment/create-payment-intent`;
        const token = localStorage.getItem('token');
        const headers = new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        });

        return this.http.post(url, { amount, bookingId }, { headers }).pipe(
            tap(response => console.log('Create Payment Intent Response:', response)),
            catchError(error => {
                console.error('Error creating payment intent:', error);
                return throwError(error);
            })
        );
    }

    confirmPayment(bookingId: number, paymentIntentId: string, paymentMethodId: string): Observable<any> {
        const url = `${this.apiUrl}/BookingPayment/confirm-booking-payment`;
        const token = localStorage.getItem('token');
        const headers = new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        });

        return new Observable(observer => {
            // Implementation of client-side retry with exponential backoff
            const maxRetries = 3;
            let retryCount = 0;
            let lastError: any = null;
            
            const retryRequest = () => {
                // Start with a longer initial delay (1 second)
                const delay = Math.pow(2, retryCount) * 1000; // 1000ms, 2000ms, 4000ms
                
                console.log(`Payment confirmation attempt ${retryCount + 1}/${maxRetries + 1}${retryCount > 0 ? ` (retry after ${delay}ms)` : ''}`);
                
                // Wait before making the request (even on first attempt)
                setTimeout(() => {
                    this.http.post(url, { bookingId, paymentIntentId, paymentMethodId }, { headers })
                        .subscribe({
                            next: (response) => {
                                console.log('Payment confirmation successful:', response);
                                observer.next(response);
                                observer.complete();
                            },
                            error: (error) => {
                                console.error(`Payment confirmation attempt ${retryCount + 1} failed:`, error);
                                lastError = error;
                                
                                const isRetryableError = 
                                    // Include all 4xx errors except 401/403 as retryable
                                    (error.status >= 400 && error.status < 500 && error.status !== 401 && error.status !== 403) || 
                                    // Check for specific error messages
                                    (error?.error?.message && (
                                        error.error.message.includes('second operation was started') ||
                                        error.error.message.includes('busy') || 
                                        error.error.message.includes('try again')
                                    ));
                                
                                if (retryCount < maxRetries && isRetryableError) {
                                    retryCount++;
                                    console.log(`Will retry payment confirmation (attempt ${retryCount + 1}/${maxRetries + 1})`);
                                    retryRequest(); // Recursive retry
                                } else {
                                    // Out of retries or non-retryable error
                                    if (retryCount === maxRetries) {
                                        console.error('Maximum retry attempts reached for payment confirmation');
                                    }
                                    observer.error(lastError);
                                }
                            }
                        });
                }, retryCount === 0 ? 0 : delay); // No delay for first attempt
            };
            
            // Start the retry process
            retryRequest();
        });
    }

    getPaymentStatus(transactionId: string): Observable<any> {
        const url = `${this.apiUrl}/BookingPayment/GetPaymentByTransactionId/${transactionId}`;
        const token = localStorage.getItem('token');
        const headers = new HttpHeaders({
            'Authorization': `Bearer ${token}`
        });

        return this.http.get(url, { headers }).pipe(
            tap(response => console.log('Payment Status Response:', response)),
            catchError(error => {
                console.error('Error fetching payment status:', error);
                return throwError(error);
            })
        );
    }

    createCheckoutSession(amount: number, bookingId: number): Observable<any> {
        const url = `${this.apiUrl}/BookingPayment/create-checkout-session`;
        const token = localStorage.getItem('token');
        const headers = new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        });

        return this.http.post(url, { amount, bookingId }, { headers }).pipe(
            tap(response => console.log('Create Checkout Session Response:', response)),
            catchError(error => {
                console.error('Error creating checkout session:', error);
                return throwError(error);
            })
        );
    }


    getBookingDetails(bookingId: number): Observable<any> {
      const url = `https://localhost:7228/api/Booking/${bookingId}/details`; // Matches the working endpoint
      const token = localStorage.getItem('token');
      const headers = new HttpHeaders({
        'Authorization': `Bearer ${token}`
      });
  
      return this.http.get(url, { headers }).pipe(
        tap(response => console.log('Booking Details Response:', response)),
        catchError(error => {
          console.error('Error fetching booking details:', error);
          return throwError(error);
        })
      );
    }
    async redirectToCheckout(sessionId: string): Promise<void> {
        const stripe = await this.stripePromise;
        if (!stripe) {
            throw new Error('Stripe.js not loaded');
        }
        const { error } = await stripe.redirectToCheckout({ sessionId });
        if (error) {
            console.error('Error redirecting to Checkout:', error);
            throw error;
        }
    }

    processAdminRefund(refundData: AdminRefundDto): Observable<any> {
        const url = `${this.apiUrl}/BookingPayment/admin-refund`;
        const token = localStorage.getItem('token');
        const headers = new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        });

        return this.http.post(url, refundData, { headers }).pipe(
            tap(response => console.log('Admin Refund Response:', response)),
            catchError(error => {
                console.error('Error processing admin refund:', error);
                return throwError(error);
            })
        );
    }
}