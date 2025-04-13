import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
@Injectable({
  providedIn: 'root'
})
export class CreatePropertyService {
  private apiUrl = 'https://localhost:7228/api/Properties'; // Replace with your API URL

  constructor(private http: HttpClient) { }

  addProperty(propertyData: any): Observable<any> {
    return this.http.post(this.apiUrl, propertyData);
  }
}
