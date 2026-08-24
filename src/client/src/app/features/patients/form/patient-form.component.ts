import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PatientService } from '../../../core/patients/patient.service';
import { PATIENT_GENDERS, Patient } from '../../../core/patients/patients.models';

/**
 * Increment 1: create-mode only (Add Patient). Edit-mode (pre-populate + PUT) is added in
 * Increment 2 once GET/PUT /api/patients/{id} exist — see Planning\02_Patient_Management_Plan.md §9.
 *
 * There is no Patient Detail/Profile screen yet (also Increment 2), so on success this shows an
 * inline confirmation with the created patient's details plus a "Register another patient"
 * action, rather than navigating to a profile view that doesn't exist yet.
 */
@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);

  readonly genders = PATIENT_GENDERS;

  readonly today = new Date().toISOString().substring(0, 10);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(20)]]
  });

  submitting = false;
  errorMessage: string | null = null;
  createdPatient: Patient | null = null;

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;

    const raw = this.form.getRawValue();
    this.patientService
      .create({
        fullName: raw.fullName,
        dateOfBirth: raw.dateOfBirth,
        gender: raw.gender as 'Male' | 'Female' | 'Other',
        phoneNumber: raw.phoneNumber
      })
      .subscribe({
        next: (patient) => {
          this.submitting = false;
          this.createdPatient = patient;
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err?.error?.message ?? 'Could not save patient. Please check the form and try again.';
        }
      });
  }

  registerAnother(): void {
    this.createdPatient = null;
    this.errorMessage = null;
    this.form.reset();
  }
}
