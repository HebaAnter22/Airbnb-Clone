import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PropertyService } from '../../../services/property.service';
import { DecimalPipe } from '@angular/common';
import { PropertyDto } from '../../../models/property';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HeaderComponent } from '../header/header.component';
import { HttpClientModule } from '@angular/common/http';
import { ImageUploadComponent } from '../../host/image-upload/image-upload.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { finalize } from 'rxjs/operators';
import { StickyNavComponent } from '../sticky-nav/sticky-nav.component';

@Component({
  selector: 'app-property-listings',
  standalone: true,
  imports: [
    StickyNavComponent,
    HeaderComponent,
    DecimalPipe,
    CommonModule,
    MatButtonModule,
    MatIconModule,
    HttpClientModule,
    ImageUploadComponent,
    MatSnackBarModule,
    MatCardModule
  ],
  templateUrl: './property-listing.component.html',
  styleUrls: ['./property-listing.component.css']
})
export class PropertyListingsComponent implements OnInit {
  properties: PropertyDto[] = [];
  currentImageIndices: { [key: number]: number } = {};
  favorites: Set<number> = new Set();
  selectedPropertyId: number | null = null;
  isLoading: boolean = true;
  activeFilter: string = 'All homes';

  constructor(
    private router: Router,
    private propertyService: PropertyService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.fetchProperties();
    this.loadFavoritesFromStorage();
  }

  fetchProperties() {
    this.isLoading = true;
    this.propertyService.getProperties()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: (properties) => {
          this.properties = properties;
          this.properties.forEach(property => {
            this.currentImageIndices[property.id] = 0;
            console.log(`Property ${property.id} images:`, property.images);
          });
        },
        error: (error) => {
          console.error('Error fetching properties:', {
            status: error.status,
            statusText: error.statusText,
            message: error.message,
            error: error.error
          });
          this.properties = [];
          this.showError('Failed to load properties');
        }
      });
  }

  applyFilter(filter: string) {
    this.activeFilter = filter;
    // In a real implementation, this would filter the properties based on the selected filter
    // For now, we'll just show all properties
  }

  showError(message: string) {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar']
    });
  }

  getPropertyImage(property: PropertyDto): string {
    if (!property.images || property.images.length === 0) {
      return 'assets/images/property-placeholder.jpg';
    }
    const currentIndex = this.currentImageIndices[property.id] ?? 0;
    return property.images[currentIndex]?.imageUrl || 'assets/images/property-placeholder.jpg';
  }

  handleImageError(property: PropertyDto) {
    console.warn(`Image failed to load for property ${property.id}:`, this.getPropertyImage(property));
    const currentIndex = this.currentImageIndices[property.id] ?? 0;
    
    if (property.images && property.images.length > currentIndex + 1) {
      this.currentImageIndices[property.id] = currentIndex + 1;
    } else {
      // Use a placeholder if all images fail
      property.images = [{ 
        id: 0, 
        imageUrl: 'assets/images/property-placeholder.jpg', 
        isPrimary: true 
      }];
    }
  }

  prevImage(propertyId: number, event: MouseEvent) {
    event.stopPropagation();
    const property = this.properties.find(p => p.id === propertyId);
    if ((property?.images ?? []).length > 1) {
      this.currentImageIndices[propertyId] =
        (this.currentImageIndices[propertyId] - 1 + (property?.images?.length ?? 0)) % (property?.images?.length ?? 1);
    }
  }

  nextImage(propertyId: number, event: MouseEvent) {
    event.stopPropagation();
    const property = this.properties.find(p => p.id === propertyId);
    if ((property?.images ?? []).length > 1) {
      this.currentImageIndices[propertyId] =
        (this.currentImageIndices[propertyId] + 1) % (property?.images?.length ?? 1);
    }
  }

  goToProperty(propertyId: number) {
    this.router.navigate(['/property', propertyId]);
  }

  toggleFavorite(propertyId: number, event: MouseEvent) {
    event.stopPropagation();
    if (this.favorites.has(propertyId)) {
      this.favorites.delete(propertyId);
    } else {
      this.favorites.add(propertyId);
    }
    this.saveFavoritesToStorage();
  }

  isFavorite(propertyId: number): boolean {
    return this.favorites.has(propertyId);
  }

  private loadFavoritesFromStorage() {
    const storedFavorites = localStorage.getItem('favorites');
    if (storedFavorites) {
      const favoriteIds = JSON.parse(storedFavorites) as number[];
      this.favorites = new Set(favoriteIds);
    }
  }

  private saveFavoritesToStorage() {
    localStorage.setItem('favorites', JSON.stringify(Array.from(this.favorites)));
  }

  generateRandomWeeks(): number {
    // Random number between 1 and 8
    return Math.floor(Math.random() * 8) + 1;
  }

  generateRandomMonth(): string {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    // Random month
    return months[Math.floor(Math.random() * months.length)];
  }

  generateRandomDateRange(): string {
    // Generate random start day between 1 and 25
    const startDay = Math.floor(Math.random() * 25) + 1;
    // End day is start day + random number between 3 and 7
    const endDay = startDay + Math.floor(Math.random() * 5) + 3;
    return `${startDay}-${endDay}`;
  }
}