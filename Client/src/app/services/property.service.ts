import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { PropertyDto } from '../models/property';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private readonly API_URL = 'https://localhost:7228/api/Properties';

  constructor(private http: HttpClient) {}

  // Fetch all properties (handles object response with properties array)
  getProperties(): Observable<PropertyDto[]> {
    return this.http.get<{ properties: PropertyDto[], total: number }>(this.API_URL).pipe(
      map(response => {
        const properties = Array.isArray(response.properties) ? response.properties : [];
        return properties.map(p => ({
          ...p,
          rating: p.averageRating ?? 0,
          isGuestFavorite: p.isGuestFavorite ?? false,
          viewCount: p.viewCount ?? Math.floor(Math.random() * 100)
        }));
      }),
      catchError(error => {
        console.error('Error fetching properties:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url
        });
        return throwError(() => new Error('Failed to fetch properties. Please try again later.'));
      })
    );
  }

  // Fetch paginated properties
  getPropertiesPaginated(page: number, pageSize: number, categoryId: number | null = null, minPrice: number | null = null, maxPrice: number | null = null, excludeHostId: number | null = null): Observable<{ properties: PropertyDto[], total: number }> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (categoryId !== null) {
      params = params.set('categoryId', categoryId.toString());
    }

    if (minPrice !== null) {
      params = params.set('minPrice', minPrice.toString());
    }

    if (maxPrice !== null) {
      params = params.set('maxPrice', maxPrice.toString());
    }
    
    if (excludeHostId !== null) {
      params = params.set('excludeHostId', excludeHostId.toString());
    }

    return this.http.get<{ properties: PropertyDto[], total: number }>(this.API_URL, { params }).pipe(
      map(response => {
        if (!response || typeof response.total !== 'number' || !Array.isArray(response.properties)) {
          throw new Error('Invalid response structure from API');
        }

        return {
          properties: response.properties.map(p => ({
            ...p,
            rating: p.averageRating ?? 0,
            isGuestFavorite: p.isGuestFavorite ?? false,
            viewCount: p.viewCount ?? Math.floor(Math.random() * 100)
          })),
          total: response.total
        };
      }),
      catchError(error => {
        console.error('Error fetching paginated properties:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url
        });
        return throwError(() => new Error('Failed to fetch paginated properties. Please try again later.'));
      })
    );
  }


  getUniqueCountries(): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/countries`).pipe(
      catchError(error => {
        console.error('Error fetching unique countries:', error);
        return throwError(() => new Error('Failed to fetch unique countries'));
      })
    );
  }
  // Search properties with pagination and category filtering
  searchProperties(
    params: {
      title?: string;
      country?: string;
      minNights?: number;
      maxNights?: number;
      startDate?: Date;
      endDate?: Date;
      maxGuests?: number;
      page?: number;
      pageSize?: number;
      categoryId?: number | null;
      excludeHostId?: number | null;
    }
  ): Observable<{ properties: PropertyDto[], total: number }> {
    let queryParams = new HttpParams();

    if (params.title) {
      queryParams = queryParams.set('title', params.title);
    }
    if (params.country) {
      queryParams = queryParams.set('country', params.country);
    }
    if (params.minNights) {
      queryParams = queryParams.set('minNights', params.minNights.toString());
    }
    if (params.maxNights) {
      queryParams = queryParams.set('maxNights', params.maxNights.toString());
    }
    if (params.startDate) {
      queryParams = queryParams.set('startDate', params.startDate.toISOString());
    }
    if (params.endDate) {
      queryParams = queryParams.set('endDate', params.endDate.toISOString());
    }
    if (params.maxGuests) {
      queryParams = queryParams.set('maxGuests', params.maxGuests.toString());
    }
    if (params.page) {
      queryParams = queryParams.set('page', params.page.toString());
    }
    if (params.pageSize) {
      queryParams = queryParams.set('pageSize', params.pageSize.toString());
    }
    if (params.categoryId !== null && params.categoryId !== undefined) {
      queryParams = queryParams.set('categoryId', params.categoryId.toString());
    }
    if (params.excludeHostId !== null && params.excludeHostId !== undefined) {
      queryParams = queryParams.set('excludeHostId', params.excludeHostId.toString());
    }

    const url = `${this.API_URL}/NewSearch`;
    console.log('Search URL:', `${url}?${queryParams.toString()}`);

    return this.http.get<{ properties: PropertyDto[], total: number }>(url, { params: queryParams }).pipe(
      map(response => {
        if (!response || typeof response.total !== 'number' || !Array.isArray(response.properties)) {
          throw new Error('Invalid response structure from API');
        }

        return {
          properties: response.properties.map(p => ({
            ...p,
            rating: p.averageRating ?? 0,
            isGuestFavorite: p.isGuestFavorite ?? false,
            viewCount: p.viewCount ?? Math.floor(Math.random() * 100)
          })),
          total: response.total
        };
      }),
      catchError(error => {
        console.error('Error searching properties:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url
        });
        return throwError(() => new Error('Failed to search properties. Please try again later.'));
      })
    );
  }
}