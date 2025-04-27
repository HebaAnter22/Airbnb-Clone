import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateViolationDto {
  violationType: string;
  description: string;
  reportedPropertyId?: number;
  reportedHostId?: number;
}

export interface ViolationResponseDto {
  id: number;
  reportedById: number;
  reporterName: string;
  reportedPropertyId?: number;
  reportedPropertyTitle?: string;
  reportedHostId?: number;
  reportedHostName?: string;
  violationType: string;
  description: string;
  status: string;
  createdAt: Date;
  resolvedAt?: Date;
}

export interface UpdateViolationStatusDto {
  status: string;
  adminNotes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ViolationService {
  private readonly API_URL = 'https://localhost:7228/api/Violations';

  constructor(private http: HttpClient) { }

  reportViolation(data: CreateViolationDto): Observable<ViolationResponseDto> {
    return this.http.post<ViolationResponseDto>(this.API_URL, data);
  }

  getAllViolations(): Observable<ViolationResponseDto[]> {
    return this.http.get<ViolationResponseDto[]>(this.API_URL);
  }

  getViolationsByStatus(status: string): Observable<ViolationResponseDto[]> {
    return this.http.get<ViolationResponseDto[]>(`${this.API_URL}/status/${status}`);
  }

  getMyViolations(): Observable<ViolationResponseDto[]> {
    return this.http.get<ViolationResponseDto[]>(`${this.API_URL}/user`);
  }

  getViolationById(id: number): Observable<ViolationResponseDto> {
    return this.http.get<ViolationResponseDto>(`${this.API_URL}/${id}`);
  }

  updateViolationStatus(id: number, data: UpdateViolationStatusDto): Observable<ViolationResponseDto> {
    return this.http.put<ViolationResponseDto>(`${this.API_URL}/${id}/status`, data);
  }

  blockHost(hostId: number): Observable<any> {
    return this.http.put(`${this.API_URL}/block-host/${hostId}`, {});
  }
} 