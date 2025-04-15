import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import { PropertyDto, PropertyCategory, Amenity } from '../models/property';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface PropertyCreateDto {
  categoryId: number;
  title: string;
  description: string;
  propertyType: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  pricePerNight: number;
  cleaningFee: number;
  serviceFee: number;
  minNights: number;
  maxNights: number;
  bedrooms: number;
  bathrooms: number;
  maxGuests: number;
  currency: string;
  instantBook: boolean;
  cancellationPolicyId: number;
  images: {
    imageUrl: string;
    isPrimary: boolean;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class CreatePropertyService {
  private readonly API_URL = 'https://localhost:7228/api';
  private readonly BASE_URL = 'https://localhost:7228';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  getCurrentUserId(): Promise<number> {
    return Promise.resolve(this.authService.getCurrentUserId());
  }

  addProperty(property: PropertyCreateDto): Observable<PropertyDto> {
    console.log('Sending property data to server:', JSON.stringify(property, null, 2));
    return this.http.post<PropertyDto>(`${this.API_URL}/Properties`, property)
      .pipe(
        catchError(error => {
          console.error('Error from server:', error);
          if (error.error && typeof error.error === 'string') {
            return throwError(() => new Error(error.error));
          }
          return throwError(() => new Error('Failed to create property. Please try again.'));
        })
      );
  }

  uploadPropertyImages(files: File[]): Observable<string[]> {
    console.log('Starting image upload...');
    console.log('Files to upload:', files.map(f => ({
      name: f.name,
      type: f.type,
      size: f.size
    })));
    
    const formData = new FormData();
    files.forEach((file, index) => {
        console.log(`Adding file ${index} to form data:`, {
            name: file.name,
            type: file.type,
            size: file.size
        });
        formData.append('files', file);
    });

    // The API now returns full URLs, so we don't need to modify them
    return this.http.post<string[]>(`${this.API_URL}/Properties/images/upload`, formData);
  }

  addImagesToProperty(propertyId: number, imageUrls: string[]): Observable<any> {
    console.log('Adding images to property:', {
      propertyId,
      imageUrls
    });
    
    // The API now expects full URLs, so we don't need to process them
    return this.http.post(`${this.API_URL}/Properties/${propertyId}/images`, { imageUrls: imageUrls });
  }

  uploadImagesForProperty(propertyId: number, files: File[]): Observable<any> {
    console.log('Uploading images for property:', {
      propertyId,
      fileCount: files.length
    });
    
    const formData = new FormData();
    files.forEach((file, index) => {
        formData.append('files', file);
    });

    return this.http.post(`${this.API_URL}/Properties/${propertyId}/upload-images`, formData);
  }

  deleteProperty(propertyId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/Properties/${propertyId}`);
  }

  getCategories(): Observable<PropertyCategory[]> {
    return this.http.get<PropertyCategory[]>(`${this.API_URL}/PropertyCategory`);
  }

  getAmenities(): Observable<Amenity[]> {
    return this.http.get<Amenity[]>(`${this.API_URL}/Amenity`);
  }

  getPropertyById(propertyId: number): Observable<any> {
    return this.http.get(`${this.API_URL}/Properties/${propertyId}`);
  }

  getMyProperties(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/Properties/my-properties`);
  }

  editPropertyAsync(propertyId: number, property: any): Observable<any> {
    return this.http.put(`${this.API_URL}/Properties/${propertyId}`, property);
  }

  deletePropertyImage(propertyId: number, imageId: number): Observable<any> {
    console.log(`Deleting image ${imageId} from property ${propertyId}`);
    return this.http.delete(`${this.API_URL}/Properties/${propertyId}/images/${imageId}`);
  }

}
