import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Module 8 (Data Export) — authenticated blob-fetch client for the four new export endpoints
 * (plan §7, §10 task 13), mirroring PrescriptionService's call pattern exactly (Bearer interceptor
 * applies automatically). includeHistory defaults to false, matching the server's default (plan
 * §5 Open Question 3 — patient export is profile-only unless explicitly opted in).
 */
@Injectable({ providedIn: 'root' })
export class DataExportService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  exportPatientCsv(patientId: string | number, includeHistory = false): Observable<Blob> {
    return this.http.get(`${this.apiBaseUrl}/patients/${patientId}/export/csv`, {
      responseType: 'blob',
      params: new HttpParams().set('includeHistory', includeHistory)
    });
  }

  exportPatientPdf(patientId: string | number, includeHistory = false): Observable<Blob> {
    return this.http.get(`${this.apiBaseUrl}/patients/${patientId}/export/pdf`, {
      responseType: 'blob',
      params: new HttpParams().set('includeHistory', includeHistory)
    });
  }

  exportVisitCsv(visitId: string | number): Observable<Blob> {
    return this.http.get(`${this.apiBaseUrl}/visits/${visitId}/export/csv`, { responseType: 'blob' });
  }

  exportVisitPdf(visitId: string | number): Observable<Blob> {
    return this.http.get(`${this.apiBaseUrl}/visits/${visitId}/export/pdf`, { responseType: 'blob' });
  }
}
