import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePatientRequest, PagedResult, Patient, UpdatePatientRequest } from './patients.models';

/**
 * Create/getById/update land in Increments 1–2. Increment 3 adds `list()`, a single method
 * serving both the browse-all grid (no query) and the optional search box (query set), both
 * paginated — see Planning\02_Patient_Management_Plan.md §9b.1/§9b.5.
 */
@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePatientRequest): Observable<Patient> {
    return this.http.post<Patient>(`${this.apiBaseUrl}/patients`, request);
  }

  getById(id: string | number): Observable<Patient> {
    return this.http.get<Patient>(`${this.apiBaseUrl}/patients/${id}`);
  }

  update(id: string | number, request: UpdatePatientRequest): Observable<Patient> {
    return this.http.put<Patient>(`${this.apiBaseUrl}/patients/${id}`, request);
  }

  /**
   * Browse-all when `query` is omitted/empty; search when set. Both branches call the same
   * GET /api/patients route and return the same paged envelope shape.
   */
  list(options: { query?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<Patient>> {
    let params = new HttpParams()
      .set('page', String(options.page ?? 1))
      .set('pageSize', String(options.pageSize ?? 25));

    if (options.query) {
      params = params.set('query', options.query);
    }

    return this.http.get<PagedResult<Patient>>(`${this.apiBaseUrl}/patients`, { params });
  }
}
