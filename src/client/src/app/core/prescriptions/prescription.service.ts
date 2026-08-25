import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Module 5 — server-generated PDF prescription (product decision: server-generated PDF rather
 * than browser-native window.print(), overriding the plan's original browser-print
 * recommendation). Fetches the PDF as a blob via the same authenticated HttpClient (Bearer
 * interceptor applies) and hands the caller a Blob to open/download.
 */
@Injectable({ providedIn: 'root' })
export class PrescriptionService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getPrescriptionPdf(visitId: string): Observable<Blob> {
    return this.http.get(`${this.apiBaseUrl}/visits/${visitId}/prescription/pdf`, { responseType: 'blob' });
  }
}
