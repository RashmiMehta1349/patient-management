import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PatientFormComponent } from './patient-form.component';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

describe('PatientFormComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
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
      id: '11111111-1111-1111-1111-111111111111',
      fullName: 'Jane Doe',
      dateOfBirth: '1990-05-15',
      age: 36,
      gender: 'Female',
      phoneNumber: '555-123-4567',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
    spyOn(patientService, 'create').and.returnValue(of(createdPatient));

    component.form.setValue({
      fullName: 'Jane Doe',
      dateOfBirth: '1990-05-15',
      gender: 'Female',
      phoneNumber: '555-123-4567'
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
