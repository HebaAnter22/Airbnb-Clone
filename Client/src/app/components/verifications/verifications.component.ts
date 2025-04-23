import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FirebaseAuthService } from '../../services/firebaseAuth.service';
import { provideFirebaseApp } from '@angular/fire/app';
import { initializeApp } from 'firebase/app';
import { environment } from '../../../environments/environment';
import { getAuth } from 'firebase/auth';
import { provideAuth } from '@angular/fire/auth';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-verification',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './verifications.component.html',
  styleUrls: ['./verifications.component.scss']
})
export class VerificationComponent implements OnInit, OnDestroy {
  currentStep: 'overview' | 'email' | 'phone' | 'id' | 'email-sent' | 'phone-verify' = 'overview';
  email: string = '';
  password: string = ''; // Temporary password for new users
  phoneNumber: string = '';
  countryCode: string = '+1';
  verificationCode: string = '';
  idFrontImage: File | null = null;
  idBackImage: File | null = null;
  idFrontPreviewUrl: string | null = null; // New property for front image preview
  idBackPreviewUrl: string | null = null;  // New property for back image preview
  
  emailVerified: boolean = false;
  phoneVerified: boolean = false;
  idVerified: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  userId: string = ''; // User ID from Firebase Auth
  apiUrl: string = environment.apiUrl; // Base URL for your backend API
  private userSubscription: Subscription | null = null;
  
  // This would be filled with country codes in a real implementation
  countryCodes = [
    { code: '+1', name: 'United States (+1)' },
    { code: '+44', name: 'United Kingdom (+44)' },
    { code: '+33', name: 'France (+33)' },
    // Add more country codes as needed
  ];

  constructor(
    private authService: FirebaseAuthService,
    private http: HttpClient,
    private auth: AuthService,
    private router:Router
  ) {}

  ngOnInit(): void {
    // Check email verification status from your backend API
    this.checkEmailVerificationStatus();
    this.userId = this.auth.userId || ''; // Get the user ID from your auth service
  }

  ngOnDestroy(): void {
    // Clean up subscription
    if (this.userSubscription) {
      this.userSubscription.unsubscribe();
    }
    
    // Clean up image preview URLs
    if (this.idFrontPreviewUrl) {
      URL.revokeObjectURL(this.idFrontPreviewUrl);
    }
    if (this.idBackPreviewUrl) {
      URL.revokeObjectURL(this.idBackPreviewUrl);
    }
  }

  navigateTo(step: 'overview' | 'email' | 'phone' | 'id' | 'email-sent'): void {
    this.currentStep = step;
    this.errorMessage = '';
  }
  
  goToProfilePage(): void {
    this.router.navigate(['/profile', this.userId]); // Navigate to the profile page with the user ID
  }
  
  async verifyEmail(): Promise<void> {
    if (!this.email || !this.email.includes('@')) {
      this.errorMessage = 'Please enter a valid email address';
      return;
    }
    
    this.isLoading = true;
    this.errorMessage = '';
    
    try {
      this.password = 'Temp' + Math.random().toString(36).substring(2, 10);
      
      // First, update the email in your backend database
      await this.updateUserEmail(this.email);
      
      // Then, send the verification email through Firebase
      const success = await this.authService.sendVerificationEmail(this.email, this.password);
      
      if (success) {
        this.navigateTo('email-sent');
      } else {
        this.errorMessage = 'Failed to send verification email. Please try again.';
      }
    } catch (error) {
      
      console.error('Error during email verification:', error);
      if(this.errorMessage === '') {
      this.errorMessage = 'An error occurred. Please try again later.';
      }
    } finally {
      this.isLoading = false;
    }
  }
  
  async resendVerificationEmail(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';
    
    try {
      const success = await this.authService.resendVerificationEmail();
      
      if (!success) {
        this.errorMessage = 'Failed to resend verification email. Please try again.';
      }
    } catch (error) {
      console.error('Error resending verification email:', error);
      this.errorMessage = 'An error occurred. Please try again later.';
    } finally {
      this.isLoading = false;
    }
  }
  
  checkEmailVerification(): void {
    // Check if email has been verified from your backend API
    this.isLoading = true;
    
    // First, have Firebase reload the user to check verification status
    this.authService.getCurrentUser()?.reload()
      .then(() => {
        // If Firebase confirms the email is verified, update your backend
        if (this.authService.isEmailVerified()) {
          // Update the verification status in your backend
          this.updateEmailVerificationStatus(true)
            .then(() => {
              this.emailVerified = true;
              this.navigateTo('overview');
            })
            .catch(error => {
              console.error('Error updating email verification status:', error);
              this.errorMessage = 'Failed to update verification status. Please try again.';
            });
        } else {
          this.errorMessage = 'Email not yet verified. Please check your inbox and click the verification link.';
        }
      })
      .catch(error => {
        console.error('Error checking email verification:', error);
        this.errorMessage = 'Failed to check verification status. Please try again.';
      })
      .finally(() => {
        this.isLoading = false;
      });
  }
  
