import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PatientDetailComponent } from './patient-detail.component';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

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
});
