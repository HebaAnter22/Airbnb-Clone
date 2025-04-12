import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PropertyDto } from '../models/property';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private apiUrl = 'https://localhost:7228/api/Properties';

  constructor(private http: HttpClient) {}

  getProperties(): Observable<PropertyDto[]> {
    return this.http.get<PropertyDto[]>(this.apiUrl).pipe(
      map(properties => properties.map(p => ({
        ...p,
        // Calculate UI-specific fields if not provided by backend
        rating: p.averageRating || 0,
        isGuestFavorite: p.isGuestFavorite || false,
        viewCount: p.viewCount || Math.floor(Math.random() * 100) // Example random view count
      })))
    );
  }
}