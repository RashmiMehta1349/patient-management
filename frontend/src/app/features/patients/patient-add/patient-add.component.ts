import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PatientService } from '../patient.service';
import { CreatePatientRequest, GENDER_OPTIONS, ValidationErrorResponse } from '../patient.models';

const PHONE_PATTERN = /^\+?[0-9][0-9\s-]{6,19}$/;

/** Register (Add) Patient screen at /patients/new (Module 2 plan §7, workflow §3.1). */
@Component({
  selector: 'app-patient-add',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './patient-add.component.html',
  styleUrl: './patient-add.component.scss'
})
export class PatientAddComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly genderOptions = GENDER_OPTIONS;

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

  submitting = false;
  errorMessage: string | null = null;
  fieldErrors: Record<string, string> = {};

  // Non-blocking duplicate-warning confirm dialog state (B5).
  showDuplicateConfirm = false;
  duplicatePatientCode: string | null = null;

  ngOnInit(): void {
    const prefillName = this.route.snapshot.queryParamMap.get('name');
    if (prefillName) {
      this.form.patchValue({ fullName: prefillName });
    }
  }

  get dobMode(): 'dob' | 'age' {
    return this.form.controls.dobMode.value;
  }

  setDobMode(mode: 'dob' | 'age'): void {
    this.form.patchValue({ dobMode: mode });
  }

  /** Cross-field validation mirroring server-side rules (required DOB-or-age, DOB not in future). */
  private validateAgeFields(): boolean {
    const { dobMode, dateOfBirth, approxAgeAtEntry } = this.form.getRawValue();
    this.fieldErrors = { ...this.fieldErrors };
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

  private buildRequest(): CreatePatientRequest {
    const raw = this.form.getRawValue();
    return {
      fullName: raw.fullName.trim(),
      dateOfBirth: raw.dobMode === 'dob' && raw.dateOfBirth ? raw.dateOfBirth : null,
      approxAgeAtEntry: raw.dobMode === 'age' ? raw.approxAgeAtEntry : null,
      gender: raw.gender as CreatePatientRequest['gender'],
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
    const raw = this.form.getRawValue();

    this.patientService.checkDuplicate(raw.fullName.trim(), raw.phoneNumber.trim()).subscribe({
      next: (duplicateCheck) => {
        if (duplicateCheck.possibleDuplicate) {
          this.submitting = false;
          this.duplicatePatientCode = duplicateCheck.existingPatientCode;
          this.showDuplicateConfirm = true;
          return;
        }
        this.save();
      },
      error: () => {
        // Advisory check failing shouldn't block registration; proceed to save directly.
        this.save();
      }
    });
  }

  confirmSaveDespiteDuplicate(): void {
    this.showDuplicateConfirm = false;
    this.submitting = true;
    this.save();
  }

  cancelDuplicateConfirm(): void {
    this.showDuplicateConfirm = false;
    this.submitting = false;
  }

  private save(): void {
    this.patientService.create(this.buildRequest()).subscribe({
      next: (patient) => {
        this.submitting = false;
        this.router.navigate(['/patients', patient.id]);
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
      this.errorMessage = 'Unable to save this patient right now. Please try again.';
    }
  }

  private toControlName(field: string): string {
    return field.length ? field.charAt(0).toLowerCase() + field.slice(1) : field;
  }
}
