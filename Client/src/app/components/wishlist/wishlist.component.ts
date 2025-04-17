// wishlist.component.ts
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProfileService } from '../../services/profile.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-wishlist',
  templateUrl: './wishlist.component.html',
  imports: [CommonModule],
  styleUrls: ['./wishlist.component.css']
})
export class WishlistComponent implements OnInit {
  favorites: any[] = [];
  loading = true;
  error = '';
  apiBaseUrl = 'https://localhost:7228'; // Backend server URL


  constructor(
    private profileService: ProfileService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadFavorites();
  }

  loadFavorites(): void {
    this.loading = true;
    this.profileService.getUserFavorites().subscribe({
      next: (data) => {
        this.favorites = data;


        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading favorites:', err);
        this.error = 'Failed to load your wishlist. Please try again later.';
        this.loading = false;
      }
    });
  }

  
  getImageUrl(url: string): string {
    if (!url) {
      return 'assets/images/placeholder.jpg';
    }
    
    // If the URL is already absolute (starts with http:// or https://)
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    
    // If it's a relative URL, prepend the backend API base URL
    if (url.startsWith('/')) {
      return `${this.apiBaseUrl}${url}`;
    } else {
      return `${this.apiBaseUrl}/${url}`;
    }
  }

  getPrimaryImageUrl(property: { primaryImage?: string; images?: { isPrimary: boolean; imageUrl: string }[] }): string {
    // First try to get the primary image
    if (property.primaryImage) {
      return this.getImageUrl(property.primaryImage);
    }
    
    // If no primary image is explicitly set, look through all images
    if (property.images && property.images.length > 0) {
      // Try to find a primary image
      const primaryImage = property.images.find((img: { isPrimary: boolean; imageUrl: string }) => img.isPrimary);
      if (primaryImage) {
        return this.getImageUrl(primaryImage.imageUrl);
      }
      
      // If no primary image is found, just return the first image
      return this.getImageUrl(property.images[0].imageUrl);
    }
    
    // If no images at all, return a placeholder
    return 'assets/images/placeholder.jpg';
  }


  viewProperty(propertyId: number): void {
    this.router.navigate(['/property', propertyId]);
  }

  removeFromWishlist(propertyId: number, event: Event): void {
    event.stopPropagation(); // Prevent navigation to property details
    
    this.profileService.addOrRemoveToFavourites(propertyId).subscribe({
      next: () => {
        this.favorites = this.favorites.filter(item => item.property.id !== propertyId);
      },
      error: (err) => {
        console.error('Error removing from favorites:', err);
      }
    });
  }

  getFormattedDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  }
}