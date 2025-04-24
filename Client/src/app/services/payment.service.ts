import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { PaymentIntentResponse } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl; // URL الخاص بـ API

  // إدارة الحالة بالإشارات (Signals)
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  async createPaymentIntent(bookingId: number, amount: number): Promise<PaymentIntentResponse> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const response = await this.http.post<PaymentIntentResponse>(
        `${this.apiUrl}/BookingPayment/create-intent`,
        { bookingId, amount }
      ).toPromise();

      if (!response) throw new Error('لا يوجد استجابة من الخادم');
      return response;

    } catch (error: any) {
      this.errorMessage.set(error.message || 'حدث خطأ أثناء الدفع');
      throw error;
    } finally {
      this.isLoading.set(false);
    }
  }
}