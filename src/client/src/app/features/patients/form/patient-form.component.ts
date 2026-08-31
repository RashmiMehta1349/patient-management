import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/patients/patient.service';
import { PATIENT_GENDERS, Patient } from '../../../core/patients/patients.models';
import { COUNTRY_CODES } from '../../../core/patients/country-codes';
import { RecentPatientsService } from '../../../core/patients/recent-patients.service';

const PHONE_NUMBER_MAX_LENGTH = 10;

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
  templateUrl: './patient-form.component.html'
})
export class PatientFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly recentPatientsService = inject(RecentPatientsService);

  readonly genders = PATIENT_GENDERS;
  readonly countryCodes = COUNTRY_CODES;

  readonly today = new Date().toISOString().substring(0, 10);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['', [Validators.required]],
    countryCode: ['+91', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{1,10}$/)]]
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
            countryCode: patient.countryCode,
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
      countryCode: raw.countryCode,
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
          // Record newly registered patients as "recently viewed" (Module 7, §5) so they
          // surface on the Dashboard immediately, not only after a later profile visit.
          this.recentPatientsService.record({
            id: patient.id,
            fullName: patient.fullName,
            countryCode: patient.countryCode,
            phoneNumber: patient.phoneNumber
          });
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err?.error?.message ?? 'Could not save patient. Please check the form and try again.';
      }
    });
  }

  blockNonDigit(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }
    const navigationKeys = ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
    if (navigationKeys.includes(event.key)) {
      return;
    }
    if (!/^[0-9]$/.test(event.key)) {
      event.preventDefault();
    }
  }

  stripNonDigits(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digitsOnly = input.value.replace(/\D/g, '').slice(0, PHONE_NUMBER_MAX_LENGTH);
    if (digitsOnly !== input.value) {
      this.form.controls.phoneNumber.setValue(digitsOnly);
    }
  }

  registerAnother(): void {
    this.createdPatient = null;
    this.errorMessage = null;
    this.form.reset();
  }
}
