import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePatientRequest, Patient } from './patients.models';

/**
 * Increment 1 scope: create only. GET-by-id/search/update land in later increments per the
 * approved plan (Planning\02_Patient_Management_Plan.md §9).
 */
@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePatientRequest): Observable<Patient> {
    return this.http.post<Patient>(`${this.apiBaseUrl}/patients`, request);
  }
}
