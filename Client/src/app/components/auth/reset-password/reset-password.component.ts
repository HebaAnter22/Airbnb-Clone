// src/app/components/auth/reset-password/reset-password.component.ts
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PasswordResetService } from '../../../services/password-reset.service';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, MainNavbarComponent],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  email: string = '';
  token: string = '';
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;
  isLoading: boolean = true;
  isSubmitting: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  isTokenValid: boolean = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private passwordResetService: PasswordResetService
  ) {
    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit() {
    // Get the email and token from the URL
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      this.token = params['token'] || '';

      if (!this.email || !this.token) {
        this.errorMessage = 'Invalid password reset link';
        this.isLoading = false;
        return;
      }

      // Validate the token
      this.validateToken();
    });
  }

  validateToken() {
    this.passwordResetService.validateResetToken(this.email, this.token).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.isTokenValid = true;
        } else {
          this.errorMessage = 'Your password reset link is invalid or has expired.';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Your password reset link is invalid or has expired.';
      }
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('newPassword')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;

    if (password !== confirmPassword) {
      form.get('confirmPassword')?.setErrors({ mismatch: true });
      return { mismatch: true };
    } else {
      form.get('confirmPassword')?.setErrors(null);
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
    if (this.resetPasswordForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const newPassword = this.resetPasswordForm.get('newPassword')?.value;

    this.passwordResetService.resetPassword(this.email, this.token, newPassword).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.successMessage = 'Your password has been reset successfully.';
          this.resetPasswordForm.reset();
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 3000);
        } else {
          this.errorMessage = response.message;
        }
      },
      error: (error) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.message || 'Failed to reset password. Please try again.';
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}