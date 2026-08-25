import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/patients/patient.service';
import { PATIENT_GENDERS, Patient } from '../../../core/patients/patients.models';

/**
 * Handles both create-mode (Add Patient, Increment 1) and edit-mode (Increment 2) in a single
 * component rather than a separate edit component, to avoid duplicating the form/validators and
 * risking create/edit drift — see Planning\02_Patient_Management_Plan.md §9a.6.
 *
 * Edit-mode is driven by an `id` route param: when present, the form loads the existing patient
 * via getById(), pre-populates, and submits via update() instead of create(), navigating back to
 * the Patient Detail view on success instead of the create-mode inline confirmation.
 */
@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly genders = PATIENT_GENDERS;

  readonly today = new Date().toISOString().substring(0, 10);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(20)]]
  });

  id: string | null = null;
  isEditMode = false;
  loading = false;
  notFound = false;
  loadError = false;

  submitting = false;
  errorMessage: string | null = null;
  createdPatient: Patient | null = null;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    this.isEditMode = this.id !== null;

    if (this.isEditMode && this.id) {
      this.loading = true;
      this.patientService.getById(this.id).subscribe({
        next: (patient) => {
          this.loading = false;
          this.form.patchValue({
            fullName: patient.fullName,
            dateOfBirth: patient.dateOfBirth,
            gender: patient.gender,
            phoneNumber: patient.phoneNumber
          });
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
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;

    const raw = this.form.getRawValue();
    const payload = {
      fullName: raw.fullName,
      dateOfBirth: raw.dateOfBirth,
      gender: raw.gender as 'Male' | 'Female' | 'Other',
      phoneNumber: raw.phoneNumber
    };

    const request$ =
      this.isEditMode && this.id ? this.patientService.update(this.id, payload) : this.patientService.create(payload);

    request$.subscribe({
      next: (patient) => {
        this.submitting = false;
        if (this.isEditMode && this.id) {
          this.router.navigate(['/patients', this.id]);
        } else {
          this.createdPatient = patient;
        }
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
