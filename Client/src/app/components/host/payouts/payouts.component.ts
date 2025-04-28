import { Component, OnInit } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface Payout {
  id: number;
  amount: number;
  status: string;
  createdAt: string;
  processedAt?: string;
}

@Component({
  selector: 'app-host-payouts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './payouts.component.html',
  styleUrls: ['./payouts.component.css']
})
export class PayoutsComponent implements OnInit {
    private readonly API_URL = 'https://localhost:7228/api';

  payouts: Payout[] = [];
  availableBalance: number = 0;
  stripeConnectUrl: string = '';
  isStripeConnected: boolean = false;
  payoutAmount: number = 0;
  hostId: number | null = null;
  error: string = '';
  success: string = '';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.hostId = this.authService.getCurrentUserId();
    if (this.hostId) {
      console.log(this.hostId);
      this.loadHostBalance();
      this.loadPayouts();
      this.checkStripeConnection();
    }
  }

  loadHostBalance() {
    if (!this.hostId) return;

    this.http.get<any>(`${this.API_URL}/Payout/host/balance/${this.hostId}`).subscribe({
      next: (response) => {
        this.availableBalance = response.availableBalance;
        console.log(this.availableBalance);
      },
      error: (error: HttpErrorResponse) => {
        this.handleError('Error loading balance', error);
      }
    });
  }

  loadPayouts() {
    if (!this.hostId) return;

    this.http.get<Payout[]>(`${this.API_URL}/Payout/host/${this.hostId}`).subscribe({
      next: (response) => {
        this.payouts = response;
      },
      error: (error: HttpErrorResponse) => {
        this.handleError('Error loading payouts', error);
      }
    });
  }

  checkStripeConnection() {
    if (!this.hostId) return;

    this.http.get<any>(`${this.API_URL}/Payout/stripe/connect/status/${this.hostId}`).subscribe({
      next: (response) => {
        this.isStripeConnected = response.isConnected;
        if (!this.isStripeConnected) {
          this.getStripeConnectLink();
        }
      },
      error: (error: HttpErrorResponse) => {
        this.handleError('Error checking Stripe connection', error);
        this.isStripeConnected = false;
      }
    });
  }

  getStripeConnectLink() {
    if (!this.hostId) return;

    this.http.get<any>(`${this.API_URL}/Payout/stripe/connect/link/${this.hostId}`).subscribe({
      next: (response) => {
        this.stripeConnectUrl = response.link;
      },
      error: (error: HttpErrorResponse) => {
        this.handleError('Error getting Stripe connect link', error);
      }
    });
  }

  setupStripeConnect() {
    if (!this.hostId || !this.stripeConnectUrl) return;
    window.location.href = this.stripeConnectUrl;
  }

  requestPayout() {
    if (this.payoutAmount <= 0 || !this.hostId) return;
    if (this.payoutAmount > this.availableBalance) {
      this.error = 'Payout amount cannot exceed available balance';
      return;
    }

    this.http.post<any>(`${this.API_URL}/Payout/request`, {
      hostId: this.hostId,
      amount: this.payoutAmount
    }).subscribe({
      next: (response) => {
        this.success = 'Payout request submitted successfully';
        this.loadPayouts();
        this.loadHostBalance();
        this.payoutAmount = 0;
      },
      error: (error: HttpErrorResponse) => {
        this.handleError('Error requesting payout', error);
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'bg-success';
      case 'processing':
        return 'bg-warning text-dark';
      case 'failed':
        return 'bg-danger';
      case 'pending':
        return 'bg-info';
      default:
        return 'bg-secondary';
    }
  }

  private handleError(message: string, error: HttpErrorResponse) {
    console.error(message, error);
    this.error = error.error?.message || error.message || 'An unexpected error occurred';
  }
} 