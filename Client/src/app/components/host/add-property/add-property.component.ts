import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CreatePropertyService, PropertyCreateDto } from '../../../services/property-crud.service';
import { PropertyCategory, Amenity } from '../../../models/property';
import { LocationMapComponent } from '../../map/location-map.component';
import { ImageUploadComponent } from '../image-upload/image-upload.component';
import { AuthService } from '../../../services/auth.service';

interface InvalidField {
  field: string;
  value: any;
  errors: any;
}

interface LocationData {
  address: string;
  city: string;
  country: string;
  postalCode?: string;
  latitude: number;
  longitude: number;
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
    MatProgressSpinnerModule,
    LocationMapComponent,
    ImageUploadComponent
  ],
  templateUrl: './add-property.component.html',
  styleUrls: ['./add-property.component.css']
})
export class AddPropertyComponent implements OnInit {
  propertyForm!: FormGroup;
  currentStep = 1;
  totalSteps = 10;
  steps = [
    'Property Type',
    'Sharing Type',
    'Location',
    'Basics',
    'Amenities',
    'Photos',
    'Title & Description',
    'Price & Booking'
  ];
  uploadedImages: File[] = [];
  uploadedImageUrls: string[] = [];
  isLoading = false;
  errorMessage = '';
  showForm = true;
  currentSection = 1;
  showHero = true;
  user: any;
  userProfile: any;
  categories: PropertyCategory[] = [];
  amenities: Amenity[] = [];
  selectedAmenities: Set<number> = new Set();
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

  async ngOnInit() {
    try {
      await Promise.all([
        this.loadCategories(),
        this.loadAmenities()
      ]);

      // Setup form value changes subscription for debugging
      this.propertyForm.valueChanges.subscribe(values => {
        console.log('Form values updated:', values);
        this.logFormValidationStatus();
      });

      // Initial logging of form state
      this.logFormValidationStatus();
    } catch (error) {
      console.error('Error initializing component:', error);
      this.snackBar.open('Error loading required data. Please try again.', 'Close', {
        duration: 5000,
        horizontalPosition: 'center',
        verticalPosition: 'bottom'
      });
    }
  }

