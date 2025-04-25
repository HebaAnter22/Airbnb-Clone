// checkout.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { loadStripe } from '@stripe/stripe-js';
import { PaymentService } from '../../services/payment.service';
import { environment } from '../../../environments/environment';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
})
export class CheckoutComponent implements OnInit {
  bookingId: number;
  amount: number = 0;
  paymentIntentId: string | null = null;
  paymentStatus: string | null = null;
  errorMessage: string | null = null;
  bookingDetails: any = null;

  private stripePromise = loadStripe(environment.stripePublicKey);
  card: any;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentService
  ) {
    const bookingIdParam = this.route.snapshot.paramMap.get('bookingId');
    console.log(bookingIdParam);
    this.bookingId = bookingIdParam ? +bookingIdParam : 0;
    console.log(this.bookingId);

    if (!this.bookingId || this.bookingId <= 0) {
      this.errorMessage = 'Invalid or missing booking ID';
      // this.router.navigate(['/']);
      return;
    }

    this.amount = 0;
  }

  async ngOnInit() {
    if (!this.bookingId || this.bookingId <= 0) {
      return;
    }

    // Fetch booking details
    this.paymentService.getBookingDetails(this.bookingId).subscribe({
      next: (details) => {
        // Map the response to the expected format
        this.bookingDetails = {
          bookingId: details.id,
          amount: details.totalAmount,
          propertyName: details.propertyTitle,
          propertyLocation: 'Not specified', // Location not provided in response; adjust if available
          checkInDate: details.startDate,
          checkOutDate: details.endDate
        };
        this.amount = this.bookingDetails.amount;
      },
      error: (error) => {
        this.errorMessage = error.message || 'Error fetching booking details';
      }
    });

    // Set up Stripe Elements
    const stripe = await this.stripePromise;
    if (!stripe) {
      this.errorMessage = 'Stripe.js not loaded';
      return;
    }

    const elements = stripe.elements();
    this.card = elements.create('card', {
      style: {
        base: {
          fontSize: '16px',
          color: '#32325d',
          fontFamily: 'Arial, sans-serif',
          '::placeholder': {
            color: '#aab7c4'
          }
        },
        invalid: {
          color: '#fa755a'
        }
      }
    });
    this.card.mount('#card-element');
  }

  async pay() {
    this.errorMessage = null;
    this.paymentStatus = null;

    try {
      const paymentIntentResponse = await this.paymentService.createPaymentIntent(this.amount, this.bookingId).toPromise();
      this.paymentIntentId = paymentIntentResponse.clientSecret.split('_secret_')[0];

      const stripe = await this.stripePromise;
      if (!stripe || !this.card) {
        throw new Error('Stripe.js not loaded or card element not initialized');
      }

      const { paymentIntent, error } = await stripe.confirmCardPayment(paymentIntentResponse.clientSecret, {
        payment_method: {
          card: this.card,
          billing_details: {
            name: (document.getElementById('cardholder-name') as HTMLInputElement).value
          }
        }
      });

      if (error) {
        throw new Error(error.message);
      }

      if (paymentIntent && paymentIntent.status === 'succeeded') {
        await this.paymentService.confirmPayment(this.bookingId, this.paymentIntentId!, 'pm_card_visa').toPromise();
        this.paymentStatus = 'Payment successful!';
      }
    } catch (error) {
      this.errorMessage = error instanceof Error ? error.message : 'Payment failed';
    }
  }
}