import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgIf } from '@angular/common';
import { GoogleSigninButtonModule } from '@abacritt/angularx-social-login';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, NgIf,GoogleSigninButtonModule,MainNavbarComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  registerForm: FormGroup;
  errorMessage: string = '';
  showPassword: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
     // role: ['Guest', Validators.required],  // Default to 'Guest'
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }



  passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    return password && confirmPassword && password.value !== confirmPassword.value 
      ? { passwordMismatch: true } 
      : null;
  };
  
togglePasswordVisibility(): void {
  this.showPassword = !this.showPassword;
}

  goToLogin() {
    this.router.navigate(['/login']);
  }

  signInWithGoogle(): void {
    this.authService.signInWithGoogle();
  }

  onSubmit() {
    if (this.registerForm.valid) {
      const { email, firstName, lastName, password } = this.registerForm.value;
      this.authService.register(email, firstName, lastName, password)
        .subscribe({
          next: () => {
            this.router.navigate(['/login']);
          },
          error: (err) => {
            this.errorMessage = err.error || 'Registration failed';
          }
        });
    }
  }
}