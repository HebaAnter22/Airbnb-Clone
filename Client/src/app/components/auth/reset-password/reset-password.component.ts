// src/app/components/auth/reset-password/reset-password.component.ts
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';
import { FirebaseAuthService } from '../../../services/firebaseAuth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MainNavbarComponent],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  successMessage: string = '';
  errorMessage: string = '';
  isSubmitting: boolean = false;
  actionCode: string = '';
  email: string = '';
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;
  codeVerified: boolean = false;

  constructor(
    private fb: FormBuilder,
    private firebaseAuthService: FirebaseAuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.resetPasswordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validator: this.passwordMatchValidator });
  }

  ngOnInit() {
    // Get action code from URL
    this.route.queryParams.subscribe(params => {
      this.actionCode = params['oobCode'] || '';
      const mode = params['mode'] || '';

      if (!this.actionCode) {
        this.errorMessage = 'Invalid password reset link. No action code found.';
        return;
      }

      if (mode !== 'resetPassword') {
        this.errorMessage = 'Invalid action mode. Expected "resetPassword".';
        return;
      }

      // Verify the action code
      this.firebaseAuthService.verifyPasswordResetCode(this.actionCode)
        .then(email => {
          // Store the email for the UI and mark code as verified
          this.email = email;
          this.codeVerified = true;
          console.log('Reset code verified for email:', email);
        })
        .catch(error => {
          console.error('Error verifying reset code:', error);
          this.errorMessage = 'Invalid or expired password reset link. Please request a new one.';
        });
    });
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('password')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (password !== confirmPassword) {
      formGroup.get('confirmPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      formGroup.get('confirmPassword')?.setErrors(null);
      return null;
    }
  }

  togglePasswordVisibility(field: string): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else if (field === 'confirmPassword') {
      this.showConfirmPassword = !this.showConfirmPassword;
    }
  }

  onSubmit() {
    if (this.resetPasswordForm.valid && this.actionCode && this.codeVerified) {
      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';

      const newPassword = this.resetPasswordForm.get('password')?.value;

      this.firebaseAuthService.confirmPasswordReset(this.actionCode, newPassword)
        .then(success => {
          this.isSubmitting = false;
          if (success) {
            this.successMessage = 'Your password has been successfully reset.';
            this.resetPasswordForm.reset();

            // Redirect to login after a short delay
            setTimeout(() => {
              this.router.navigate(['/login']);
            }, 3000);
          } else {
            this.errorMessage = 'Failed to reset password. Please try again.';
          }
        })
        .catch(error => {
          this.isSubmitting = false;

          // Handle specific Firebase errors
          if (error.code === 'auth/expired-action-code') {
            this.errorMessage = 'The password reset link has expired. Please request a new one.';
          } else if (error.code === 'auth/invalid-action-code') {
            this.errorMessage = 'The password reset link is invalid. Please request a new one.';
          } else if (error.code === 'auth/weak-password') {
            this.errorMessage = 'Password is too weak. Please choose a stronger password.';
          } else {
            this.errorMessage = 'Error resetting password: ' + error.message;
          }
        });
    } else if (!this.codeVerified) {
      this.errorMessage = 'Password reset code has not been verified. Please check your email link.';
    }
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  requestNewLink() {
    this.router.navigate(['/forgot-password']);
  }
}