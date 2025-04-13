import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CreatePropertyService } from '../../services/property-crud.service';
import { AuthService } from '../auth/auth.service';
import { PropertyCategory, Amenity } from '../../models/property';
import { LocationMapComponent } from '../map/location-map.component';

interface InvalidField {
  field: string;
  value: any;
  errors: any;
}

@Component({
  selector: 'app-add-property',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    MatIconModule, 
    MatButtonModule, 
    MatInputModule,
    LocationMapComponent
  ],
  templateUrl: './add-property.component.html',
  styleUrls: ['./add-property.component.css']
})
export class AddPropertyComponent implements OnInit {
  currentSection = 1;
  currentStep = 1;
  totalSteps = 8;
  steps = [
    'Category',
    'Space Type',
    'Location',
    'Basics',
    'Amenities',
    'Photos',
    'Description',
    'Price'
  ];
  propertyForm!: FormGroup;
  uploadedImages: string[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  showForm = false;
  showHero = true;
  user: any;
  userProfile: any;
  categories: PropertyCategory[] = [];
  amenities: Amenity[] = [];
  selectedAmenities: number[] = [];
  savedFormData: any;

  constructor(
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private propertyService: CreatePropertyService,
    private snackBar: MatSnackBar
  ) {
    this.initializeForm();
    this.user = this.authService.currentUserValue;
    this.getUserProfile();
  }

  ngOnInit() {
    this.loadCategories();
    this.loadAmenities();
  }

  private initializeForm() {
    this.propertyForm = this.fb.group({
      categoryId: [1, Validators.required],
      title: ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(50)]],
      propertyType: ['', Validators.required],
      sharingType: ['entire_place', Validators.required],
      country: ['', Validators.required],
      address: ['', Validators.required],
      city: ['', Validators.required],
      postalCode: ['', Validators.required],
      latitude: ['', Validators.required],
      longitude: ['', Validators.required],
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
      images: [[]],
      amenities: [[]]
    });

