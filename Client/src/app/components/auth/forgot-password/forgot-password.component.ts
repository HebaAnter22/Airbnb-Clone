// src/app/components/auth/forgot-password/forgot-password.component.ts
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';
import { FirebaseAuthService } from '../../../services/firebaseAuth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MainNavbarComponent],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent {
  forgotPasswordForm: FormGroup;
  successMessage: string = '';
  errorMessage: string = '';
  isSubmitting: boolean = false;

  constructor(
    private fb: FormBuilder,
    private firebaseAuthService: FirebaseAuthService,
    private router: Router
  ) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit() {
    if (this.forgotPasswordForm.valid) {
      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';

      const email = this.forgotPasswordForm.get('email')?.value;

      this.firebaseAuthService.sendPasswordResetEmail(email)
        .then(success => {
          this.isSubmitting = false;
          if (success) {
            this.successMessage = 'Password reset email sent! Check your inbox and follow the link to reset your password.';
            // Optional: Clear the form
            this.forgotPasswordForm.reset();
          } else {
            this.errorMessage = 'Failed to send password reset email. Please try again.';
          }
        })
        .catch(error => {
          this.isSubmitting = false;

          // Handle specific Firebase errors
          if (error.code === 'auth/user-not-found') {
            this.errorMessage = 'No account exists with this email address.';
          } else if (error.code === 'auth/invalid-email') {
            this.errorMessage = 'The email address is not valid.';
          } else {
            this.errorMessage = 'Error sending reset email: ' + error.message;
          }
        });
    }
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}