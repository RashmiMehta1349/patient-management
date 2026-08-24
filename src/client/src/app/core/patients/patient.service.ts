import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePatientRequest, Patient, UpdatePatientRequest } from './patients.models';

/**
 * Increment 1: create. Increment 2 adds getById/update — see
 * Planning\02_Patient_Management_Plan.md §9a.8. Search lands in Increment 3.
 */
@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePatientRequest): Observable<Patient> {
    return this.http.post<Patient>(`${this.apiBaseUrl}/patients`, request);
  }

  getById(id: string): Observable<Patient> {
    return this.http.get<Patient>(`${this.apiBaseUrl}/patients/${id}`);
  }

  update(id: string, request: UpdatePatientRequest): Observable<Patient> {
    return this.http.put<Patient>(`${this.apiBaseUrl}/patients/${id}`, request);
  }
}
