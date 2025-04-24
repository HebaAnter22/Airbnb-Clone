import { Component, inject, viewChild } from '@angular/core';
import { PaymentService } from '../../services/payment.service';
import { loadStripe } from '@stripe/stripe-js';
import { StripePaymentElementComponent } from 'ngx-stripe';
import { CommonModule, DatePipe } from '@angular/common';
import { environment } from '../../../environments/environment';
declare var Stripe: any;
@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [StripePaymentElementComponent, CommonModule],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent {
  paymentService = inject(PaymentService);
  paymentElement = viewChild.required(StripePaymentElementComponent);

  bookingDetails = {
    id: 1,
    propertyTitle: 'شقة فاخرة بوسط القاهرة',
    checkInDate: '2023-12-01',
    checkOutDate: '2023-12-04',
    totalPrice: 1500,
    propertyImage: 'https://a0.muscache.com/im/pictures/123456.jpg'
  };

  // إصلاح نوع options ليتوافق مع Stripe
  paymentOptions = {
    layout: {
      type: 'tabs' as const, // تحديد النوع كقيمة ثابتة
      defaultCollapsed: false
    }
  };

  async handlePayment() {
    try {
      const { clientSecret } = await this.paymentService.createPaymentIntent(
        this.bookingDetails.id,
        this.bookingDetails.totalPrice
      );

      const stripe = await loadStripe(environment.stripePublicKey);
      if (!stripe) throw new Error('تعذر تحميل نظام الدفع');

      const { error } = await stripe.confirmPayment({
        elements: this.paymentElement().elements,
        clientSecret,
        confirmParams: {
          return_url: `${window.location.origin}/booking/${this.bookingDetails.id}/success`,
        }
      });

      if (error) throw error;

    } catch (error) {
      console.error('فشل في عملية الدفع:', error);
    }
  }

  // دالة مساعدة لتنسيق التاريخ
  
}