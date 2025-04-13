import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PropertyCategory, Amenity } from '../models/property';

export interface PropertyCreateDto {
  categoryId: number;
  title: string;
  description: string;
  propertyType: string;
  country: string;
  address: string;
  city: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  pricePerNight: number;
  cleaningFee?: number;
  serviceFee?: number;
  minNights?: number;
  maxNights?: number;
  bedrooms?: number;
  bathrooms?: number;
  maxGuests?: number;
  currency: string;
  instantBook?: boolean;
  cancellationPolicyId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class CreatePropertyService {
  private baseUrl = 'https://localhost:7228/api/Properties';

  constructor(private http: HttpClient) { }

  getCategories(): Observable<PropertyCategory[]> {
    return this.http.get<PropertyCategory[]>(`https://localhost:7228/api/PropertyCategories`);
  }

  getAmenities(): Observable<Amenity[]> {
    return this.http.get<Amenity[]>(`https://localhost:7228/api/Amenities`);
  }

  addProperty(propertyData: PropertyCreateDto): Observable<any> {
    // Ensure required fields are present
    const data = {
      ...propertyData,
      currency: propertyData.currency || 'USD',
      postalCode: propertyData.postalCode || '00000'
    };
    return this.http.post(`${this.baseUrl}`, data);
  }

  getPropertyById(propertyId: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/${propertyId}`);
  }

  // getPropertyiesbyUserId(userId: number): Observable<any[]> {
  //   return this.http.get<any[]>(`${this.apiUrl}/Properties/user/${userId}`);
  // }
}
