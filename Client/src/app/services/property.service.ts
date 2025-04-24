import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { PropertyDto } from '../models/property';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private readonly API_URL = 'https://localhost:7228/api/Properties';

  constructor(
    private http: HttpClient
  ) {}

  getProperties(): Observable<PropertyDto[]> {
    return this.http.get<PropertyDto[]>(this.API_URL).pipe(
      map(properties => properties.map(p => ({
        ...p,
        // Calculate UI-specific fields if not provided by backend
        rating: p.averageRating || 0,
        isGuestFavorite: p.isGuestFavorite || false,
        viewCount: p.viewCount || Math.floor(Math.random() * 100) // Example random view count
      })))
     
    )
    
    ;

  }

  searchProperties(params: {
    title?: string;
    country?: string;
    minNights?: number;
    maxNights?: number;
    startDate?: Date;
    endDate?: Date;
    maxGuests?: number;
  }): Observable<PropertyDto[]> {
    const queryParams = new URLSearchParams();
    
    if (params.title) {
      queryParams.append('title', params.title);
    }
    
    if (params.country) {
      queryParams.append('country', params.country);
    }
    
    if (params.minNights) {
      queryParams.append('minNights', params.minNights.toString());
    }
    
    if (params.maxNights) {
      queryParams.append('maxNights', params.maxNights.toString());
    }
    
    if (params.startDate) {
      queryParams.append('startDate', params.startDate.toISOString());
    }
    
    if (params.endDate) {
      queryParams.append('endDate', params.endDate.toISOString());
    }
    
    if (params.maxGuests) {
      queryParams.append('maxGuests', params.maxGuests.toString());
    }

    console.log('Search URL:', `${this.API_URL}/NewSearch?${queryParams.toString()}`);

    return this.http.get<PropertyDto[]>(`${this.API_URL}/NewSearch?${queryParams.toString()}`).pipe(
      map(properties => properties.map(p => ({
        ...p,
        rating: p.averageRating || 0,
        isGuestFavorite: p.isGuestFavorite || false,
        viewCount: p.viewCount || Math.floor(Math.random() * 100)
      })))
    );
  }
}