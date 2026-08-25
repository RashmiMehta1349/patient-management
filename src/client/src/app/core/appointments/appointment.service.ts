import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Appointment,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  UpdateAppointmentStatusRequest
} from './appointments.models';

/**
 * Mirrors PatientService's shape/conventions. listByDate/listByPatientId both call the same
 * GET /api/appointments route, differentiated by which query param is set (approved plan §6).
 */
@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  create(request: CreateAppointmentRequest): Observable<Appointment> {
    return this.http.post<Appointment>(`${this.apiBaseUrl}/appointments`, request);
  }

  getById(id: string): Observable<Appointment> {
    return this.http.get<Appointment>(`${this.apiBaseUrl}/appointments/${id}`);
  }

  update(id: string, request: UpdateAppointmentRequest): Observable<Appointment> {
    return this.http.put<Appointment>(`${this.apiBaseUrl}/appointments/${id}`, request);
  }

  updateStatus(id: string, status: UpdateAppointmentStatusRequest['status']): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiBaseUrl}/appointments/${id}/status`, { status });
  }

  listByDate(date: string): Observable<Appointment[]> {
    const params = new HttpParams().set('date', date);
    return this.http.get<Appointment[]>(`${this.apiBaseUrl}/appointments`, { params });
  }

  listByPatientId(patientId: string): Observable<Appointment[]> {
    const params = new HttpParams().set('patientId', patientId);
    return this.http.get<Appointment[]>(`${this.apiBaseUrl}/appointments`, { params });
  }
}
