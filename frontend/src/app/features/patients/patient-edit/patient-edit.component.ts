import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PatientService } from '../patient.service';
import { GENDER_OPTIONS, UpdatePatientRequest, ValidationErrorResponse } from '../patient.models';

const PHONE_PATTERN = /^\+?[0-9][0-9\s-]{6,19}$/;

/** Edit Patient screen at /patients/{id}/edit (Module 2 plan §7, workflow §3.2). */
@Component({
  selector: 'app-patient-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './patient-edit.component.html',
  styleUrl: './patient-edit.component.scss'
})
export class PatientEditComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly genderOptions = GENDER_OPTIONS;
  patientId!: string;

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    dobMode: ['dob' as 'dob' | 'age'],
    dateOfBirth: [''],
    approxAgeAtEntry: [null as number | null],
    gender: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    email: [''],
    address: ['']
  });

  loading = true;
  submitting = false;
  errorMessage: string | null = null;
  fieldErrors: Record<string, string> = {};

  ngOnInit(): void {
    this.patientId = this.route.snapshot.paramMap.get('id')!;
    this.patientService.getById(this.patientId).subscribe({
      next: (patient) => {
        this.form.patchValue({
          fullName: patient.fullName,
          dobMode: patient.dateOfBirth ? 'dob' : 'age',
          dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.substring(0, 10) : '',
          approxAgeAtEntry: patient.approxAgeAtEntry,
          gender: patient.gender,
          phoneNumber: patient.phoneNumber,
          email: patient.email ?? '',
          address: patient.address ?? ''
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load this patient.';
      }
    });
  }

  get dobMode(): 'dob' | 'age' {
    return this.form.controls.dobMode.value;
  }

  setDobMode(mode: 'dob' | 'age'): void {
    this.form.patchValue({ dobMode: mode });
  }

  private validateAgeFields(): boolean {
    const { dobMode, dateOfBirth, approxAgeAtEntry } = this.form.getRawValue();
    delete this.fieldErrors['dateOfBirth'];
    delete this.fieldErrors['approxAgeAtEntry'];

    if (dobMode === 'dob') {
      if (!dateOfBirth) {
        this.fieldErrors['dateOfBirth'] = 'Date of birth is required.';
        return false;
      }
      if (new Date(dateOfBirth) > new Date()) {
        this.fieldErrors['dateOfBirth'] = 'Date of birth cannot be in the future.';
        return false;
      }
      return true;
    }

    if (approxAgeAtEntry == null || approxAgeAtEntry < 0 || approxAgeAtEntry > 150) {
      this.fieldErrors['approxAgeAtEntry'] = 'Enter a valid age (0-150).';
      return false;
    }
    return true;
  }

  private buildRequest(): UpdatePatientRequest {
    const raw = this.form.getRawValue();
    return {
      fullName: raw.fullName.trim(),
      dateOfBirth: raw.dobMode === 'dob' && raw.dateOfBirth ? raw.dateOfBirth : null,
      approxAgeAtEntry: raw.dobMode === 'age' ? raw.approxAgeAtEntry : null,
      gender: raw.gender as UpdatePatientRequest['gender'],
      phoneNumber: raw.phoneNumber.trim(),
      email: raw.email?.trim() || null,
      address: raw.address?.trim() || null
    };
  }

  submit(): void {
    if (this.submitting) {
      return;
    }

    this.fieldErrors = {};
    const ageFieldsValid = this.validateAgeFields();

    if (this.form.invalid || !ageFieldsValid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.submitting = true;

    this.patientService.update(this.patientId, this.buildRequest()).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/patients', this.patientId]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting = false;
        this.applyServerErrors(err);
      }
    });
  }

  private applyServerErrors(err: HttpErrorResponse): void {
    const body = err.error as ValidationErrorResponse | undefined;
    if (body?.errors?.length) {
      const map: Record<string, string> = {};
      for (const fieldError of body.errors) {
        map[this.toControlName(fieldError.field)] = fieldError.message;
      }
      this.fieldErrors = map;
      this.errorMessage = body.message ?? 'Please correct the highlighted fields.';
    } else {
      this.errorMessage = 'Unable to save changes right now. Please try again.';
    }
  }

  private toControlName(field: string): string {
    return field.length ? field.charAt(0).toLowerCase() + field.slice(1) : field;
  }
}
