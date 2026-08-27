import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AppointmentFormComponent } from './appointment-form.component';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { Appointment } from '../../../core/appointments/appointments.models';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

describe('AppointmentFormComponent', () => {
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

  const savedAppointment: Appointment = {
    id: 101,
    patientId: 1,
    patientName: 'Jane Doe',
    appointmentDate: '2026-08-26',
    appointmentTime: '09:00',
    status: 'Scheduled',
    notes: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    hasOverlapWarning: false,
    conflictingAppointments: []
  };

  function setup(paramMap: Record<string, string> = {}, queryParamMap: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [AppointmentFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(paramMap),
              queryParamMap: convertToParamMap(queryParamMap)
            }
          }
        }
      ]
    });
  }

  it('validation: submit blocked and no patient selected shows a required error', () => {
    setup();
    const fixture = TestBed.createComponent(AppointmentFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.submit();
    fixture.detectChanges();

    expect(component.submitting).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Patient is required.');
  });

  it('submit success shows the overlap warning banner when the response flags it', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'create').and.returnValue(
      of({
        ...savedAppointment,
        hasOverlapWarning: true,
        conflictingAppointments: [{ id: 102, patientName: 'John Roe', appointmentTime: '09:15' }]
      })
    );

    const fixture = TestBed.createComponent(AppointmentFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.selectedPatient = testPatient;
    component.form.setValue({ appointmentDate: '2026-08-26', appointmentTime: '09:00', notes: '' });
    component.submit();
    fixture.detectChanges();

    expect(component.savedAppointment?.hasOverlapWarning).toBeTrue();
    const banner = fixture.nativeElement.querySelector('.banner-warning');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('John Roe');
  });

  it('submit error handling surfaces the server error message', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'create').and.returnValue(throwError(() => ({ error: { message: 'Patient not found.' } })));

    const fixture = TestBed.createComponent(AppointmentFormComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.selectedPatient = testPatient;
    component.form.setValue({ appointmentDate: '2026-08-26', appointmentTime: '09:00', notes: '' });
    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage).toBe('Patient not found.');
    expect(component.submitting).toBeFalse();
  });

  it('edit-mode pre-populates the form from getById and submits via update()', () => {
    setup({ id: 'a1' });
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'getById').and.returnValue(of(savedAppointment));
    const updateSpy = spyOn(appointmentService, 'update').and.returnValue(of(savedAppointment));

    const fixture = TestBed.createComponent(AppointmentFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.isEditMode).toBeTrue();
    expect(component.form.value.appointmentDate).toBe('2026-08-26');
    expect(component.patientLocked).toBeTrue();

    component.form.patchValue({ appointmentDate: '2026-08-28' });
    component.submit();

    expect(updateSpy).toHaveBeenCalledWith('a1', jasmine.objectContaining({ appointmentDate: '2026-08-28' }));
  });

  it('patientId query param pre-selects and locks the patient (from Patient Detail entry point)', () => {
    setup({}, { patientId: '1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(AppointmentFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.patientLocked).toBeTrue();
    expect(component.selectedPatient?.id).toBe(1);
  });
});
