import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PropertyService } from '../../../services/property.service';
import { DecimalPipe } from '@angular/common';
import { PropertyDto } from '../../../models/property';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-property-listings',
  standalone: true,
  imports: [
    HeaderComponent,
    DecimalPipe,
    CommonModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './property-listing.component.html',
  styleUrls: ['./property-listing.component.css']
})
export class PropertyListingsComponent implements OnInit {
  properties: PropertyDto[] = [];
  currentImageIndices: { [key: number]: number } = {};
  favorites: Set<number> = new Set();

  constructor(
    private router: Router,
    private propertyService: PropertyService
  ) {}

  ngOnInit() {
    this.fetchProperties();
  }

  fetchProperties() {
    this.propertyService.getProperties().subscribe({
      next: (properties) => {
        this.properties = properties;
        // Initialize current image indices
        this.properties.forEach(property => {
          this.currentImageIndices[property.id] = 0;
        });
      },
      error: (error) => {
        console.error('Error fetching properties:', error);
        this.properties = [];
      }
    });
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
  }

  isFavorite(propertyId: number): boolean {
    return this.favorites.has(propertyId);
  }
}