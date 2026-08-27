import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { Appointment } from '../../../core/appointments/appointments.models';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

const SEARCH_DEBOUNCE_MS = 300;

/**
 * Handles both create-mode (Schedule Appointment, Increment 1) and edit-mode (reschedule,
 * Increment 3) in a single component — mirrors PatientFormComponent's single-component-two-modes
 * pattern (approved plan §7).
 *
 * Entry points: from the Daily List ("Add Appointment", patient chosen via the picker below), or
 * from Patient Detail (patientId route/query param, patient pre-selected and picker disabled).
 * Edit-mode is driven by an `id` route param: loads the existing appointment via getById(),
 * pre-populates, and submits via update() instead of create().
 */
@Component({
  selector: 'app-appointment-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './appointment-form.component.html'
})
export class AppointmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly appointmentService = inject(AppointmentService);
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly patientSearchChanged = new Subject<string>();

  readonly form = this.fb.nonNullable.group({
    appointmentDate: ['', [Validators.required]],
    appointmentTime: ['', [Validators.required]],
    notes: ['']
  });

  /** Local (not UTC) today's date, 'yyyy-MM-dd' — bound as the date input's `min` so past dates
   * can't be picked in the UI. The server re-validates this regardless (AppointmentValidation). */
  readonly minDate = toLocalIsoDate(new Date());

  id: string | null = null;
  isEditMode = false;
  loading = false;
  notFound = false;
  loadError = false;

  submitting = false;
  errorMessage: string | null = null;
  savedAppointment: Appointment | null = null;

  selectedPatient: Patient | null = null;
  patientLocked = false;
  patientSearchTerm = '';
  patientResults: Patient[] = [];
  patientTouched = false;

  ngOnInit(): void {
    this.patientSearchChanged.pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()).subscribe((term) => {
      if (!term) {
        this.patientResults = [];
        return;
      }
      this.patientService.list({ query: term, page: 1, pageSize: 10 }).subscribe({
        next: (result) => (this.patientResults = result.items),
        error: () => (this.patientResults = [])
      });
    });

    this.id = this.route.snapshot.paramMap.get('id');
    this.isEditMode = this.id !== null;

    const patientIdParam = this.route.snapshot.queryParamMap.get('patientId');

    if (this.isEditMode && this.id) {
      this.loading = true;
      this.appointmentService.getById(this.id).subscribe({
        next: (appointment) => {
          this.loading = false;
          this.form.patchValue({
            appointmentDate: appointment.appointmentDate,
            appointmentTime: appointment.appointmentTime,
            notes: appointment.notes ?? ''
          });
          this.selectedPatient = {
            id: appointment.patientId,
            fullName: appointment.patientName
          } as Patient;
          this.patientLocked = true;
        },
        error: (err) => {
          this.loading = false;
          if (err?.status === 404) {
            this.notFound = true;
          } else {
            this.loadError = true;
          }
        }
      });
    } else if (patientIdParam) {
      this.patientLocked = true;
      this.patientService.getById(patientIdParam).subscribe({
        next: (patient) => (this.selectedPatient = patient),
        error: () => {
          this.loadError = true;
        }
      });
    }
  }

  onPatientSearchChange(value: string): void {
    this.patientSearchTerm = value;
    this.patientSearchChanged.next(value);
  }

  selectPatient(patient: Patient): void {
    this.selectedPatient = patient;
    this.patientResults = [];
    this.patientSearchTerm = '';
  }

  clearSelectedPatient(): void {
    if (this.patientLocked) {
      return;
    }
    this.selectedPatient = null;
  }

  submit(): void {
    this.patientTouched = true;

    if (this.form.invalid || !this.selectedPatient || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;

    const raw = this.form.getRawValue();

    if (this.isEditMode && this.id) {
      this.appointmentService
        .update(this.id, {
          appointmentDate: raw.appointmentDate,
          appointmentTime: raw.appointmentTime,
          notes: raw.notes || undefined
        })
        .subscribe({
          next: () => {
            this.submitting = false;
            this.router.navigate(['/appointments'], { queryParams: { date: raw.appointmentDate } });
          },
          error: (err) => {
            this.submitting = false;
            this.errorMessage = err?.error?.message ?? 'Could not save appointment. Please check the form and try again.';
          }
        });
      return;
    }

    this.appointmentService
      .create({
        patientId: this.selectedPatient.id,
        appointmentDate: raw.appointmentDate,
        appointmentTime: raw.appointmentTime,
        notes: raw.notes || undefined
      })
      .subscribe({
        next: (appointment) => {
          this.submitting = false;
          this.savedAppointment = appointment;
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err?.error?.message ?? 'Could not save appointment. Please check the form and try again.';
        }
      });
  }

  scheduleAnother(): void {
    this.savedAppointment = null;
    this.errorMessage = null;
    this.form.reset();
    if (!this.patientLocked) {
      this.selectedPatient = null;
    }
    this.patientTouched = false;
  }
}

/** Local date parts (not toISOString(), which converts to UTC and can shift the calendar date
 * depending on the browser's timezone offset) — mirrors AppointmentsListComponent's toIsoDate. */
function toLocalIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
