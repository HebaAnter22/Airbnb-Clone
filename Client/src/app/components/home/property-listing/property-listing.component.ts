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
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { ProfileService } from '../../../services/profile.service';

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
    MatCardModule,
    SearchBarComponent
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
  isSearching: boolean = false;
  wishlistProperties: number[] = []; 
  showToast: boolean = false;
  toastMessage: string = ''; 

  constructor(
    private router: Router,
    private propertyService: PropertyService,
    private snackBar: MatSnackBar,
    private profileService:ProfileService
  ) {}

  ngOnInit() {
    this.fetchProperties();
    this.loadFavoritesFromStorage();
    this.loadWishlistProperties();
  }

  fetchProperties() {
    this.isLoading = true;
    this.isSearching = false;
    this.propertyService.getProperties()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: (properties) => {
          console.log('Fetched all properties:', properties);
          this.properties = properties;
          this.properties.forEach(property => {
            this.currentImageIndices[property.id] = 0;
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

  onSearch(searchParams: any) {
    console.log('Search params received:', searchParams);
    this.isLoading = true;
    this.isSearching = true;

    this.propertyService.searchProperties({
      country: searchParams.destination,
      startDate: searchParams.checkIn,
      endDate: searchParams.checkOut,
      maxGuests: searchParams.guests.adults + searchParams.guests.children
    }).subscribe({
      next: (properties) => {
        console.log('Search results:', properties);
        this.properties = properties;
        this.properties.forEach(property => {
          this.currentImageIndices[property.id] = 0;
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Search failed:', error);
        this.showError('Failed to search properties');
        this.isLoading = false;
      }
    });
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

  // toggleFavorite(propertyId: number, event: MouseEvent) {
  //   event.stopPropagation();
  //   if (this.favorites.has(propertyId)) {
  //     this.favorites.delete(propertyId);
  //   } else {
  //     this.favorites.add(propertyId);
  //   }
  //   this.saveFavoritesToStorage();
  // }


  loadWishlistProperties(): void {
    this.profileService.getUserFavorites().subscribe({
      next: (properties) => {
        // Assuming the service returns an array of property IDs or objects with IDs
        console.log('Wishlist properties from API:', properties);
        this.wishlistProperties = properties.map((property: any) => property.propertyId);
          console.log('Wishlist properties loaded:', this.wishlistProperties);
      },
      error: (err) => console.error('Error loading wishlist:', err)
    });
  }

  isFavorite(propertyId: number): boolean {
    return this.wishlistProperties.includes(propertyId);
  }


  toggleFavorite(propertyId: number, event: Event): void {
    event.stopPropagation(); // Prevent click from bubbling to parent elements
    
    if (this.isFavorite(propertyId)) {
      console.log('Removing from wishlist:', propertyId);
      this.removeFromWishlist(propertyId);
    } else {
      console.log('Adding to wishlist:', propertyId);
      this.addToWishlist(propertyId);
    }
  }

  addToWishlist(propertyId: number): void {
    this.profileService.addOrRemoveToFavourites(propertyId).subscribe({
      next: () => {
        if (!this.wishlistProperties.includes(propertyId)) {
          this.wishlistProperties.push(propertyId);
          this.showToast = true;
          this.toastMessage = "🏡 Property added to your wishlist! <a href='/wishlist'>Click here to view</a>";
  
          setTimeout(() => {
            this.showToast = false;
          }, 3000);
        }
      },
      error: (err) => console.error('Error adding to wishlist:', err)
    });
  }
 
  removeFromWishlist(propertyId: number): void {
    this.profileService.addOrRemoveToFavourites(propertyId).subscribe({
      next: () => {
        this.wishlistProperties = this.wishlistProperties.filter(id => id !== propertyId);
      },
      error: (err) => console.error('Error removing from wishlist:', err)
    });
  }

  // isFavorite(propertyId: number): boolean {
  //   return this.favorites.has(propertyId);
  // }

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

  clearSearch() {
    if (this.isSearching) {
      this.fetchProperties();
    }
  }
}