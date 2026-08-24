import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePatientRequest,
  DuplicateCheckResponse,
  PatientResponse,
  UpdatePatientRequest
} from './patient.models';

/**
 * API client for the Patient Management endpoints (Module 2 plan §6). Mirrors AuthService's
 * plain HttpClient usage/error-handling style — errors are left to callers to inspect via
 * HttpErrorResponse, consistent with how the auth feature components handle them.
 */
@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly baseUrl = `${environment.apiBaseUrl}/patients`;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePatientRequest): Observable<PatientResponse> {
    return this.http.post<PatientResponse>(this.baseUrl, request);
  }

  getById(id: string): Observable<PatientResponse> {
    return this.http.get<PatientResponse>(`${this.baseUrl}/${id}`);
  }

  update(id: string, request: UpdatePatientRequest): Observable<PatientResponse> {
    return this.http.put<PatientResponse>(`${this.baseUrl}/${id}`, request);
  }

  search(term: string): Observable<PatientResponse[]> {
    const params = new HttpParams().set('search', term);
    return this.http.get<PatientResponse[]>(this.baseUrl, { params });
  }

  getRecent(limit = 10): Observable<PatientResponse[]> {
    const params = new HttpParams().set('sort', 'recent').set('limit', limit);
    return this.http.get<PatientResponse[]>(this.baseUrl, { params });
  }

  checkDuplicate(name: string, phone: string): Observable<DuplicateCheckResponse> {
    const params = new HttpParams().set('name', name).set('phone', phone);
    return this.http.get<DuplicateCheckResponse>(`${this.baseUrl}/check-duplicate`, { params });
  }
}
