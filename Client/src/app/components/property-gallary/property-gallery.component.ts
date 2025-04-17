import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CreatePropertyService } from '../../services/property-crud.service';

@Component({
  selector: 'app-property-gallery',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './property-gallery.component.html',
  styleUrls: ['./property-gallery.component.scss']
})
export class PropertyGalleryComponent implements OnInit {
  propertyId: number = 0;
  property: any;
  images: string[] = [];
  loading = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private propertyService: CreatePropertyService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.propertyId = +this.route.snapshot.paramMap.get('id')!;
    this.loadPropertyImages();
  }

  loadPropertyImages(): void {
    this.propertyService.getPropertyById(this.propertyId).subscribe({
      next: (data) => {
        this.property = data;
        if (this.property.images && this.property.images.length > 0) {
          this.images = this.property.images.map((img: any) => this.getFullImageUrl(img.imageUrl));
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load property images';
        this.loading = false;
        console.error(err);
      }
    });
  }

  getFullImageUrl(imageUrl: string): string {
    if (imageUrl.startsWith('http')) {
      return imageUrl;
    }
    return `https://localhost:7228${imageUrl}`;
  }

  goBack(): void {
    this.router.navigate(['/property', this.propertyId]);
  }
}