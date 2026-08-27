import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { PatientDetailComponent } from './patient-detail.component';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';
import { RecentPatientsService } from '../../../core/patients/recent-patients.service';
import { DataExportService } from '../../../core/data-export/data-export.service';

describe('PatientDetailComponent', () => {
  let httpMock: HttpTestingController;

  const testPatient: Patient = {
    id: 1,
    fullName: 'Jane Doe',
    dateOfBirth: '1990-05-15',
    age: 36,
    gender: 'Female',
    countryCode: '+91',
    phoneNumber: '9876543210',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  function setup(routeId: string = String(testPatient.id)) {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: routeId }) },
            paramMap: of(convertToParamMap({ id: routeId }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'listByPatientId').and.returnValue(of([]));
  }

  afterEach(() => httpMock.verify());

  it('renders loading then loaded state given a mocked getById observable', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.patient).toEqual(testPatient);
    expect(component.notFound).toBeFalse();
  });

  it('renders not-found state on a 404 error response', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.notFound).toBeTrue();
    expect(component.patient).toBeNull();
  });

  it('Edit link points at /patients/{id}/edit', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const editLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a[href*="edit"]');
    expect(editLink).toBeTruthy();
    expect(editLink.getAttribute('href')).toBe(`/patients/${testPatient.id}/edit`);
  });

  it('Appointments tab shows real appointment data instead of the disabled placeholder (AC10)', () => {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: of(convertToParamMap({ id: String(testPatient.id) }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(visitService, 'listByPatientId').and.returnValue(of([]));
    spyOn(appointmentService, 'listByPatientId').and.returnValue(
      of([
        {
          id: 101,
          patientId: testPatient.id,
          patientName: testPatient.fullName,
          appointmentDate: '2026-08-26',
          appointmentTime: '09:00',
          status: 'Scheduled',
          notes: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          hasOverlapWarning: false,
          conflictingAppointments: []
        }
      ])
    );

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Appointments (coming soon)');
    const rows = fixture.nativeElement.querySelectorAll('.appointments-list li');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('2026-08-26');
  });

  it('Consultations section shows real visit data instead of the disabled placeholder (AC8)', () => {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: of(convertToParamMap({ id: String(testPatient.id) }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    spyOn(visitService, 'listByPatientId').and.returnValue(
      of([
        {
          id: 201,
          patientId: testPatient.id,
          patientName: testPatient.fullName,
          appointmentId: null,
          visitDate: new Date('2026-08-25T10:00:00Z').toISOString(),
          temperatureValue: 98.6,
          temperatureNotRecorded: false,
          bloodPressureValue: '120/80',
          bloodPressureNotRecorded: false,
          pulseValue: 72,
          pulseNotRecorded: false,
          complaints: 'Cough',
          diagnosis: 'Bronchitis',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          medications: []
        }
      ])
    );

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Consultations (coming soon)');
    const rows = fixture.nativeElement.querySelectorAll('.consultations-list li');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Bronchitis');
  });

  it('Consultations section shows a medication count badge per visit row', () => {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: of(convertToParamMap({ id: String(testPatient.id) }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    spyOn(visitService, 'listByPatientId').and.returnValue(
      of([
        {
          id: 201,
          patientId: testPatient.id,
          patientName: testPatient.fullName,
          appointmentId: null,
          visitDate: new Date('2026-08-25T10:00:00Z').toISOString(),
          temperatureValue: null,
          temperatureNotRecorded: true,
          bloodPressureValue: null,
          bloodPressureNotRecorded: true,
          pulseValue: null,
          pulseNotRecorded: true,
          complaints: null,
          diagnosis: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          medications: [
            { name: 'Paracetamol', dosage: '500mg', frequency: 'Twice daily', duration: '5 days', instructions: 'After food' },
            { name: 'Cetirizine', dosage: '10mg', frequency: 'Once daily', duration: '3 days', instructions: 'At night' }
          ]
        }
      ])
    );

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('.medication-badge');
    expect(badge).toBeTruthy();
    expect(badge.textContent).toContain('2 medicines');
  });

  it('Consultations empty state renders correctly for a patient with none', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.no-consultations');
    expect(emptyState).toBeTruthy();
  });

  it('the dead "History (coming soon)" placeholder no longer appears anywhere (AC8)', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('History (coming soon)');
    expect(fixture.nativeElement.querySelector('.placeholder-nav')).toBeFalsy();
  });

  it('Consultations row shows a Complaints line when present (AC5)', () => {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: of(convertToParamMap({ id: String(testPatient.id) }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    spyOn(visitService, 'listByPatientId').and.returnValue(
      of([
        {
          id: 201,
          patientId: testPatient.id,
          patientName: testPatient.fullName,
          appointmentId: null,
          visitDate: new Date('2026-08-25T10:00:00Z').toISOString(),
          temperatureValue: 98.6,
          temperatureNotRecorded: false,
          bloodPressureValue: '120/80',
          bloodPressureNotRecorded: false,
          pulseValue: 72,
          pulseNotRecorded: false,
          complaints: 'Cough and fever',
          diagnosis: 'Bronchitis',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          medications: []
        }
      ])
    );

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const complaintsEl = fixture.nativeElement.querySelector('.visit-complaints');
    expect(complaintsEl).toBeTruthy();
    expect(complaintsEl.textContent).toContain('Cough and fever');
  });

  it('Consultations row links to the read-only visit detail route, not the edit form', () => {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: of(convertToParamMap({ id: String(testPatient.id) }))
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    spyOn(visitService, 'listByPatientId').and.returnValue(
      of([
        {
          id: 201,
          patientId: testPatient.id,
          patientName: testPatient.fullName,
          appointmentId: null,
          visitDate: new Date('2026-08-25T10:00:00Z').toISOString(),
          temperatureValue: null,
          temperatureNotRecorded: true,
          bloodPressureValue: null,
          bloodPressureNotRecorded: true,
          pulseValue: null,
          pulseNotRecorded: true,
          complaints: null,
          diagnosis: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          medications: []
        }
      ])
    );

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const rowLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.consultations-list li a');
    expect(rowLink.getAttribute('href')).toBe('/visits/201');

    const editLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.consultations-list li a.edit-visit-link');
    expect(editLink).toBeTruthy();
    expect(editLink.getAttribute('href')).toBe('/consultations/201/edit');
  });

  it('applying a date filter re-fetches visits with fromDate/toDate forwarded to the service', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const listSpy = visitService.listByPatientId as jasmine.Spy;
    listSpy.and.returnValue(of([]));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.fromDate = '2026-08-01';
    component.toDate = '2026-08-31';
    component.onFilterChange();

    expect(listSpy).toHaveBeenCalledWith(testPatient.id, '2026-08-01', '2026-08-31');
  });

  it('clearing the filter restores the full list', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const listSpy = visitService.listByPatientId as jasmine.Spy;
    listSpy.and.returnValue(of([]));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.fromDate = '2026-08-01';
    component.toDate = '2026-08-31';
    component.clearFilter();

    expect(component.fromDate).toBe('');
    expect(component.toDate).toBe('');
    expect(listSpy).toHaveBeenCalledWith(testPatient.id, undefined, undefined);
  });

  it('filtered-empty state shows the distinct "No visits found in the selected date range" message', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const listSpy = visitService.listByPatientId as jasmine.Spy;
    listSpy.and.returnValue(of([]));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.fromDate = '2026-08-01';
    component.toDate = '2026-08-31';
    component.onFilterChange();
    fixture.detectChanges();

    const filteredEmpty = fixture.nativeElement.querySelector('.no-consultations-filtered');
    expect(filteredEmpty).toBeTruthy();
    expect(filteredEmpty.textContent).toContain('No visits found in the selected date range');
    expect(fixture.nativeElement.querySelector('.no-consultations')).toBeFalsy();
  });

  it('records exactly one RecentPatientsService.record() call with the correct patient data on successful load', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    const recordSpy = spyOn(recentPatientsService, 'record');

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    expect(recordSpy).toHaveBeenCalledTimes(1);
    expect(recordSpy).toHaveBeenCalledWith({
      id: testPatient.id,
      fullName: testPatient.fullName,
      countryCode: testPatient.countryCode,
      phoneNumber: testPatient.phoneNumber
    });
  });

  it('a failed load (404) does not record anything as recently viewed', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(throwError(() => ({ status: 404 })));
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    const recordSpy = spyOn(recentPatientsService, 'record');

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    expect(recordSpy).not.toHaveBeenCalled();
  });

  it('re-loads patient, appointments and visits (and records recently-viewed) when the route id changes without recreating the component (Module 7 global search widget hand-off)', () => {
    const patientB: Patient = {
      id: 2,
      fullName: 'John Smith',
      dateOfBirth: '1980-02-10',
      age: 46,
      gender: 'Male',
      countryCode: '+91',
      phoneNumber: '9876543210',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };

    const paramMap$ = new Subject<ReturnType<typeof convertToParamMap>>();

    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) },
            paramMap: paramMap$.asObservable()
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);

    const patientService = TestBed.inject(PatientService);
    const appointmentService = TestBed.inject(AppointmentService);
    const visitService = TestBed.inject(VisitService);
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    const recordSpy = spyOn(recentPatientsService, 'record');

    const getByIdSpy = spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const appointmentsSpy = spyOn(appointmentService, 'listByPatientId').and.returnValue(of([]));
    const visitsSpy = spyOn(visitService, 'listByPatientId').and.returnValue(of([]));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    paramMap$.next(convertToParamMap({ id: String(testPatient.id) }));
    fixture.detectChanges();

    expect(component.patient).toEqual(testPatient);
    expect(recordSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({ id: testPatient.id })
    );

    // Simulate the doctor selecting Patient B from the global search widget while this
    // component instance is reused (same route, id-only change).
    getByIdSpy.and.returnValue(of(patientB));
    appointmentsSpy.and.returnValue(
      of([
        {
          id: 102,
          patientId: patientB.id,
          patientName: patientB.fullName,
          appointmentDate: '2026-08-27',
          appointmentTime: '10:00',
          status: 'Scheduled',
          notes: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          hasOverlapWarning: false,
          conflictingAppointments: []
        }
      ])
    );
    visitsSpy.and.returnValue(
      of([
        {
          id: 202,
          patientId: patientB.id,
          patientName: patientB.fullName,
          appointmentId: null,
          visitDate: new Date('2026-08-26T10:00:00Z').toISOString(),
          temperatureValue: null,
          temperatureNotRecorded: true,
          bloodPressureValue: null,
          bloodPressureNotRecorded: true,
          pulseValue: null,
          pulseNotRecorded: true,
          complaints: null,
          diagnosis: 'Flu',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          medications: []
        }
      ])
    );

    paramMap$.next(convertToParamMap({ id: String(patientB.id) }));
    fixture.detectChanges();

    expect(component.id).toBe(String(patientB.id));
    expect(component.patient).toEqual(patientB);
    expect(component.appointments.length).toBe(1);
    expect(component.appointments[0].patientId).toBe(patientB.id);
    expect(component.visits.length).toBe(1);
    expect(component.visits[0].patientId).toBe(patientB.id);
    expect(recordSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({ id: patientB.id, fullName: patientB.fullName })
    );
    expect(getByIdSpy).toHaveBeenCalledWith(patientB.id);
    expect(appointmentsSpy).toHaveBeenCalledWith(patientB.id);
    expect(visitsSpy).toHaveBeenCalledWith(patientB.id, undefined, undefined);
  });

  it('"Export CSV" calls DataExportService with includeHistory=false by default', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const dataExportService = TestBed.inject(DataExportService);
    const exportSpy = spyOn(dataExportService, 'exportPatientCsv').and.returnValue(of(new Blob(['csv'])));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const exportButton: HTMLButtonElement = fixture.nativeElement.querySelector('.export-csv-btn');
    exportButton.click();

    expect(exportSpy).toHaveBeenCalledWith(testPatient.id, false);
  });

  it('"Export PDF" forwards includeHistory=true when the checkbox is checked', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const dataExportService = TestBed.inject(DataExportService);
    const exportSpy = spyOn(dataExportService, 'exportPatientPdf').and.returnValue(of(new Blob(['%PDF'])));
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');

    const fixture = TestBed.createComponent(PatientDetailComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.includeHistoryInExport = true;
    const exportButton: HTMLButtonElement = fixture.nativeElement.querySelector('.export-pdf-btn');
    exportButton.click();

    expect(exportSpy).toHaveBeenCalledWith(testPatient.id, true);
  });

  it('renders an error banner when patient export fails', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const dataExportService = TestBed.inject(DataExportService);
    spyOn(dataExportService, 'exportPatientCsv').and.returnValue(throwError(() => ({ status: 500 })));

    const fixture = TestBed.createComponent(PatientDetailComponent);
    fixture.detectChanges();

    const exportButton: HTMLButtonElement = fixture.nativeElement.querySelector('.export-csv-btn');
    exportButton.click();
    fixture.detectChanges();

    const banners: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.banner-error'));
    expect(banners.some((b) => b.textContent?.includes('Could not export'))).toBeTrue();
  });
});
