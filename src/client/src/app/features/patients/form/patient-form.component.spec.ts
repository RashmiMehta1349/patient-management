import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PatientFormComponent } from './patient-form.component';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

describe('PatientFormComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}) } } }
      ]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the component', () => {
    const fixture = TestBed.createComponent(PatientFormComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('marks the form invalid when required fields are empty', () => {
    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;

    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.fullName.hasError('required')).toBeTrue();
    expect(component.form.controls.dateOfBirth.hasError('required')).toBeTrue();
    expect(component.form.controls.gender.hasError('required')).toBeTrue();
    expect(component.form.controls.phoneNumber.hasError('required')).toBeTrue();
  });

  it('does not submit when the form is invalid', () => {
    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    const patientService = TestBed.inject(PatientService);
    const createSpy = spyOn(patientService, 'create');

    component.submit();

    expect(createSpy).not.toHaveBeenCalled();
    expect(component.form.controls.fullName.touched).toBeTrue();
  });

  it('shows a confirmation with the created patient on submit success', () => {
    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    const patientService = TestBed.inject(PatientService);
    const createdPatient: Patient = {
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
    spyOn(patientService, 'create').and.returnValue(of(createdPatient));

    component.form.setValue({
      fullName: 'Jane Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9876543210'
    });
    component.submit();

    expect(component.createdPatient).toEqual(createdPatient);
    expect(component.submitting).toBeFalse();
  });

  it('surfaces an error message on submit failure', () => {
    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'create').and.returnValue(
      throwError(() => ({ error: { message: 'Phone number is required.' } }))
    );

    component.form.setValue({
      fullName: 'Jane Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: ''
    });
    // Bypass Angular's own required validator to exercise the handler's error path directly,
    // mirroring a server-side validation failure the client didn't already catch.
    component.form.controls.phoneNumber.setValidators([]);
    component.form.controls.phoneNumber.updateValueAndValidity();
    component.submit();

    expect(component.errorMessage).toBe('Phone number is required.');
    expect(component.submitting).toBeFalse();
    expect(component.createdPatient).toBeNull();
  });
});

describe('PatientFormComponent (edit mode)', () => {
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

  function setup() {
    TestBed.configureTestingModule({
      imports: [PatientFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: String(testPatient.id) }) } } }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => httpMock.verify());

  it('pre-populates all four fields from a mocked getById response', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isEditMode).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.form.getRawValue()).toEqual({
      fullName: 'Jane Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9876543210'
    });
  });

  it('submit calls PatientService.update with the route id and form values, not create', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const updateSpy = spyOn(patientService, 'update').and.returnValue(of(testPatient));
    const createSpy = spyOn(patientService, 'create');

    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({
      fullName: 'Jane A. Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9999000000'
    });
    component.submit();

    expect(updateSpy).toHaveBeenCalledWith(testPatient.id, {
      fullName: 'Jane A. Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9999000000'
    });
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('on success navigates to /patients/{id}', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(patientService, 'update').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate');
    fixture.detectChanges();

    component.form.setValue({
      fullName: 'Jane A. Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9999000000'
    });
    component.submit();

    expect(navigateSpy).toHaveBeenCalledWith(['/patients', testPatient.id]);
  });

  it('on server-side validation error, displays the same inline error banner used by create-mode', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    spyOn(patientService, 'update').and.returnValue(throwError(() => ({ error: { message: 'Phone number is required.' } })));

    const fixture = TestBed.createComponent(PatientFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({
      fullName: 'Jane A. Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      countryCode: '+91',
      phoneNumber: '9999000000'
    });
    component.submit();

    expect(component.errorMessage).toBe('Phone number is required.');
    expect(component.submitting).toBeFalse();
  });

  it('Cancel link points at /patients/{id}, not /dashboard', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(PatientFormComponent);
    fixture.detectChanges();

    const cancelLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a.back-link');
    expect(cancelLink.getAttribute('href')).toBe(`/patients/${testPatient.id}`);
  });
});
