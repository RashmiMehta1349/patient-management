import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PrescriptionService } from './prescription.service';
import { environment } from '../../../environments/environment';

describe('PrescriptionService', () => {
  let service: PrescriptionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PrescriptionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the PDF as a blob from the correct endpoint', () => {
    const blob = new Blob(['%PDF-1.4'], { type: 'application/pdf' });

    service.getPrescriptionPdf('v1').subscribe((result) => {
      expect(result).toEqual(blob);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/visits/v1/prescription/pdf`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);
  });
});
