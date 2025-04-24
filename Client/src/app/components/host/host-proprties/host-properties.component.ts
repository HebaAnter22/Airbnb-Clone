import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CreatePropertyService } from '../../../services/property-crud.service';
import { PropertyDto } from '../../../models/property';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  selector: 'app-host-properties',
  standalone: true,
  templateUrl: './host-properties.component.html',
  styleUrls: ['./host-properties.component.css'],
  imports: [CommonModule, RouterModule, MatSnackBarModule]
})
export class HostPropertiesComponent implements OnInit, OnChanges {
  @Input() status: 'active' | 'pending' = 'active';
  properties: PropertyDto[] = [];
  filteredProperties: PropertyDto[] = [];
  loading: boolean = true;
  error: string | null = null;
  currentImageIndices: { [key: number]: number } = {};

  constructor(
    private propertyService: CreatePropertyService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadProperties();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['status'] && !changes['status'].firstChange) {
      this.filterProperties();
    }
  }
  viewPropertyDetails(propertyid:number) {
    this.router.navigate(['/property', propertyid]);
  }
  async loadProperties() {
    try {
      const userId = await this.propertyService.currentUserValue();
      const result = await this.propertyService.getMyProperties().toPromise();
      
      if (result) {
        this.properties = result;
        this.filterProperties();
      }
      
      this.loading = false;
    } catch (err) {
      this.error = 'Failed to load properties';
      this.loading = false;
      this.showError('Failed to load properties');
    }
  }

  filterProperties() {
    console.log('Filtering properties for status:', this.status);
    console.log('All properties:', this.properties);
    
    this.filteredProperties = this.properties.filter(property => {
      const isMatch = this.status === 'active' 
        ? property.status?.toLowerCase() === 'active'
        : property.status?.toLowerCase() === 'pending';
      
      console.log(`Property ${property.id} status: ${property.status}, isMatch: ${isMatch}`);
      return isMatch;
    });

    console.log('Filtered properties:', this.filteredProperties);

    // Initialize currentImageIndex for filtered properties
    this.filteredProperties.forEach(property => {
      if (property && property.id) {
        this.currentImageIndices[property.id] = 0;
      }
    });
  }

  getPropertyImage(property: PropertyDto): string {
    if (!property || !property.images || property.images.length === 0) {
      return 'assets/images/property-placeholder.jpg';
    }
    const currentIndex = property.id ? (this.currentImageIndices[property.id] ?? 0) : 0;
    return property.images[currentIndex]?.imageUrl || 'assets/images/property-placeholder.jpg';
  }

  hasMultipleImages(property: PropertyDto): boolean {
    return property?.images && property.images.length > 1;
  }

  handleImageError(property: PropertyDto) {
    if (!property || !property.id) return;

    console.warn(`Image failed to load for property ${property.id}:`, this.getPropertyImage(property));
    const currentIndex = this.currentImageIndices[property.id] ?? 0;
    
    if (property.images && property.images.length > currentIndex + 1) {
      this.currentImageIndices[property.id] = currentIndex + 1;
    } else {
      property.images = [{ 
        id: 0, 
        imageUrl: 'assets/images/property-placeholder.jpg', 
        isPrimary: true 
      }];
    }
  }

  prevImage(propertyId: number, event: MouseEvent) {
    event.stopPropagation();
    const property = this.filteredProperties.find(p => p.id === propertyId);
    if (!property || !property.images) return;

    const imagesLength = property.images.length;
    if (imagesLength > 1) {
      const currentIndex = this.currentImageIndices[propertyId] ?? 0;
      this.currentImageIndices[propertyId] = (currentIndex - 1 + imagesLength) % imagesLength;
    }
  }

  nextImage(propertyId: number, event: MouseEvent) {
    event.stopPropagation();
    const property = this.filteredProperties.find(p => p.id === propertyId);
    if (!property || !property.images) return;

    const imagesLength = property.images.length;
    if (imagesLength > 1) {
      const currentIndex = this.currentImageIndices[propertyId] ?? 0;
      this.currentImageIndices[propertyId] = (currentIndex + 1) % imagesLength;
    }
  }

  editProperty(propertyId: number) {
    this.router.navigate(['/host/edit', propertyId]);
  }

  viewBookingDetails(propertyId: number) {
    this.router.navigate(['/host/bookings', propertyId]);
  }

  async deleteProperty(propertyId: number) {
    if (confirm('Are you sure you want to delete this property?')) {
      try {
        await this.propertyService.deleteProperty(propertyId).toPromise();
        this.properties = this.properties.filter(p => p.id !== propertyId);
        this.filterProperties();
      } catch (err) {
        this.showError('Failed to delete property');
      }
    }
  }

  showError(message: string) {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar']
    });
  }
} 