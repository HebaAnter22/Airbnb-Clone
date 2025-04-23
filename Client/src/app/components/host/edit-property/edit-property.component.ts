import { Component, OnInit, ViewChild, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CreatePropertyService } from '../../../services/property-crud.service';
import { PropertyDto, PropertyImageDto, Amenity } from '../../../models/property';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbToastModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-edit-property',
  standalone: true,
  templateUrl: './edit-property.component.html',
  styleUrls: ['./edit-property.component.css'],
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbToastModule]
})
export class EditPropertyComponent implements OnInit {
  @ViewChild('deleteModal') deleteModal!: TemplateRef<any>;
  propertyToDelete: number | null = null;

  propertyForm: FormGroup;
  propertyId!: number;
  property!: PropertyDto;
  loading = true;
  saving = false;
  error: string | null = null;
  propertyImages: PropertyImageDto[] = [];
  uploadedFiles: File[] = [];
  previewUrls: string[] = [];
  allAmenities: Amenity[] = [];
  propertyAmenities: Amenity[] = [];
  selectedAmenityIds: Set<number> = new Set();
  
  // Navigation sections
  sections = [
    { id: 'basic', label: 'Basic Information' },
    { id: 'location', label: 'Location' },
    { id: 'pricing', label: 'Pricing' },
    { id: 'details', label: 'Property Details' },
    { id: 'amenities', label: 'Amenities' },
    { id: 'images', label: 'Images' }
  ];
  
  activeSection: string = 'basic';

