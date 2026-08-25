import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateVisitRequest, UpdateVisitRequest, Visit } from './visits.models';

/** Mirrors AppointmentService's shape/conventions. */
@Injectable({ providedIn: 'root' })
export class VisitService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  create(request: CreateVisitRequest): Observable<Visit> {
    return this.http.post<Visit>(`${this.apiBaseUrl}/visits`, request);
  }

  getById(id: string): Observable<Visit> {
    return this.http.get<Visit>(`${this.apiBaseUrl}/visits/${id}`);
  }

  update(id: string, request: UpdateVisitRequest): Observable<Visit> {
    return this.http.put<Visit>(`${this.apiBaseUrl}/visits/${id}`, request);
  }

  /** Module 6 (Patient History): fromDate/toDate (ISO date strings, e.g. 'yyyy-MM-dd') are optional
   * and forwarded as query params for server-side date-range filtering; omitted returns the full,
   * unfiltered patient history exactly as before. */
  listByPatientId(patientId: string, fromDate?: string, toDate?: string): Observable<Visit[]> {
    let params = new HttpParams().set('patientId', patientId);
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    if (toDate) {
      params = params.set('toDate', toDate);
    }
    return this.http.get<Visit[]>(`${this.apiBaseUrl}/visits`, { params });
  }
}