    // Store the initial form values
    this.savedFormData = this.propertyForm.value;
  }

  private getUserProfile() {
    this.authService.getUserProfile().subscribe({
      next: (profile) => {
        this.userProfile = profile;
      },
      error: (error) => {
        console.error('Error fetching user profile:', error);
      }
    });
  }

  getFullName(): string {
    if (this.userProfile) {
      return `${this.userProfile.firstName} ${this.userProfile.lastName}`;
    }
    return '';
  }

  // Navigation methods
  nextStep() {
    if (this.currentStep < this.totalSteps) {
      // Validate current step before proceeding
      if (this.validateCurrentStep()) {
        this.savedFormData = { ...this.savedFormData, ...this.propertyForm.value };
        this.currentStep++;
        this.errorMessage = null;
      }
    }
  }

  private validateCurrentStep(): boolean {
    const stepValidations = {
      1: () => this.propertyForm.get('categoryId')?.valid && this.propertyForm.get('propertyType')?.valid,
      2: () => this.propertyForm.get('sharingType')?.valid,
      3: () => {
        const locationControls = ['address', 'city', 'country', 'latitude', 'longitude'];
        return locationControls.every(control => this.propertyForm.get(control)?.valid);
      },
      4: () => {
        const basicControls = ['bedrooms', 'bathrooms', 'maxGuests'];
        return basicControls.every(control => this.propertyForm.get(control)?.valid);
      },
      5: () => true, // Amenities are optional
      6: () => this.uploadedImages.length >= 1,
      7: () => this.propertyForm.get('title')?.valid && this.propertyForm.get('description')?.valid,
      8: () => this.propertyForm.get('pricePerNight')?.valid && this.propertyForm.get('minNights')?.valid
    };

    const isValid = stepValidations[this.currentStep as keyof typeof stepValidations]?.() ?? false;
    
    if (!isValid) {
      this.markFormGroupTouched(this.propertyForm);
      this.errorMessage = `Please complete all required fields in ${this.steps[this.currentStep - 1]}`;
      return false;
    }

    return true;
  }

  prevStep() {
    if (this.currentStep > 1) {
      // Save current form data
      this.savedFormData = { ...this.savedFormData, ...this.propertyForm.value };
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

    Array.from(input.files).forEach(file => {
      if (!file.type.match('image.*')) {
        this.errorMessage = 'Only image files are allowed';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result) {
          const imageData = e.target.result as string;
          this.uploadedImages.push(imageData);
          
          // Update form control
          const currentImages = this.propertyForm.get('images')?.value || [];
          this.propertyForm.patchValue({
            images: [...currentImages, {
              imageUrl: imageData,
              isPrimary: currentImages.length === 0,
              category: 'property'
            }]
          });
        }
      };
      reader.onerror = () => {
        this.errorMessage = 'Error reading file';
      };
      reader.readAsDataURL(file);
    });
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
      
      // Get all invalid fields
      const invalidFields: InvalidField[] = [];
      Object.keys(this.propertyForm.controls).forEach(key => {
        const control = this.propertyForm.get(key);
        if (control?.invalid) {
          invalidFields.push({
            field: key,
            value: control.value,
            errors: control.errors
          });
        }
      });

      console.log('Invalid fields:', invalidFields);

      // Create a user-friendly error message
      const missingFields = invalidFields.map(field => {
        let fieldName = field.field.replace(/([A-Z])/g, ' $1').toLowerCase();
        fieldName = fieldName.charAt(0).toUpperCase() + fieldName.slice(1);
        
        if (!field.value) {
          return `${fieldName} is required`;
        } else if (field.errors?.['min']) {
          return `${fieldName} must be at least ${field.errors['min'].min}`;
        } else if (field.errors?.['minlength']) {
          return `${fieldName} must be at least ${field.errors['minlength'].requiredLength} characters`;
        }
        return `${fieldName} is invalid`;
      });

      this.errorMessage = 'Please fix the following issues:\n' + missingFields.join('\n');
      this.snackBar.open(this.errorMessage, 'Close', { 
        duration: 10000,
        verticalPosition: 'top'
      });

      // Navigate to the first step with an invalid field
      const steps = {
        1: ['categoryId', 'propertyType'],
        2: ['address', 'city', 'country', 'postalCode', 'latitude', 'longitude'],
        3: ['bedrooms', 'bathrooms', 'maxGuests'],
        4: ['amenities'],
        5: ['images'],
        6: ['title', 'description'],
        7: ['pricePerNight', 'minNights', 'maxNights', 'cancellationPolicyId']
      };

      for (const [step, fields] of Object.entries(steps)) {
        if (fields.some(field => this.propertyForm.get(field)?.invalid)) {
          this.currentStep = Number(step);
          break;
        }
      }

      return;
    }

    if (this.uploadedImages.length < 1) {
      this.errorMessage = 'Please upload at least one image';
      this.currentStep = 5; // Navigate to photos step
      this.snackBar.open(this.errorMessage, 'Close', { duration: 5000 });
      return;
    }

    this.isLoading = true;

    // Prepare the API request body
    const formValue = this.propertyForm.value;
    const requestBody = {
      categoryId: Number(formValue.categoryId),
      title: formValue.title,
      description: formValue.description,
      propertyType: formValue.propertyType,
      country: formValue.country,
      address: formValue.address,
      city: formValue.city,
      postalCode: formValue.postalCode,
      latitude: Number(formValue.latitude) || 0,
      longitude: Number(formValue.longitude) || 0,
      pricePerNight: Number(formValue.pricePerNight),
      cleaningFee: Number(formValue.cleaningFee) || 0,
      serviceFee: Number(formValue.serviceFee) || 0,
      minNights: Number(formValue.minNights),
      maxNights: Number(formValue.maxNights),
      bedrooms: Number(formValue.bedrooms),
      bathrooms: Number(formValue.bathrooms),
      maxGuests: Number(formValue.maxGuests),
      currency: formValue.currency,
      instantBook: Boolean(formValue.instantBook),
      cancellationPolicyId: Number(formValue.cancellationPolicyId),
      images: this.uploadedImages.map((imageData, index) => ({
        imageUrl: imageData,
        isPrimary: index === 0,
        category: 'property'
      }))
    };

    console.log('Submitting form with data:', requestBody);

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

  getStepProgress(): number {
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

  private loadCategories() {
    this.propertyService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.snackBar.open('Error loading property categories', 'Close', { duration: 5000 });
      }
    });
  }

  private loadAmenities() {
    this.propertyService.getAmenities().subscribe({
      next: (amenities) => {
        this.amenities = amenities;
      },
      error: (error) => {
        console.error('Error loading amenities:', error);
        this.snackBar.open('Error loading amenities', 'Close', { duration: 5000 });
      }
    });
  }

  toggleAmenity(amenityId: number) {
    const currentAmenities = this.propertyForm.get('amenities')?.value || [];
    const index = currentAmenities.indexOf(amenityId);
    
    if (index === -1) {
      currentAmenities.push(amenityId);
    } else {
      currentAmenities.splice(index, 1);
    }
    
    this.propertyForm.patchValue({
      amenities: currentAmenities
    });
  }

  isAmenitySelected(amenityId: number): boolean {
    return this.propertyForm.get('amenities')?.value?.includes(amenityId) || false;
  }

  onLocationSelected(location: any) {
    if (!location) return;

    // Update form with location data
    this.propertyForm.patchValue({
      address: location.address,
      city: location.city,
      country: location.country,
      postalCode: location.postalCode || '',
      latitude: location.latitude.toFixed(6),
      longitude: location.longitude.toFixed(6)
    });

    // Save the location data
    this.savedFormData = { 
      ...this.savedFormData, 
      address: location.address,
      city: location.city,
      country: location.country,
      postalCode: location.postalCode || '',
      latitude: location.latitude.toFixed(6),
      longitude: location.longitude.toFixed(6)
    };

    // Validate location fields
    if (!location.city || !location.country) {
      this.snackBar.open('Please select a more specific location', 'Close', { duration: 5000 });
      return;
    }
  }
}