  showToast = false;
  toastMessage = '';
  toastType = 'danger'; // can be 'success', 'danger', 'warning', etc.

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private propertyService: CreatePropertyService,
    private modalService: NgbModal
  ) {
    this.propertyForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(50)]],
      address: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
      postalCode: ['', Validators.required],
      pricePerNight: ['', [Validators.required, Validators.min(1)]],
      cleaningFee: ['', [Validators.required, Validators.min(0)]],
      serviceFee: ['', [Validators.required, Validators.min(0)]],
      minNights: ['', [Validators.required, Validators.min(1)]],
      maxNights: ['', [Validators.required, Validators.min(1)]],
      bedrooms: ['', [Validators.required, Validators.min(1)]],
      bathrooms: ['', [Validators.required, Validators.min(1)]],
      maxGuests: ['', [Validators.required, Validators.min(1)]],
      instantBook: [false],
      status: ['Active'],
      amenities: [[]]
    });
  }

  ngOnInit() {
    this.propertyId = +this.route.snapshot.params['id'];
    this.loadData();
  }

  getSectionIcon(sectionId: string): string {
    const iconMap: { [key: string]: string } = {
      'basic': 'info',
      'location': 'location_on',
      'pricing': 'attach_money',
      'details': 'apartment',
      'amenities': 'spa',
      'images': 'photo_library'
    };
    return iconMap[sectionId] || 'info';
  }

  async loadData() {
    try {
      // Load property data
      this.property = await this.propertyService.getPropertyById(this.propertyId).toPromise();
      console.log('Loaded property data:', this.property);
      
      // Initialize selected amenities as a Set with proper IDs
      this.selectedAmenityIds = new Set(
        this.property.amenities?.map(a => a.id) || []
      );
      
      // Update form with property data
      this.propertyForm.patchValue({
        ...this.property,
        instantBook: this.property.instantBook || false,
        amenities: Array.from(this.selectedAmenityIds)
      });
      
      this.propertyImages = this.property.images || [];
      this.loading = false;

      // Load and categorize amenities
      await this.loadAmenities();
    } catch (err) {
      console.error('Error loading data:', err);
      this.error = 'Failed to load property data';
      this.loading = false;
    }
  }

  async loadAmenities() {
    try {
      const result = await this.propertyService.getAmenities().toPromise();
      this.allAmenities = result || [];
      
      // Update property amenities based on selected IDs
      this.updatePropertyAmenities();

      console.log('All Amenities:', this.allAmenities);
      console.log('Selected Amenity IDs:', Array.from(this.selectedAmenityIds));
      console.log('Property Amenities:', this.propertyAmenities);
    } catch (err) {
      console.error('Error loading amenities:', err);
      this.error = 'Failed to load amenities';
    }
  }

  private updatePropertyAmenities() {
    this.propertyAmenities = this.allAmenities.filter(amenity => 
      this.selectedAmenityIds.has(amenity.id)
    );
  }

  setActiveSection(sectionId: string) {
    this.activeSection = sectionId;
  }

  onInstantBookChange(event: any) {
    const checked = event.target.checked;
    this.propertyForm.patchValue({ instantBook: checked });
    this.property.instantBook = checked;
  }

  async onFileUpload(event: any) {
    const files: FileList = event.target.files;
    if (files.length > 0) {
      try {
        // Store the files for preview
        this.uploadedFiles = [...this.uploadedFiles, ...Array.from(files)];
        
        // Create preview URLs
        for (const file of Array.from(files)) {
          const reader = new FileReader();
          reader.onload = (e: any) => {
            this.previewUrls.push(e.target.result);
          };
          reader.readAsDataURL(file);
        }

        // Upload the files
        const uploadedUrls = await this.propertyService.uploadPropertyImages(Array.from(files)).toPromise();
        if (uploadedUrls) {
          // Add the images to the property
          await this.propertyService.addImagesToProperty(this.propertyId, uploadedUrls).toPromise();
          
          // Reload the property to get the updated images
          await this.loadProperty();
        }
      } catch (err) {
        console.error('Failed to upload images:', err);
        this.error = 'Failed to upload images';
      }
    }
  }

  async loadProperty() {
    try {
      this.property = await this.propertyService.getPropertyById(this.propertyId).toPromise();
      this.propertyForm.patchValue(this.property);
      this.propertyImages = this.property.images || [];
    } catch (err) {
      console.error('Failed to reload property:', err);
      this.error = 'Failed to reload property';
    }
  }

  removeImage(index: number) {
    // Get the image ID from the propertyImages array
    const imageToDelete = this.propertyImages[index];
    
    if (imageToDelete && imageToDelete.id) {
      // Show confirmation dialog
      if (confirm('Are you sure you want to delete this image?')) {
        // Call the service to delete the image
        this.propertyService.deletePropertyImage(this.propertyId, imageToDelete.id)
          .subscribe({
            next: () => {
              // Remove from propertyImages array after successful deletion
              this.propertyImages.splice(index, 1);
              console.log(`Image ${imageToDelete.id} deleted successfully`);
            },
            error: (err) => {
              console.error('Failed to delete image:', err);
              this.error = 'Failed to delete image. Please try again.';
            }
          });
      }
    } else {
      // If the image doesn't have an ID (e.g., it's a newly uploaded image that hasn't been saved yet)
      this.propertyImages.splice(index, 1);
    }
  }

  removeUploadedFile(index: number) {
    // Remove from uploadedFiles and previewUrls arrays
    this.uploadedFiles.splice(index, 1);
    this.previewUrls.splice(index, 1);
  }

  toggleAmenity(amenityId: number, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    
    if (!amenityId) {
      console.error('Invalid amenity ID:', amenityId);
      return;
    }

    if (this.selectedAmenityIds.has(amenityId)) {
      this.selectedAmenityIds.delete(amenityId);
    } else {
      this.selectedAmenityIds.add(amenityId);
    }

    // Update the form control value with the current selection
    const selectedIds = Array.from(this.selectedAmenityIds);
    this.propertyForm.patchValue({
      amenities: selectedIds
    });

    // Update the property amenities list
    this.propertyAmenities = this.allAmenities.filter(amenity => 
      this.selectedAmenityIds.has(amenity.id)
    );

    console.log('Selected amenity IDs:', selectedIds);
    console.log('Updated property amenities:', this.propertyAmenities);
  }

  isAmenitySelected(amenityId: number): boolean {
    return amenityId ? this.selectedAmenityIds.has(amenityId) : false;
  }

  async deleteProperty(propertyId: number) {
    this.propertyToDelete = propertyId;
    this.modalService.open(this.deleteModal, { centered: true });
  }

  async confirmDelete() {
    if (!this.propertyToDelete) return;

    try {
      await this.propertyService.deleteProperty(this.propertyToDelete).toPromise();
      this.modalService.dismissAll();
      this.toastMessage = 'Property deleted successfully';
      this.toastType = 'success';
      this.showToast = true;
      setTimeout(() => {
        this.showToast = false;
        this.router.navigate(['/host']);
      }, 2000);
    } catch (err) {
      this.modalService.dismissAll();
      this.toastMessage = 'Failed to delete property';
      this.toastType = 'danger';
      this.showToast = true;
      setTimeout(() => this.showToast = false, 3000);
    }
    this.propertyToDelete = null;
  }

  async onSubmit() {
    if (this.propertyForm.valid) {
      this.saving = true;
      try {
        // Create a clean update object with only the fields that have changed
        const formValue = this.propertyForm.value;
        const updatedProperty: any = {};
        
        // Only include fields that have values
        Object.keys(formValue).forEach(key => {
          if (formValue[key] !== null && formValue[key] !== undefined) {
            updatedProperty[key] = formValue[key];
          }
        });
        
        // Ensure instantBook and amenities are included
        updatedProperty.instantBook = this.property.instantBook;
        updatedProperty.amenities = Array.from(this.selectedAmenityIds);
        
        console.log('Updating property with:', updatedProperty);
        
        await this.propertyService.editPropertyAsync(this.propertyId, updatedProperty).toPromise();
        this.router.navigate(['/host']);
      } catch (err) {
        console.error('Failed to update property:', err);
        this.error = 'Failed to update property';
        this.saving = false;
      }
    }
  }

  cancel() {
    this.router.navigate(['/host']);
  }
} 