import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PatientDetailComponent } from './patient-detail.component';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';

describe('PatientDetailComponent', () => {
  let httpMock: HttpTestingController;

  const testPatient: Patient = {
    id: '11111111-1111-1111-1111-111111111111',
    fullName: 'Jane Doe',
    dateOfBirth: '1990-05-15',
    age: 36,
    gender: 'Female',
    phoneNumber: '555-123-4567',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  function setup(routeId = testPatient.id) {
    TestBed.configureTestingModule({
      imports: [PatientDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: routeId }) } }
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
          useValue: { snapshot: { paramMap: convertToParamMap({ id: testPatient.id }) } }
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
          id: 'a1',
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
          useValue: { snapshot: { paramMap: convertToParamMap({ id: testPatient.id }) } }
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
          id: 'v1',
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
          useValue: { snapshot: { paramMap: convertToParamMap({ id: testPatient.id }) } }
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
          id: 'v1',
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
});