  private initializeForm() {
    this.propertyForm = this.fb.group({
      categoryId: ['', Validators.required],
      propertyType: ['', Validators.required],
      sharingType: ['', Validators.required],
      title: ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(20)]],
      address: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
      postalCode: [''],
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
      instantBook: [true],
      cancellationPolicyId: [1],
      images: [[], Validators.required]
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
  nextStep(): void {
    console.log('Attempting to move to next step...');
    if (this.validateCurrentStep()) {
      if (this.currentStep < this.totalSteps) {
        this.currentStep++;
        this.errorMessage = '';
        console.log(`Successfully moved to step ${this.currentStep}`);
        this.logFormValidationStatus();
      }
    } else {
      console.warn(`Cannot proceed to next step. Current step ${this.currentStep} validation failed.`);
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = '';
    }
  }

  isStepComplete(step: number): boolean {
    if (step > this.currentStep) {
      return false;
    }
    
    const previousStep = this.currentStep;
    this.currentStep = step;
    const isValid = this.validateCurrentStep();
    this.currentStep = previousStep;
    return isValid;
  }

  nextSection() {
    if (this.currentSection < 10) {
      this.currentSection++;
      this.errorMessage = '';
    }
  }

  prevSection() {
    if (this.currentSection > 1) {
      this.currentSection--;
      this.errorMessage = '';
    }
  }

  // Form display control
  displayForm() {
    this.showForm = true;
    this.showHero = false;
    this.currentStep = 1;
    this.errorMessage = '';
  }

  // Image handling
  onImageUpload(event: Event) {
    this.errorMessage = '';
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
          this.uploadedImages.push(file);
          
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
  async submitForm(): Promise<void> {
    console.log('Form submission started...');
    console.log('Complete form state:', {
      values: this.propertyForm.value,
      valid: this.propertyForm.valid,
      dirty: this.propertyForm.dirty,
      touched: this.propertyForm.touched,
      imageUrls: this.uploadedImageUrls
    });
    
    if (this.propertyForm.invalid) {
      console.error('Form submission failed: Invalid form');
      console.log('Validation errors:', this.getFormValidationErrors());
      this.markFormGroupTouched(this.propertyForm);
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    this.isLoading = true;
    console.log('Form is valid. Creating property...');

    try {
      // Format the property data according to API expectations
      const formValues = this.propertyForm.value;
      
      // Ensure amenities is an array of IDs, not null values
      const amenities = formValues.amenities || [];
      const validAmenities = amenities.filter((id: number | null | undefined) => id !== null && id !== undefined);
      
      // According to your comment, the API uses propertyType for what we collect as sharingType
      // So we'll map the sharingType value to propertyType in the API request
      
      // The API now expects full URLs, so we don't need to strip the base URL
      const propertyData: PropertyCreateDto = {
        categoryId: formValues.categoryId,
        title: formValues.title,
        description: formValues.description,
        propertyType: formValues.sharingType, // Using the sharingType (entire_place, private_room, etc.) as propertyType
        country: formValues.country,
        address: formValues.address,
        city: formValues.city,
        postalCode: formValues.postalCode || '',
        latitude: parseFloat(formValues.latitude),
        longitude: parseFloat(formValues.longitude),
        pricePerNight: formValues.pricePerNight,
        cleaningFee: formValues.cleaningFee || 0,
        serviceFee: formValues.serviceFee || 0,
        minNights: formValues.minNights,
        maxNights: formValues.maxNights,
        bedrooms: formValues.bedrooms,
        bathrooms: formValues.bathrooms,
        maxGuests: formValues.maxGuests,
        currency: 'USD', // Default currency
        instantBook: formValues.instantBook || false,
        cancellationPolicyId: formValues.cancellationPolicyId || 1,
        amenities: validAmenities, // Ensure we're sending valid amenity IDs
        images: this.uploadedImageUrls.map((url, index) => ({
          imageUrl: url,
          isPrimary: index === 0
        }))
      };
      
      console.log('Prepared property data:', propertyData);

      const createdProperty = await this.propertyService.addProperty(propertyData).toPromise();
      console.log('Property created successfully:', createdProperty);
      
      if (!createdProperty?.id) {
        throw new Error('Failed to create property: No ID returned');
      }
      
      this.snackBar.open('Property created successfully!', 'Close', { duration: 3000 });
      this.router.navigate(['/properties']);
    } catch (error) {
      console.error('Error creating property:', error);
      this.snackBar.open('Error creating property', 'Close', { duration: 3000 });
    } finally {
      this.isLoading = false;
    }
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

  private async loadCategories() {
    try {
      const categories = await this.propertyService.getCategories().toPromise();
      this.categories = categories || [];
    } catch (error) {
      console.error('Error loading categories:', error);
      throw error;
    }
  }

  private async loadAmenities() {
    try {
      const amenities = await this.propertyService.getAmenities().toPromise();
      
      // Map the amenities to ensure they have the correct property names
      this.amenities = (amenities || []).map(amenity => {
        // Log the original amenity object
        console.log('Original amenity:', amenity);
        
        // Check if the amenity has an 'id' property instead of 'amenityId'
        const amenityAny = amenity as any;
        if (amenityAny.id !== undefined && amenityAny.amenityId === undefined) {
          console.log('Mapping amenity.id to amenity.amenityId');
          return {
            ...amenity,
            amenityId: amenityAny.id
          };
        }
        
        return amenity;
      });
      
      // Debug the amenities
      console.log('Mapped amenities:', this.amenities);
      if (this.amenities.length > 0) {
        console.log('First amenity:', this.amenities[0]);
        console.log('First amenity ID:', this.amenities[0].amenityId);
        console.log('First amenity keys:', Object.keys(this.amenities[0]));
      }
      
      // Initialize the form with empty amenities array
      this.propertyForm.patchValue({
        amenities: []
      });
    } catch (error) {
      console.error('Error loading amenities:', error);
      throw error;
    }
  }

  toggleAmenity(amenityId: number) {
    console.log('Toggling amenity:', amenityId);
    
    // Check if amenityId is valid
    if (amenityId === undefined || amenityId === null) {
      console.error('Invalid amenity ID:', amenityId);
      return;
    }
    
    // Get the current amenities array
    const currentAmenities = this.propertyForm.get('amenities')?.value || [];
    console.log('Current amenities:', currentAmenities);
    
    // Create a new array based on whether the amenity is already selected
    let newAmenities: number[];
    if (currentAmenities.includes(amenityId)) {
      // Remove the amenity if it's already selected
      newAmenities = currentAmenities.filter((id: number) => id !== amenityId);
      console.log('Removing amenity, new array:', newAmenities);
    } else {
      // Add the amenity if it's not selected
      newAmenities = [...currentAmenities, amenityId];
      console.log('Adding amenity, new array:', newAmenities);
    }
    
    // Update the form with the new amenities array
    this.propertyForm.patchValue({
      amenities: newAmenities
    });
    
    // Mark as touched to trigger validation
    this.propertyForm.get('amenities')?.markAsTouched();
  }

  isAmenitySelected(amenityId: number): boolean {
    // Check if amenityId is valid
    if (amenityId === undefined || amenityId === null) {
      console.error('Invalid amenity ID in isAmenitySelected:', amenityId);
      return false;
    }
    
    const amenities = this.propertyForm.get('amenities')?.value || [];
    return amenities.includes(amenityId);
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

  onImagesUploaded(urls: string[]): void {
    console.log('Images uploaded successfully:', urls);
    this.uploadedImageUrls = urls;
    
    // Update the form control
    this.propertyForm.patchValue({
      images: urls.map((url, index) => ({
        imageUrl: url,
        isPrimary: index === 0
      }))
    });
    
    console.log('Updated form with image URLs');
    this.propertyForm.get('images')?.markAsTouched();
    
    if (urls.length === 0) {
      console.warn('No images uploaded');
      this.errorMessage = 'Please upload at least one image';
    } else {
      console.log(`${urls.length} images have been uploaded`);
      this.errorMessage = '';
    }
  }

  onUploadError(error: string): void {
    console.error('Image upload error:', error);
    this.errorMessage = error;
    this.snackBar.open(error, 'Close', { duration: 3000 });
  }

  validateCurrentStep(): boolean {
    console.log(`Validating step ${this.currentStep}: ${this.steps[this.currentStep - 1]}`);
    
    switch (this.currentStep) {
      case 1: // Property Type
        if (!this.propertyForm.get('propertyType')?.valid) {
          console.warn('Property Type validation failed:', this.propertyForm.get('propertyType')?.errors);
          this.errorMessage = 'Please select a property type';
          return false;
        }
        break;

      case 2: // Sharing Type
        if (!this.propertyForm.get('sharingType')?.valid) {
          console.warn('Sharing Type validation failed:', this.propertyForm.get('sharingType')?.errors);
          this.errorMessage = 'Please select a sharing type';
          return false;
        }
        break;

      case 3: // Location
        const locationControls = ['address', 'city', 'country', 'latitude', 'longitude'];
        for (const control of locationControls) {
          if (!this.propertyForm.get(control)?.valid) {
            console.warn(`Location validation failed for ${control}:`, this.propertyForm.get(control)?.errors);
            this.errorMessage = `Please provide a valid ${control.replace(/([A-Z])/g, ' $1').toLowerCase()}`;
            return false;
          }
        }
        break;

      case 4: // Basics
        const basicControls = ['bedrooms', 'bathrooms', 'maxGuests'];
        for (const control of basicControls) {
          if (!this.propertyForm.get(control)?.valid) {
            console.warn(`Basics validation failed for ${control}:`, this.propertyForm.get(control)?.errors);
            this.errorMessage = `Please provide valid ${control.replace(/([A-Z])/g, ' $1').toLowerCase()}`;
            return false;
          }
        }
        break;

      case 5: // Amenities
        if (!this.propertyForm.get('amenities')?.value?.length) {
          console.warn('Amenities validation failed: No amenities selected');
          this.errorMessage = 'Please select at least one amenity';
          return false;
        }
        break;

      case 6: // Photos
        if (this.uploadedImageUrls.length === 0) {
          console.warn('Photos validation failed: No images uploaded');
          this.errorMessage = 'Please upload at least one photo';
          return false;
        }
        break;

      case 7: // Title & Description
        if (!this.propertyForm.get('title')?.valid || !this.propertyForm.get('description')?.valid) {
          console.warn('Title & Description validation failed:', {
            title: this.propertyForm.get('title')?.errors,
            description: this.propertyForm.get('description')?.errors
          });
          this.errorMessage = 'Please provide both title and description';
          return false;
        }
        break;

      case 8: // Price & Booking
        if (!this.propertyForm.get('pricePerNight')?.valid || !this.propertyForm.get('minNights')?.valid) {
          console.warn('Price & Booking validation failed:', {
            pricePerNight: this.propertyForm.get('pricePerNight')?.errors,
            minNights: this.propertyForm.get('minNights')?.errors
          });
          this.errorMessage = 'Please provide valid price and minimum nights';
          return false;
        }
        break;
    }
    
    console.log(`Step ${this.currentStep} validation successful`);
    this.errorMessage = '';
    return true;
  }

  // Log form validation status to console
  private logFormValidationStatus() {
    console.log('Form valid:', this.propertyForm.valid);
    console.log('Form errors:', this.getFormValidationErrors());
    console.log('Current step:', this.currentStep);
    
    // Log specific field validations for the current step
    switch (this.currentStep) {
      case 1: // Property Type
        console.log('Property Type validation:', {
          value: this.propertyForm.get('propertyType')?.value,
          valid: this.propertyForm.get('propertyType')?.valid,
          errors: this.propertyForm.get('propertyType')?.errors
        });
        break;
        
      case 2: // Sharing Type
        console.log('Sharing Type validation:', {
          value: this.propertyForm.get('sharingType')?.value,
          valid: this.propertyForm.get('sharingType')?.valid,
          errors: this.propertyForm.get('sharingType')?.errors
        });
        break;
        
      case 3: // Location
        const locationControls = ['address', 'city', 'country', 'latitude', 'longitude'];
        console.log('Location validation:', locationControls.map(control => ({
          field: control,
          value: this.propertyForm.get(control)?.value,
          valid: this.propertyForm.get(control)?.valid,
          errors: this.propertyForm.get(control)?.errors
        })));
        break;
        
      case 4: // Basics
        const basicControls = ['bedrooms', 'bathrooms', 'maxGuests'];
        console.log('Basics validation:', basicControls.map(control => ({
          field: control,
          value: this.propertyForm.get(control)?.value,
          valid: this.propertyForm.get(control)?.valid,
          errors: this.propertyForm.get(control)?.errors
        })));
        break;
        
      case 5: // Amenities
        console.log('Amenities validation:', {
          value: this.propertyForm.get('amenities')?.value,
          valid: this.propertyForm.get('amenities')?.valid,
          errors: this.propertyForm.get('amenities')?.errors
        });
        break;
        
      case 6: // Photos
        console.log('Photos validation:', {
          uploadedImageUrls: this.uploadedImageUrls,
          length: this.uploadedImageUrls.length
        });
        break;
        
      case 7: // Title & Description
        console.log('Title & Description validation:', {
          title: {
            value: this.propertyForm.get('title')?.value,
            valid: this.propertyForm.get('title')?.valid,
            errors: this.propertyForm.get('title')?.errors
          },
          description: {
            value: this.propertyForm.get('description')?.value,
            valid: this.propertyForm.get('description')?.valid,
            errors: this.propertyForm.get('description')?.errors
          }
        });
        break;
        
      case 8: // Price & Booking
        console.log('Price & Booking validation:', {
          pricePerNight: {
            value: this.propertyForm.get('pricePerNight')?.value,
            valid: this.propertyForm.get('pricePerNight')?.valid,
            errors: this.propertyForm.get('pricePerNight')?.errors
          },
          minNights: {
            value: this.propertyForm.get('minNights')?.value,
            valid: this.propertyForm.get('minNights')?.valid,
            errors: this.propertyForm.get('minNights')?.errors
          }
        });
        break;
    }
  }

  // Helper method to get all validation errors
  private getFormValidationErrors(): any[] {
    const result: any[] = [];
    Object.keys(this.propertyForm.controls).forEach(key => {
      const control = this.propertyForm.get(key);
      if (control && !control.valid) {
        result.push({
          field: key,
          value: control.value,
          errors: control.errors
        });
      }
    });
    return result;
  }
}