import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ValidationErrors } from '@angular/forms';

@Component({
  selector: 'app-validation-error',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="errors" class="validation-errors">
      <div class="error-message" *ngIf="errors['required']">
        {{ fieldName || 'This field' }} is required.
      </div>
      <div class="error-message" *ngIf="errors['minlength']">
        {{ fieldName || 'This field' }} must be at least {{ errors['minlength'].requiredLength }} characters.
      </div>
      <div class="error-message" *ngIf="errors['maxlength']">
        {{ fieldName || 'This field' }} cannot exceed {{ errors['maxlength'].requiredLength }} characters.
      </div>
      <div class="error-message" *ngIf="errors['email'] || errors['emailFormat']">
        Please enter a valid email address.
      </div>
      <div class="error-message" *ngIf="errors['passwordMismatch']">
        Passwords do not match.
      </div>
      <div class="error-message" *ngIf="errors['phoneFormat']">
        Please enter a valid phone number.
      </div>
      <div class="error-message" *ngIf="errors['notANumber']">
        Please enter a valid number.
      </div>
      <div class="error-message" *ngIf="errors['min']">
        {{ fieldName || 'This field' }} must be at least {{ errors['min'].required }}.
      </div>
      <div class="error-message" *ngIf="errors['max']">
        {{ fieldName || 'This field' }} cannot exceed {{ errors['max'].required }}.
      </div>
      <div class="error-message" *ngIf="errors['invalidDate']">
        Please enter a valid date.
      </div>
      <div class="error-message" *ngIf="errors['minDate']">
        Date must be after {{ formatDate(errors['minDate'].required) }}.
      </div>
      <div class="error-message" *ngIf="errors['maxDate']">
        Date must be before {{ formatDate(errors['maxDate'].required) }}.
      </div>
      <div class="error-message" *ngIf="errors['invalidUrl']">
        Please enter a valid URL.
      </div>
      <div class="error-message" *ngIf="errors['lettersOnly']">
        {{ fieldName || 'This field' }} should contain only letters.
      </div>
      <!-- Strong password validation errors -->
      <div class="error-message" *ngIf="errors['uppercaseRequired']">
        Password must include at least one uppercase letter.
      </div>
      <div class="error-message" *ngIf="errors['lowercaseRequired']">
        Password must include at least one lowercase letter.
      </div>
      <div class="error-message" *ngIf="errors['numberRequired']">
        Password must include at least one number.
      </div>
      <div class="error-message" *ngIf="errors['specialCharRequired']">
        Password must include at least one special character.
      </div>
      <!-- Display custom error message if provided -->
      <div class="error-message" *ngIf="customError">
        {{ customError }}
      </div>
    </div>
  `,
  styles: [`
    .validation-errors {
      margin-top: 5px;
    }
    .error-message {
      color: #ff385c;
      font-size: 0.85rem;
      margin-top: 3px;
    }
  `]
})
export class ValidationErrorComponent {
  @Input() errors: ValidationErrors | null = null;
  @Input() fieldName: string = '';
  @Input() customError: string = '';

  formatDate(date: Date): string {
    return date ? new Date(date).toLocaleDateString() : '';
  }
} 