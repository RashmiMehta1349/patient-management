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

  listByPatientId(patientId: string): Observable<Visit[]> {
    const params = new HttpParams().set('patientId', patientId);
    return this.http.get<Visit[]>(`${this.apiBaseUrl}/visits`, { params });
  }
}
