import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { VisitDetailComponent } from './visit-detail.component';
import { VisitService } from '../../../core/visits/visit.service';
import { Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';

describe('VisitDetailComponent', () => {
  const baseVisit: Visit = {
    id: 'v1',
    patientId: 'p1',
    patientName: 'Jane Doe',
    appointmentId: null,
    visitDate: new Date('2026-08-10T10:00:00Z').toISOString(),
    temperatureValue: 98.6,
    temperatureNotRecorded: false,
    bloodPressureValue: '120/80',
    bloodPressureNotRecorded: false,
    pulseValue: 72,
    pulseNotRecorded: false,
    complaints: 'Fever',
    diagnosis: 'Viral infection',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    medications: [{ name: 'Paracetamol', dosage: '500mg', frequency: 'Twice daily', duration: '5 days', instructions: 'After food' }]
  };

  function setup(id = 'v1') {
    TestBed.configureTestingModule({
      imports: [VisitDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id }) } }
        }
      ]
    });
  }

  it('renders vitals, complaints, diagnosis, and medications for a full visit', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of(baseVisit));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('98.6');
    expect(text).toContain('120/80');
    expect(text).toContain('72');
    expect(text).toContain('Fever');
    expect(text).toContain('Viral infection');
    expect(text).toContain('Paracetamol');
  });

  it('renders "Not recorded" for vitals explicitly marked not recorded', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(
      of({
        ...baseVisit,
        temperatureValue: null,
        temperatureNotRecorded: true,
        bloodPressureValue: null,
        bloodPressureNotRecorded: true,
        pulseValue: null,
        pulseNotRecorded: true
      })
    );

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const notRecordedCount = (fixture.nativeElement.textContent.match(/Not recorded/g) || []).length;
    expect(notRecordedCount).toBe(3);
  });

  it('renders a "No medications prescribed" empty state for a visit with zero medications', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of({ ...baseVisit, medications: [] }));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.no-medications');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No medications prescribed');
  });

  it('has no editable control or submit action anywhere on the page (R8/AC6)', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of(baseVisit));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('input, textarea, select, form').length).toBe(0);
    expect(fixture.nativeElement.querySelector('button[type="submit"]')).toBeFalsy();
  });

  it('renders a distinct not-found state for an unknown visit id', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.notFound).toBeTrue();
  });

  it('"Back" link routes to the visit\'s patient profile', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of(baseVisit));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const backLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a.back-link');
    expect(backLink.getAttribute('href')).toBe('/patients/p1');
  });

  it('"Edit" link routes to the Module 4 edit form for this visit', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of(baseVisit));

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const editLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a.edit-link');
    expect(editLink).toBeTruthy();
    expect(editLink.getAttribute('href')).toBe('/consultations/v1/edit');
  });

  it('"Print Prescription" invokes PrescriptionService for this visit', () => {
    setup();
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'getById').and.returnValue(of(baseVisit));
    const prescriptionService = TestBed.inject(PrescriptionService);
    const printSpy = spyOn(prescriptionService, 'getPrescriptionPdf').and.returnValue(of(new Blob(['pdf'])));
    spyOn(window, 'open').and.returnValue({} as Window);
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');

    const fixture = TestBed.createComponent(VisitDetailComponent);
    fixture.detectChanges();

    const printButton: HTMLButtonElement = fixture.nativeElement.querySelector('.print-link');
    printButton.click();

    expect(printSpy).toHaveBeenCalledWith('v1');
  });
});