  // Check email verification status from your backend API
  private checkEmailVerificationStatus(): void {
    this.isLoading = true;
    
    this.http.get<{isEmailVerified: boolean}>(`${this.apiUrl}/Profile/user/email-verification-status`)
      .subscribe({
        next: (response) => {
          this.emailVerified = response.isEmailVerified;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error checking email verification status:', error);
          this.isLoading = false;
        }
      });
  }
  
  
  // Update the email in your backend database
  private updateUserEmail(email: string): Promise<void> {
    return new Promise((resolve, reject) => {
      this.http.put(`${this.apiUrl}/Profile/user/update-email`, { NewEmail: email })
        .subscribe({
          next: () => resolve(),
          error: (error) => {
            if (error.status === 400 && error.error === "Email already exists") {
              this.errorMessage = "This email is already registered. Please use a different email.";
            } else {
              console.error('Error updating email:', error);
              this.errorMessage = 'An error occurred while updating your email. Please try again.';
            }
            reject(error);
          }
        });
    });
  }
  
  // Update the email verification status in your backend database
  private updateEmailVerificationStatus(isVerified: boolean): Promise<void> {
    console.log(isVerified)
    return new Promise((resolve, reject) => {
      this.http.put(`${this.apiUrl}/Profile/user/verify-email`, { isVerified })
        .subscribe({
          next: () => resolve(),
          error: (error) => {
            console.error('Error updating email verification status:', error);
            reject(error);
          }
        });
    });
  }
  
  requestPhoneVerification(): void {
    // Mock SMS verification code request
    // In a real app, you would integrate with Firebase Phone Auth or another SMS service
    if (this.phoneNumber) {
      this.isLoading = true;
      
      // Simulate API call
      setTimeout(() => {
        this.isLoading = false;
        this.currentStep = 'phone-verify';
      }, 1000);
    } else {
      this.errorMessage = 'Please enter a valid phone number';
    }
  }
  
  submitVerificationCode(): void {
    // Mock verification code validation
    if (this.verificationCode && this.verificationCode.length === 4) {
      this.isLoading = true;
      
      // Simulate API call
      setTimeout(() => {
        this.isLoading = false;
        this.phoneVerified = true;
        this.navigateTo('overview');
      }, 1000);
    } else {
      this.errorMessage = 'Please enter a valid 4-digit verification code';
    }
  }
  
  onIdFrontSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.idFrontImage = input.files[0];
      
      // Create preview URL for the front image
      if (this.idFrontPreviewUrl) {
        URL.revokeObjectURL(this.idFrontPreviewUrl);
      }
      this.idFrontPreviewUrl = URL.createObjectURL(this.idFrontImage);
    }
  }
  
  onIdBackSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.idBackImage = input.files[0];
      
      // Create preview URL for the back image
      if (this.idBackPreviewUrl) {
        URL.revokeObjectURL(this.idBackPreviewUrl);
      }
      this.idBackPreviewUrl = URL.createObjectURL(this.idBackImage);
    }
  }
  
  submitIdVerification(): void {
    if (this.idFrontImage && this.idBackImage) {
      this.isLoading = true;
      this.errorMessage = '';
      
      // Create a FormData object to send the files
      const formData = new FormData();
      formData.append('frontImage', this.idFrontImage);
      formData.append('backImage', this.idBackImage);
      
      // Make the POST request to the host verification endpoint
      this.http.post(`${this.apiUrl}/HostVerification/CreateVerification`, formData)
        .subscribe({
          next: (response: any) => {
            console.log('ID verification submitted successfully:', response);
            this.idVerified = true;
            this.navigateTo('overview');
          },
          error: (error) => {
            console.error('Error submitting ID verification:', error);
            this.errorMessage = 'Failed to submit ID verification. Please try again.';
          },
          complete: () => {
            this.isLoading = false;
          }
        });
    } else {
      this.errorMessage = 'Please upload both front and back images of your ID';
    }
  }
  resendVerificationCode(): void {
    this.isLoading = true;
    
    // Simulate API call
    setTimeout(() => {
      this.isLoading = false;
    }, 1000);
  }
  
  requestCall(): void {
    this.isLoading = true;
    
    // Simulate API call
    setTimeout(() => {
      this.isLoading = false;
    }, 1000);
  }
}
