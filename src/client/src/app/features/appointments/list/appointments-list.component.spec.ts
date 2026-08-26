import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AppointmentsListComponent } from './appointments-list.component';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { Appointment } from '../../../core/appointments/appointments.models';

describe('AppointmentsListComponent', () => {
  const makeAppointment = (id: number, time: string, patientName = 'Jane Doe'): Appointment => ({
    id,
    patientId: 1,
    patientName,
    appointmentDate: '2026-08-26',
    appointmentTime: time,
    status: 'Scheduled',
    notes: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    hasOverlapWarning: false,
    conflictingAppointments: []
  });

  function setup() {
    TestBed.configureTestingModule({
      imports: [AppointmentsListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
  }

  it('renders rows in time order from listByDate()', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'listByDate').and.returnValue(
      of([makeAppointment(1, '08:00'), makeAppointment(2, '10:30')])
    );

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('08:00');
    expect(rows[1].textContent).toContain('10:30');
  });

  it('renders empty state with an Add Appointment shortcut when there are none', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'listByDate').and.returnValue(of([]));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.querySelector('.add-appointment-link')).toBeTruthy();
  });

  it('patient name links to /patients/{patientId} (Module 7 cross-navigation gap closure)', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    spyOn(appointmentService, 'listByDate').and.returnValue(of([makeAppointment(1, '08:00', 'Amy Baker')]));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();

    const nameLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.patient-name-link');
    expect(nameLink).toBeTruthy();
    expect(nameLink.getAttribute('href')).toBe('/patients/1');
    expect(nameLink.textContent?.trim()).toBe('Amy Baker');
  });

  it('date navigation re-fetches with the shifted date', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    const listSpy = spyOn(appointmentService, 'listByDate').and.returnValue(of([]));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const initialDate = component.selectedDate;
    listSpy.calls.reset();

    component.nextDay();

    expect(listSpy).toHaveBeenCalled();
    expect(component.selectedDate).not.toBe(initialDate);
  });

  it('changing status via the control calls updateStatus and updates the row in place', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    const appointment = makeAppointment(1, '09:00');
    spyOn(appointmentService, 'listByDate').and.returnValue(of([appointment]));
    const updateSpy = spyOn(appointmentService, 'updateStatus').and.returnValue(
      of({ ...appointment, status: 'Completed' })
    );

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.onStatusChange(component.appointments[0], 'Completed');

    expect(updateSpy).toHaveBeenCalledWith(1, 'Completed');
    expect(component.appointments[0].status).toBe('Completed');
  });

  it('status update error surfaces inline and reverts the status without losing the row', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    const appointment = makeAppointment(1, '09:00');
    spyOn(appointmentService, 'listByDate').and.returnValue(of([appointment]));
    spyOn(appointmentService, 'updateStatus').and.returnValue(throwError(() => ({ status: 500 })));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.onStatusChange(component.appointments[0], 'Completed');

    expect(component.appointments[0].status).toBe('Scheduled');
    expect(component.statusUpdateErrorId).toBe(1);
    expect(component.appointments.length).toBe(1);
  });

  it('renders a "Start Consultation" link with the correct query params for a Scheduled row', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    const appointment = makeAppointment(1, '09:00');
    spyOn(appointmentService, 'listByDate').and.returnValue(of([appointment]));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.start-consultation-link');
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toBe('/consultations/new?patientId=1&appointmentId=1');
  });

  it('does not render "Start Consultation" for a non-Scheduled row', () => {
    setup();
    const appointmentService = TestBed.inject(AppointmentService);
    const appointment = { ...makeAppointment(1, '09:00'), status: 'Completed' as const };
    spyOn(appointmentService, 'listByDate').and.returnValue(of([appointment]));

    const fixture = TestBed.createComponent(AppointmentsListComponent);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('.start-consultation-link');
    expect(link).toBeFalsy();
  });
});
