import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgIf } from '@angular/common';
import { GoogleSigninButtonModule, SocialAuthService } from '@abacritt/angularx-social-login';
import { NavbarComponent } from '../../home/navbar/navbar.component';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, NgIf, GoogleSigninButtonModule, MainNavbarComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  errorMessage: string = '';

  showPassword: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private socialAuthService: SocialAuthService
  ) {
    this.loginForm = this.fb.group({
      email: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  signInWithGoogle(): void {
    this.authService.signInWithGoogle();
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      this.authService.login(email, password)
        .subscribe({
          next: () => {
            this.authService.navigateBasedOnRole();
          },
          error: (err) => {
            console.error('Login error:', err); // Log the full error for debugging

            if (err.status === 0) {
              this.errorMessage = 'Unable to connect to server. Please check your connection.';
            } else if (err.status === 401) {
              this.errorMessage = 'Invalid email or password.';
            } else if (err.error && typeof err.error === 'string') {
              this.errorMessage = err.error;
            } else if (err.error && err.error.message) {
              this.errorMessage = err.error.message;
            } else if (err.message) {
              this.errorMessage = err.message;
            } else {
              this.errorMessage = 'Login failed. Please try again.';
            }
          }
        });
    }
  }
}