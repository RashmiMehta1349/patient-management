import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DataExportService } from './data-export.service';
import { environment } from '../../../environments/environment';

describe('DataExportService', () => {
  let service: DataExportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(DataExportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests patient CSV export with includeHistory=false by default', () => {
    const blob = new Blob(['a,b'], { type: 'text/csv' });

    service.exportPatientCsv('p1').subscribe((result) => expect(result).toEqual(blob));

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/patients/p1/export/csv` && r.params.get('includeHistory') === 'false'
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);
  });

  it('requests patient CSV export with includeHistory=true when passed', () => {
    const blob = new Blob(['a,b'], { type: 'text/csv' });

    service.exportPatientCsv('p1', true).subscribe((result) => expect(result).toEqual(blob));

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/patients/p1/export/csv` && r.params.get('includeHistory') === 'true'
    );
    req.flush(blob);
  });

  it('requests patient PDF export with the correct includeHistory parameter', () => {
    const blob = new Blob(['%PDF'], { type: 'application/pdf' });

    service.exportPatientPdf('p1', true).subscribe((result) => expect(result).toEqual(blob));

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/patients/p1/export/pdf` && r.params.get('includeHistory') === 'true'
    );
    req.flush(blob);
  });

  it('requests visit CSV export from the correct endpoint', () => {
    const blob = new Blob(['a,b'], { type: 'text/csv' });

    service.exportVisitCsv('v1').subscribe((result) => expect(result).toEqual(blob));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/visits/v1/export/csv`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);
  });

  it('requests visit PDF export from the correct endpoint', () => {
    const blob = new Blob(['%PDF'], { type: 'application/pdf' });

    service.exportVisitPdf('v1').subscribe((result) => expect(result).toEqual(blob));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/visits/v1/export/pdf`);
    req.flush(blob);
  });
});
