import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CreatePropertyService } from '../../services/create-property.service';

@Component({
  selector: 'app-add-property',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatButtonModule, MatInputModule],
  templateUrl: './add-property.component.html',
  styleUrls: ['./add-property.component.css']
})
export class AddPropertyComponent {
  currentSection = 1;
  currentStep = 1;
  totalSteps = 7;
  propertyForm!: FormGroup;
  uploadedImages: string[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  showForm = false;
  showHero = true;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private propertyService: CreatePropertyService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
  }

  private initializeForm() {
    this.propertyForm = this.fb.group({
      categoryId: [1, Validators.required],
      title: ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(50)]],
      propertyType: ['', Validators.required],
      country: ['US', Validators.required],
      address: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', Validators.required],
      latitude: [null],
      longitude: [null],
      pricePerNight: [0, [Validators.required, Validators.min(1)]],
      cleaningFee: [0, [Validators.min(0)]],
      serviceFee: [0, [Validators.min(0)]],
      minNights: [1, [Validators.required, Validators.min(1)]],
      maxNights: [30, [Validators.required, Validators.min(1)]],
      bedrooms: [1, [Validators.required, Validators.min(1)]],
      bathrooms: [1, [Validators.required, Validators.min(1)]],
      maxGuests: [1, [Validators.required, Validators.min(1)]],
      currency: ['USD', Validators.required],
      instantBook: [false],
      cancellationPolicyId: [1, Validators.required],
      images: [[]]
    });
  }

  // Navigation methods
  nextStep() {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
      this.errorMessage = null;
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = null;
    }
  }

  nextSection() {
    if (this.currentSection < 10) {
      this.currentSection++;
      this.errorMessage = null;
    }
  }

  prevSection() {
    if (this.currentSection > 1) {
      this.currentSection--;
      this.errorMessage = null;
    }
  }

  // Form display control
  displayForm() {
    this.showForm = true;
    this.showHero = false;
    this.currentStep = 1;
    this.errorMessage = null;
  }

  // Image handling
  onImageUpload(event: Event) {
    this.errorMessage = null;
    const input = event.target as HTMLInputElement;
    
    if (!input.files || input.files.length === 0) {
      this.errorMessage = 'No files selected';
      return;
    }

    if (input.files.length > 10) {
      this.errorMessage = 'Maximum 10 images allowed';
      return;
    }

    for (let i = 0; i < input.files.length; i++) {
      const file = input.files[i];
      if (!file.type.match('image.*')) {
        this.errorMessage = 'Only image files are allowed';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result) {
          this.uploadedImages.push(e.target.result as string);
          this.propertyForm.patchValue({
            images: [...this.propertyForm.value.images, e.target.result]
          });
        }
      };
      reader.onerror = () => {
        this.errorMessage = 'Error reading file';
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(index: number) {
    this.uploadedImages.splice(index, 1);
    const currentImages = [...this.propertyForm.value.images];
    currentImages.splice(index, 1);
    this.propertyForm.patchValue({ images: currentImages });
  }

  // Form submission
  submitForm() {
    this.errorMessage = null;
    
    if (this.propertyForm.invalid) {
      this.markFormGroupTouched(this.propertyForm);
      this.errorMessage = 'Please fill all required fields correctly';
      this.snackBar.open(this.errorMessage || 'An error occurred', 'Close', { duration: 5000 });
      return;
    }

    if (this.uploadedImages.length < 1) {
      this.errorMessage = 'Please upload at least one image';
      this.snackBar.open(this.errorMessage || 'An error occurred', 'Close', { duration: 5000 });
      return;
    }

    this.isLoading = true;

    // Prepare the API request body
    const formValue = this.propertyForm.value;
    const requestBody = {
      ...formValue,
      // Convert numeric fields
      latitude: Number(formValue.latitude) || 0,
      longitude: Number(formValue.longitude) || 0,
      pricePerNight: Number(formValue.pricePerNight),
      cleaningFee: Number(formValue.cleaningFee),
      serviceFee: Number(formValue.serviceFee),
      minNights: Number(formValue.minNights),
      maxNights: Number(formValue.maxNights),
      bedrooms: Number(formValue.bedrooms),
      bathrooms: Number(formValue.bathrooms),
      maxGuests: Number(formValue.maxGuests),
      // Convert boolean
      instantBook: Boolean(formValue.instantBook),
      // Include images
      images: this.uploadedImages
    };

    this.propertyService.addProperty(requestBody).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.snackBar.open('Property created successfully!', 'Close', { duration: 5000 });
        this.router.navigate(['/host/properties']);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error submitting property:', error);
        this.errorMessage = error.message || 'Failed to create property';
        this.snackBar.open(this.errorMessage || 'An error occurred', 'Close', { duration: 5000 });
      }
    });
  }

  // Helper methods
  private markFormGroupTouched(formGroup: FormGroup) {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getStepProgress() {
    return (this.currentStep / this.totalSteps) * 100;
  }

  // Counter methods for form fields
  increment(field: string) {
    const currentValue = this.propertyForm.get(field)?.value || 0;
    this.propertyForm.get(field)?.setValue(currentValue + 1);
  }

  decrement(field: string) {
    const currentValue = this.propertyForm.get(field)?.value || 0;
    if (currentValue > 1) {
      this.propertyForm.get(field)?.setValue(currentValue - 1);
    }
  }

  // Get amenities list (you should implement this properly)
  getAmenities() {
    return [
      { id: 'wifi', label: 'WiFi', control: 'amenities', value: 'wifi' },
      { id: 'tv', label: 'TV', control: 'amenities', value: 'tv' },
      { id: 'kitchen', label: 'Kitchen', control: 'amenities', value: 'kitchen' },
      { id: 'parking', label: 'Parking', control: 'amenities', value: 'parking' }
    ];
  }

  updateAmenities(amenityValue: string, isChecked: boolean) {
    const currentAmenities = this.propertyForm.get('amenities')?.value || [];
    
    if (isChecked) {
      // Add the amenity if checked
      this.propertyForm.patchValue({
        amenities: [...currentAmenities, amenityValue]
      });
    } else {
      // Remove the amenity if unchecked
      this.propertyForm.patchValue({
        amenities: currentAmenities.filter((a: string) => a !== amenityValue)
      });
    }
  }
}