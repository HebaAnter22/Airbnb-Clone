import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgIf } from '@angular/common';
import { GoogleSigninButtonModule } from '@abacritt/angularx-social-login';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';
import { ValidationErrorComponent } from '../../common/validation-error/validation-error.component';
import { InputValidators } from '../../../validators/input-validators';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule,
    NgIf,
    GoogleSigninButtonModule,
    MainNavbarComponent,
    ValidationErrorComponent
  ],
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
      email: ['', [Validators.required, InputValidators.emailFormat()]],
      firstName: ['', [Validators.required, InputValidators.textLength(2, 50), InputValidators.lettersOnly()]],
      lastName: ['', [Validators.required, InputValidators.textLength(2, 50), InputValidators.lettersOnly()]],
      password: ['', [
        Validators.required,
        InputValidators.textLength(8, 128),
        InputValidators.strongPassword()
      ]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: InputValidators.passwordMatch('password', 'confirmPassword')
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
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
            //return BadRequest("User already exists");       
            console.log(err)
            if (err.status == 409) {
              this.errorMessage = 'User already exists. Please use a different email.';
            }
            else {
              this.errorMessage = 'An error occurred. Please try again later.';
            }

          }
        });
    } else {
      this.markFormGroupTouched(this.registerForm);
    }
  }

  // Helper method to mark all form controls as touched to trigger validation display
  markFormGroupTouched(formGroup: FormGroup) {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  signInWithGoogle(): void {
    this.authService.signInWithGoogle();
  }
}