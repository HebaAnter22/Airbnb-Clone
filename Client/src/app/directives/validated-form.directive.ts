import { Directive, Input, ElementRef, HostListener, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';

/**
 * Directive that automatically handles validation styling and error display
 * for reactive forms.
 * 
 * Usage:
 * <form [formGroup]="myForm" validatedForm>
 *   <input formControlName="fieldName">
 * </form>
 */
@Directive({
  selector: '[validatedForm]',
  standalone: true
})
export class ValidatedFormDirective implements OnInit {
  @Input() formGroup!: FormGroup;
  
  constructor(private el: ElementRef) { }
  
  ngOnInit() {
    // Find all form controls within the form
    const formControls = this.el.nativeElement.querySelectorAll('[formControlName]');
    
    // Add blur event listeners to all form controls
    formControls.forEach((control: HTMLElement) => {
      control.addEventListener('blur', () => {
        this.validateField(control);
      });
    });
  }
  
  /**
   * Listen to form submission to validate all fields
   */
  @HostListener('submit')
  onSubmit() {
    this.markAllAsTouched();
    this.validateAllFields();
  }
  
  /**
   * Mark all form controls as touched
   */
  private markAllAsTouched() {
    Object.keys(this.formGroup.controls).forEach(controlName => {
      const control = this.formGroup.get(controlName);
      control?.markAsTouched();
    });
  }
  
  /**
   * Validate all form fields
   */
  private validateAllFields() {
    const formControls = this.el.nativeElement.querySelectorAll('[formControlName]');
    formControls.forEach((control: HTMLElement) => {
      this.validateField(control);
    });
  }
  
  /**
   * Validate a specific field and apply appropriate styling
   */
  private validateField(control: HTMLElement) {
    const controlName = control.getAttribute('formControlName');
    if (!controlName) return;
    
    const formControl = this.formGroup.get(controlName);
    if (!formControl) return;
    
    // Apply is-invalid class if control is invalid and touched
    if (formControl.invalid && formControl.touched) {
      control.classList.add('is-invalid');
    } else {
      control.classList.remove('is-invalid');
      
      // Apply is-valid class if control is valid and touched
      if (formControl.valid && formControl.touched) {
        control.classList.add('is-valid');
      } else {
        control.classList.remove('is-valid');
      }
    }
  }
} 