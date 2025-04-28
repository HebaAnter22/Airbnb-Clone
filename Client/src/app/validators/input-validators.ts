import { AbstractControl, ValidatorFn, ValidationErrors } from '@angular/forms';

/**
 * Custom validators for use across the application
 */
export class InputValidators {
  /**
   * Validates text inputs with character length constraints
   * @param minLength Minimum length required
   * @param maxLength Maximum length allowed
   * @returns ValidatorFn
   */
  static textLength(minLength: number, maxLength: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }

      const length = control.value.length;
      
      if (length < minLength) {
        return { minlength: { requiredLength: minLength, actualLength: length } };
      }
      
      if (length > maxLength) {
        return { maxlength: { requiredLength: maxLength, actualLength: length } };
      }
      
      return null;
    };
  }

  /**
   * Validates that passwords match
   */
  static passwordMatch(passwordControl: string, confirmPasswordControl: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get(passwordControl);
      const confirmPassword = control.get(confirmPasswordControl);
      
      if (!password || !confirmPassword) {
        return null;
      }
      
      return password.value === confirmPassword.value ? null : { passwordMismatch: true };
    };
  }

  /**
   * Validates email format with standard pattern
   */
  static emailFormat(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
      return emailPattern.test(control.value) ? null : { emailFormat: true };
    };
  }

  /**
   * Validates phone number format
   */
  static phoneNumber(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      // Accept phone numbers with or without country code
      // Supports formats like: +1234567890, 1234567890, (123) 456-7890
      const phonePattern = /^(\+\d{1,3})?[-.\s]?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$/;
      return phonePattern.test(control.value) ? null : { phoneFormat: true };
    };
  }

  /**
   * Validates a number is within a specified range
   */
  static numberRange(min: number, max: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value && control.value !== 0) {
        return null; // Let required validator handle empty values
      }
      
      const num = Number(control.value);
      
      if (isNaN(num)) {
        return { notANumber: true };
      }
      
      if (num < min) {
        return { min: { required: min, actual: num } };
      }
      
      if (num > max) {
        return { max: { required: max, actual: num } };
      }
      
      return null;
    };
  }

  /**
   * Validates a date is within a valid range
   */
  static dateRange(minDate?: Date, maxDate?: Date): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      const date = new Date(control.value);
      
      if (isNaN(date.getTime())) {
        return { invalidDate: true };
      }
      
      if (minDate && date < minDate) {
        return { minDate: { required: minDate, actual: date } };
      }
      
      if (maxDate && date > maxDate) {
        return { maxDate: { required: maxDate, actual: date } };
      }
      
      return null;
    };
  }

  /**
   * Validates that a URL is in correct format
   */
  static urlFormat(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      try {
        new URL(control.value);
        return null;
      } catch {
        return { invalidUrl: true };
      }
    };
  }

  /**
   * Validates input consists of letters only
   */
  static lettersOnly(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      const pattern = /^[a-zA-Z\s]+$/;
      return pattern.test(control.value) ? null : { lettersOnly: true };
    };
  }

  /**
   * Validates input consists of alphanumeric characters only
   */
  static alphanumericOnly(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      const pattern = /^[a-zA-Z0-9]+$/;
      return pattern.test(control.value) ? null : { alphanumericOnly: true };
    };
  }

  /**
   * Validates strong password requirements
   * - At least 8 characters
   * - Contains uppercase, lowercase, number, and special character
   */
  static strongPassword(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null; // Let required validator handle empty values
      }
      
      const errors: ValidationErrors = {};
      const value = control.value;
      
      // Check minimum length
      if (value.length < 8) {
        errors['minlength'] = { requiredLength: 8, actualLength: value.length };
      }
      
      // Check for uppercase letter
      if (!/[A-Z]/.test(value)) {
        errors['uppercaseRequired'] = true;
      }
      
      // Check for lowercase letter
      if (!/[a-z]/.test(value)) {
        errors['lowercaseRequired'] = true;
      }
      
      // Check for number
      if (!/[0-9]/.test(value)) {
        errors['numberRequired'] = true;
      }
      
      // Check for special character
      if (!/[!@#$%^&*(),.?":{}|<>]/.test(value)) {
        errors['specialCharRequired'] = true;
      }
      
      return Object.keys(errors).length > 0 ? errors : null;
    };
  }
} 