import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, NgIf],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  errorMessage: string = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.loginForm = this.fb.group({
      email: ['', Validators.required],
      password: ['', Validators.required]
    });
  }
  goToRegister() {
    this.router.navigate(['/register']);
  }
 
  onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      this.authService.login(email, password)
        .subscribe({
          next: () => {
            this.router.navigate(['/dashboard']);
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