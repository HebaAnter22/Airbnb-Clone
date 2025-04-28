import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PaymentService } from '../../services/payment.service';

@Component({
    selector: 'app-payment-success',
    templateUrl: './payment-success.component.html',
    styleUrls: ['./payment-success.component.css']
})
export class PaymentSuccessComponent implements OnInit {
    paymentStatus: string | null = null;
    errorMessage: string | null = null;

    constructor(
        private route: ActivatedRoute,
        private paymentService: PaymentService
    ) {}

    ngOnInit() {
        // Get sessionId or paymentIntentId from query params
        const sessionId = this.route.snapshot.queryParamMap.get('sessionId');
        const paymentIntentId = this.route.snapshot.queryParamMap.get('paymentIntentId');

        if (paymentIntentId) {
            this.fetchPaymentStatus(paymentIntentId);
        } else if (sessionId) {
            // If you have an endpoint to get payment details by sessionId, use it
            this.fetchPaymentStatusBySession(sessionId);
        } else {
            this.errorMessage = 'No payment information provided';
        }
    }

    fetchPaymentStatus(transactionId: string) {
        this.paymentService.getPaymentStatus(transactionId).subscribe({
            next: (paymentDetails) => {
                this.paymentStatus = paymentDetails.status;
            },
            error: (error) => {
                this.errorMessage = error.message || 'Error fetching payment status';
            }
        });
    }

    fetchPaymentStatusBySession(sessionId: string) {
        // If needed, add a backend endpoint to get payment details by sessionId
        // For now, assume we have the paymentIntentId
        this.paymentService.getPaymentStatus(sessionId).subscribe({
            next: (paymentDetails) => {
                this.paymentStatus = paymentDetails.status;
            },
            error: (error) => {
                this.errorMessage = error.message || 'Error fetching payment status';
            }
        });
    }
}