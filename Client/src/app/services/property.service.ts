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
    );
  }

  uploadPropertyImages(propertyId: number, files: File[]): Observable<string[]> {
    const formData = new FormData();
    files.forEach((file, index) => {
      formData.append('files', file);
    });

    return this.http.post<string[]>(`${this.API_URL}/${propertyId}/images/upload`, formData);
  }
}