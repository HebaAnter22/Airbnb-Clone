import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { environment } from '../../../environments/environment';
import { Stripe, StripeElements, loadStripe } from '@stripe/stripe-js';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-checkout',
    templateUrl: './checkout.component.html',
    styleUrls: ['./checkout.component.css'],
    standalone: true,
    imports: [CommonModule]
})
export class CheckoutComponent implements OnInit {
    bookingId: number;
    amount: number = 74; // Example amount
    stripe: Stripe | null = null;
    elements: StripeElements | null | undefined = null; // Allow undefined
    paymentIntentId: string | null = null;
    paymentStatus: string | null = null;
    errorMessage: string | null = null;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private paymentService: PaymentService
    ) {
        this.bookingId = +this.route.snapshot.paramMap.get('id')! || 21; // Default to 21 for testing
    }

    async ngOnInit() {
        // Initialize Stripe
        const stripe = await loadStripe(environment.stripePublicKey);
        this.stripe = stripe;
        this.elements = stripe?.elements(); // Can be StripeElements | undefined

        // Create a card element (for Payment Intent flow)
        if (this.elements) {
            const cardElement = this.elements.create('card');
            cardElement.mount('#card-element');
        }
    }

    async processPayment() {
        this.errorMessage = null;
        this.paymentStatus = null;

        try {
            // Step 1: Create Payment Intent
            const paymentIntentResponse = await this.paymentService.createPaymentIntent(this.amount, this.bookingId).toPromise();
            this.paymentIntentId = paymentIntentResponse.paymentIntentId;
            const clientSecret = paymentIntentResponse.clientSecret;

            // Step 2: Confirm Payment with Stripe.js
            if (this.stripe && this.elements) {
                const cardElement = this.elements.getElement('card');
                const { paymentIntent, error } = await this.stripe.confirmCardPayment(clientSecret, {
                    payment_method: {
                        card: cardElement!,
                    },
                });

                if (error) {
                    this.errorMessage = error.message || 'Payment failed';
                    return;
                }

                if (paymentIntent && paymentIntent.status === 'succeeded') {
                    // Step 3: Confirm Payment on Backend
                    await this.paymentService.confirmPayment(
                        this.bookingId,
                        this.paymentIntentId!,
                        'pm_card_visa' // In a real app, get this from Stripe.js
                    ).toPromise();

                    // Step 4: Fetch Payment Status
                    const paymentDetails = await this.paymentService.getPaymentStatus(this.paymentIntentId!).toPromise();
                    this.paymentStatus = paymentDetails.status;
                }
            } else {
                this.errorMessage = 'Stripe or card element not initialized';
            }
        } catch (error) {
            this.errorMessage = 'An error occurred during payment';
        }
    }

    async redirectToCheckout() {
        try {
            const sessionResponse = await this.paymentService.createCheckoutSession(this.amount, this.bookingId).toPromise();
            await this.paymentService.redirectToCheckout(sessionResponse.sessionId);
        } catch (error) {
            this.errorMessage =  'Error redirecting to Stripe Checkout';
        }
    }
}