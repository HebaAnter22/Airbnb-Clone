import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { CreatePropertyService, PropertyCreateDto } from '../../services/property-crud.service';
import { PropertyCategory, Amenity, PropertyDto } from '../../models/property';

interface LocationData {
  address: string;
  city: string;
  country: string;
  postalCode?: string;
  latitude: number;
  longitude: number;
}

interface InvalidField {
  step: number;
  name: string;
  message: string;
}

@Component({
  selector: 'app-add-property',
  templateUrl: './add-property.component.html',
  styleUrls: ['./add-property.component.css']
})
export class AddPropertyComponent implements OnInit {
  propertyForm: FormGroup;
  currentStep = 1;
  totalSteps = 4;
  uploadedImages: File[] = [];
  isLoading = false;
  errorMessage = '';
  showForm = true;
  showHero = true;
  categories: PropertyCategory[] = [];
  amenities: Amenity[] = [];
  savedFormData: Partial<PropertyDto> | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private propertyService: CreatePropertyService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {
    this.propertyForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(20)]],
      propertyType: ['', Validators.required],
      address: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
      latitude: [0, Validators.required],
      longitude: [0, Validators.required],
      pricePerNight: [0, [Validators.required, Validators.min(1)]],
      cleaningFee: [0, [Validators.required, Validators.min(0)]],
      serviceFee: [0, [Validators.required, Validators.min(0)]],
      minNights: [1, [Validators.required, Validators.min(1)]],
      maxNights: [30, [Validators.required, Validators.min(1)]],
      bedrooms: [1, [Validators.required, Validators.min(1)]],
      bathrooms: [1, [Validators.required, Validators.min(1)]],
      maxGuests: [1, [Validators.required, Validators.min(1)]],
      amenities: [[], Validators.required],
      images: [[], Validators.required],
      categoryId: [0, Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadAmenities();
    if (this.savedFormData) {
      this.propertyForm.patchValue(this.savedFormData);
    }
  }

  loadCategories(): void {
    this.propertyService.getCategories().subscribe(
      (categories: PropertyCategory[]) => {
        this.categories = categories;
      },
      (error) => {
        console.error('Error loading categories:', error);
        this.snackBar.open('Error loading categories', 'Close', { duration: 3000 });
      }
    );
  }

  loadAmenities(): void {
    this.propertyService.getAmenities().subscribe(
      (amenities: Amenity[]) => {
        this.amenities = amenities;
      },
      (error) => {
        console.error('Error loading amenities:', error);
        this.snackBar.open('Error loading amenities', 'Close', { duration: 3000 });
      }
    );
  }

  submitForm(): void {
    if (this.propertyForm.invalid) {
      const invalidFields: InvalidField[] = [];
      Object.keys(this.propertyForm.controls).forEach(key => {
        const control = this.propertyForm.get(key);
        if (control?.invalid) {
          let step = 1;
          let message = 'This field is required';

          // Determine which step the field belongs to
          if (['title', 'description', 'propertyType', 'categoryId'].includes(key)) {
            step = 1;
          } else if (['address', 'city', 'country', 'latitude', 'longitude'].includes(key)) {
            step = 2;
          } else if (['pricePerNight', 'cleaningFee', 'serviceFee', 'minNights', 'maxNights'].includes(key)) {
            step = 3;
          } else if (['bedrooms', 'bathrooms', 'maxGuests', 'amenities', 'images'].includes(key)) {
            step = 4;
          }

          // Create specific error messages
          if (control.errors?.['required']) {
            message = `${key.charAt(0).toUpperCase() + key.slice(1)} is required`;
          } else if (control.errors?.['minlength']) {
            message = `${key.charAt(0).toUpperCase() + key.slice(1)} must be at least ${control.errors['minlength'].requiredLength} characters`;
          } else if (control.errors?.['min']) {
            message = `${key.charAt(0).toUpperCase() + key.slice(1)} must be at least ${control.errors['min'].min}`;
          }

          invalidFields.push({ step, name: key, message });
        }
      });

      if (invalidFields.length > 0) {
        // Sort by step to find the earliest step with an error
        invalidFields.sort((a, b) => a.step - b.step);
        const firstInvalidStep = invalidFields[0].step;

        // Create error message with all invalid fields
        const errorMessages = invalidFields.map(field => field.message);
        const errorMessage = `Please fix the following errors:\n${errorMessages.join('\n')}`;

        // Show error message
        this.snackBar.open(errorMessage, 'Close', { duration: 10000 });

        // Navigate to the first step with an error
        this.currentStep = firstInvalidStep;
        return;
      }
    }

    this.isLoading = true;
    const formData = this.propertyForm.value;
    
    // Create the property DTO with all required fields
    const propertyDto: PropertyCreateDto = {
      categoryId: formData.categoryId,
      title: formData.title,
      description: formData.description,
      propertyType: formData.propertyType,
      country: formData.country,
      address: formData.address,
      city: formData.city,
      postalCode: formData.postalCode || '00000',
      latitude: formData.latitude,
      longitude: formData.longitude,
      pricePerNight: formData.pricePerNight,
      cleaningFee: formData.cleaningFee,
      serviceFee: formData.serviceFee,
      minNights: formData.minNights,
      maxNights: formData.maxNights,
      bedrooms: formData.bedrooms,
      bathrooms: formData.bathrooms,
      maxGuests: formData.maxGuests,
      currency: 'USD',
      instantBook: true,
      cancellationPolicyId: 1,
      images: this.uploadedImages.map((file: File) => ({
        imageUrl: file.name,
        isPrimary: false
      }))
    };
    
    this.propertyService.addProperty(propertyDto).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.snackBar.open('Property added successfully!', 'Close', { duration: 3000 });
        this.router.navigate(['/properties']);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error adding property:', error);
        this.snackBar.open('Error adding property', 'Close', { duration: 3000 });
      }
    });
  }

  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getStepProgress(): number {
    return (this.currentStep / this.totalSteps) * 100;
  }

  increment(): void {
    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  decrement(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  onLocationSelected(locationData: LocationData): void {
    if (!locationData.city || !locationData.country) {
      this.snackBar.open('Please select a location with valid city and country', 'Close', { duration: 3000 });
      return;
    }

    this.propertyForm.patchValue({
      address: locationData.address,
      city: locationData.city,
      country: locationData.country,
      latitude: locationData.latitude,
      longitude: locationData.longitude
    });
  }
}
