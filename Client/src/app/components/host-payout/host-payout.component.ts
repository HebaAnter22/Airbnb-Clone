import { Component, OnInit } from '@angular/core';
import { PayoutService } from '../../services/payout.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-host-payout',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NgClass
  ],
  templateUrl: './host-payout.component.html',
  styleUrls: ['./host-payout.component.scss']
})
export class HostPayoutComponent implements OnInit {
  hostId: number = 2; // Replace with actual host ID from auth service
  availableBalance: number = 0;
  payouts: any[] = [];
  payoutAmount: number = 0;
  isStripeConnected: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private payoutService: PayoutService
  ) {}

  ngOnInit(): void {
    this.loadHostData();
  }

  loadHostData(): void {
    this.isLoading = true;
    
    // Check if host has Stripe account setup
    this.payoutService.checkStripeAccountStatus(this.hostId).subscribe({
      next: (response) => {
        this.isStripeConnected = response.isReady;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = 'Could not check Stripe account status';
        this.isLoading = false;
      }
    });

    // Get host balance
    this.payoutService.getHostBalance(this.hostId).subscribe({
      next: (response) => {
        this.availableBalance = response.availableBalance;
      },
      error: (error) => {
        this.errorMessage = 'Could not retrieve balance';
      }
    });

    // Get host payout history
    this.payoutService.getHostPayouts(this.hostId).subscribe({
      next: (response) => {
        this.payouts = response;
      },
      error: (error) => {
        this.errorMessage = 'Could not retrieve payout history';
      }
    });
  }

  setupStripeAccount(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    // First create a Stripe Connect account
    this.payoutService.createStripeConnectAccount(this.hostId).subscribe({
      next: (response) => {
        // Then get the onboarding link
        this.payoutService.getStripeConnectAccountLink(this.hostId).subscribe({
          next: (linkResponse) => {
            this.isLoading = false;
            // Redirect to Stripe's onboarding page
            window.location.href = linkResponse.link;
          },
          error: (error) => {
            this.isLoading = false;
            this.errorMessage = 'Could not get Stripe onboarding link';
          }
        });
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Could not create Stripe account';
      }
    });
  }

  requestPayout(): void {
    if (this.payoutAmount <= 0) {
      this.errorMessage = 'Please enter a valid amount';
      return;
    }

    if (this.payoutAmount > this.availableBalance) {
      this.errorMessage = 'Insufficient balance';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.payoutService.requestPayout(this.hostId, this.payoutAmount).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = 'Payout request submitted successfully';
        this.payoutAmount = 0;
        this.loadHostData(); // Refresh data
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error || 'Failed to request payout';
      }
    });
  }
}