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
import { CreatePropertyService } from '../../../services/property-crud.service';
import { PropertyCategory } from '../../../models/property';
import { Output, EventEmitter, HostListener } from '@angular/core';
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
    MatSnackBarModule,
    MatCardModule
  ],
  templateUrl: './property-listing.component.html',
  styleUrls: ['./property-listing.component.css']
})
export class PropertyListingsComponent implements OnInit {
  properties: PropertyDto[] = [];
  categories: PropertyCategory[] = [];
  currentImageIndices: { [key: number]: number } = {};
  favorites: Set<number> = new Set();
  selectedPropertyId: number | null = null;
  isLoading: boolean = true;
  activeFilter: string = 'All homes';
  selectedCategoryId: number | null = null; // Track selected category
  currentPage: number = 1;
  itemsPerPage: number = 12;
  totalItems: number = 0;
  totalPages: number = 0;
  isScrolled: boolean = false;
  isSearching: boolean = false;
  wishlistProperties: number[] = [];
  showToast: boolean = false;
  toastMessage: string = '';

  @Output() scrollStateChanged = new EventEmitter<boolean>();
  @Output() searchPerformed = new EventEmitter<any>();

  @HostListener('window:scroll', ['$event'])
  onScroll() {
    const scrolled = window.scrollY > 80;

    if (scrolled !== this.isScrolled) {
      this.isScrolled = scrolled;
      this.scrollStateChanged.emit(this.isScrolled);

      const header = document.querySelector('.filters-bar');
      if (scrolled) {
        header?.classList.add('scrolled');
      } else {
        header?.classList.remove('scrolled');
      }
    }
  }
  constructor(
    private router: Router,
    private propertyService: PropertyService,
    private createPropertyService: CreatePropertyService,
    private snackBar: MatSnackBar,
    private profileService: ProfileService
  ) { }

  ngOnInit() {
    this.fetchProperties();
    this.fetchCategories();
    this.loadFavoritesFromStorage();
  }

  fetchCategories(): void {
    this.isLoading = true;
    this.createPropertyService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.isLoading = false;
        console.log(this.categories);
      },
      error: (error) => {
        console.error('Error fetching categories:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          error: error.error
        });
        this.showError('Failed to load categories');
      }
    });
  }

  fetchProperties(page: number = this.currentPage, categoryId: number | null = this.selectedCategoryId) {
    this.isLoading = true;
    this.propertyService.getPropertiesPaginated(page, this.itemsPerPage, categoryId) // Pass categoryId as-is
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: (response: { properties: PropertyDto[], total: number }) => {
          console.log('Properties loaded:', response.properties);
          this.properties = response.properties;
          this.totalItems = response.total;
          this.totalPages = Math.ceil(this.totalItems / this.itemsPerPage);
          this.currentPage = page;
          this.properties.forEach(property => {
            this.currentImageIndices[property.id] = 0;
          });
        },
        error: (error) => {
          console.error('Error fetching properties:', error);
          this.properties = [];
          this.showError(error.message || 'Failed to load properties');
        }
      });
  }

  applyFilter(category: PropertyCategory | 'All homes' | 'Prices') {
    this.currentPage = 1; // Reset to first page
    if (category === 'All homes' || category === 'Prices') {
      this.activeFilter = category;
      this.selectedCategoryId = null;
      this.fetchProperties(1, null); // Fetch all properties
    } else {
      this.activeFilter = category.name;
      this.selectedCategoryId = category.categoryId; // Ensure this matches your PropertyCategory model
      this.fetchProperties(1, category.categoryId); // Fetch properties for selected category
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
    return this.favorites.has(propertyId);
  }

  private loadFavoritesFromStorage() {
    const storedFavorites = localStorage.getItem('favorites');
    if (storedFavorites) {
      const favoriteIds = JSON.parse(storedFavorites) as number[];
      this.favorites = new Set(favoriteIds);
    }
  }

  toggleFavorite(propertyId: number, event: Event): void {
    event.stopPropagation(); // Prevent click from bubbling to parent elements

    if (this.isFavorite(propertyId)) {
      this.removeFromWishlist(propertyId);
    } else {
      this.addToWishlist(propertyId);
    }
  }
  private saveFavoritesToStorage() {
    localStorage.setItem('favorites', JSON.stringify(Array.from(this.favorites)));
  }

  generateRandomWeeks(): number {
    return Math.floor(Math.random() * 8) + 1;
  }

  generateRandomMonth(): string {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[Math.floor(Math.random() * months.length)];
  }

  addToWishlist(propertyId: number): void {
    this.profileService.addOrRemoveToFavourites(propertyId).subscribe({
      next: () => {
        if (!this.wishlistProperties.includes(propertyId)) {
          this.wishlistProperties.push(propertyId);
          this.showToast = true;
          this.toastMessage = "?? Property added to your wishlist! <a href='/wishlist'>Click here to view</a>";

          setTimeout(() => {
            this.showToast = false;
          }, 3000);
        }
      },
      error: (err) => console.error('Error adding to wishlist:', err)
    });
  }
  generateRandomDateRange(): string {
    const startDay = Math.floor(Math.random() * 25) + 1;
    const endDay = startDay + Math.floor(Math.random() * 5) + 3;
    return `${startDay}-${endDay}`;
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.fetchProperties(page, this.selectedCategoryId); // Pass selectedCategoryId
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5; // Show up to 5 page numbers
    let startPage = Math.max(1, this.currentPage - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    // Adjust startPage if endPage is at the limit
    if (endPage - startPage + 1 < maxPagesToShow) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }
  removeFromWishlist(propertyId: number): void {
    this.profileService.addOrRemoveToFavourites(propertyId).subscribe({
      next: () => {
        this.wishlistProperties = this.wishlistProperties.filter(id => id !== propertyId);
      },
      error: (err) => console.error('Error removing from wishlist:', err)
    });
  }
  // Add method to handle search results
  handleSearchResults(searchResults: any): void {
    console.log('Handling search results:', searchResults);

    if (searchResults && searchResults.properties) {
      this.properties = searchResults.properties.properties; // Extract properties from the nested structure
      this.totalItems = searchResults.properties.total;
      this.totalPages = Math.ceil(this.totalItems / this.itemsPerPage);

      // Reset image indices for the new properties
      this.properties.forEach(property => {
        this.currentImageIndices[property.id] = 0;
      });

      // Update active filter to reflect the search
      this.activeFilter = `Results for "${searchResults.destination}"`;

      console.log('Updated properties:', this.properties);
      console.log('Total items:', this.totalItems);
    } else {
      console.error('Invalid search results structure:', searchResults);
      this.showError('Failed to process search results');
    }
  }
}