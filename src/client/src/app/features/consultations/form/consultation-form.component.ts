import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';
import { Medication, Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';

/**
 * Handles both create-mode (Record Consultation) and edit-mode (edit a saved consultation, built
 * in this pass per explicit product decision) in a single component — mirrors
 * AppointmentFormComponent's single-component-two-modes pattern.
 *
 * Patient is always route/context-derived — no patient picker here (unlike the Appointment form),
 * since a consultation is only ever reached from Patient Detail or an Appointment Daily List row.
 *
 * Each vital (Temperature/Blood Pressure/Pulse) is a value input + a "Not recorded" checkbox, kept
 * mutually exclusive in the UI: entering a value clears the checkbox, checking it clears/disables
 * the value input. Per BRD §Consultation Workflow ("Vitals Capture (Mandatory)") and
 * Modules\04 §5, every vital must be explicitly addressed — a value or a "Not recorded" check —
 * before the consultation can be saved; a single click on "Not recorded" satisfies this, so it is
 * never a hard block, just a require-an-action gate. Server-side (VisitValidation) additionally
 * normalizes any still-untouched field to Not recorded as a defense-in-depth safety net for
 * non-UI API callers, but the client is expected to enforce the explicit-address requirement.
 */
@Component({
  selector: 'app-consultation-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './consultation-form.component.html',
  styleUrl: './consultation-form.component.scss'
})
export class ConsultationFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly visitService = inject(VisitService);
  private readonly patientService = inject(PatientService);
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    temperatureValue: this.fb.control<number | null>(null),
    temperatureNotRecorded: this.fb.nonNullable.control(false),
    bloodPressureValue: this.fb.control<string | null>(null),
    bloodPressureNotRecorded: this.fb.nonNullable.control(false),
    pulseValue: this.fb.control<number | null>(null),
    pulseNotRecorded: this.fb.nonNullable.control(false),
    complaints: this.fb.nonNullable.control(''),
    diagnosis: this.fb.nonNullable.control(''),
    medications: this.fb.array<FormGroup>([])
  });

  get medications(): FormArray<FormGroup> {
    return this.form.controls.medications;
  }

  private createMedicationRow(medication?: Medication): FormGroup {
    return this.fb.group({
      name: this.fb.nonNullable.control(medication?.name ?? ''),
      dosage: this.fb.nonNullable.control(medication?.dosage ?? ''),
      frequency: this.fb.nonNullable.control(medication?.frequency ?? ''),
      duration: this.fb.nonNullable.control(medication?.duration ?? ''),
      instructions: this.fb.nonNullable.control(medication?.instructions ?? '')
    });
  }

  addMedicationRow(): void {
    this.medications.push(this.createMedicationRow());
  }

  removeMedicationRow(index: number): void {
    this.medications.removeAt(index);
  }

  /** A row is "touched" if any of its five fields is non-blank — used both to flag inline
   * validation errors and to decide whether a row is dropped or must be complete on submit
   * (resolved decision: any touched row needs all 5 fields before save succeeds). */
  isMedicationRowTouched(index: number): boolean {
    const raw = this.medications.at(index).getRawValue() as Medication;
    return !!(raw.name?.trim() || raw.dosage?.trim() || raw.frequency?.trim() || raw.duration?.trim() || raw.instructions?.trim());
  }

  isMedicationRowIncomplete(index: number): boolean {
    if (!this.isMedicationRowTouched(index)) {
      return false;
    }
    const raw = this.medications.at(index).getRawValue() as Medication;
    return !(raw.name?.trim() && raw.dosage?.trim() && raw.frequency?.trim() && raw.duration?.trim() && raw.instructions?.trim());
  }

  id: string | null = null;
  isEditMode = false;
  loading = false;
  notFound = false;
  loadError = false;

  submitting = false;
  errorMessage: string | null = null;
  savedVisit: Visit | null = null;
  showVitalsValidation = false;
  showMedicationValidation = false;
  printing = false;
  printError = false;

  patient: Patient | { id: string; fullName: string } | null = null;
  appointmentId: string | null = null;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    this.isEditMode = this.id !== null;

    this.setupMutualExclusivity(this.form.controls.temperatureValue, this.form.controls.temperatureNotRecorded);
    this.setupMutualExclusivity(this.form.controls.bloodPressureValue, this.form.controls.bloodPressureNotRecorded);
    this.setupMutualExclusivity(this.form.controls.pulseValue, this.form.controls.pulseNotRecorded);

    if (this.isEditMode && this.id) {
      this.loading = true;
      this.visitService.getById(this.id).subscribe({
        next: (visit) => {
          this.loading = false;
          this.appointmentId = visit.appointmentId;
          this.patient = { id: visit.patientId, fullName: visit.patientName };
          this.form.patchValue({
            temperatureValue: visit.temperatureValue,
            temperatureNotRecorded: visit.temperatureNotRecorded,
            bloodPressureValue: visit.bloodPressureValue,
            bloodPressureNotRecorded: visit.bloodPressureNotRecorded,
            pulseValue: visit.pulseValue,
            pulseNotRecorded: visit.pulseNotRecorded,
            complaints: visit.complaints ?? '',
            diagnosis: visit.diagnosis ?? ''
          });
          this.medications.clear();
          for (const medication of visit.medications ?? []) {
            this.medications.push(this.createMedicationRow(medication));
          }
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
      return;
    }

    const patientId = this.route.snapshot.queryParamMap.get('patientId');
    this.appointmentId = this.route.snapshot.queryParamMap.get('appointmentId');

    if (!patientId) {
      this.loadError = true;
      return;
    }

    this.loading = true;
    this.patientService.getById(patientId).subscribe({
      next: (patient) => {
        this.loading = false;
        this.patient = patient;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  private setupMutualExclusivity(
    valueControl: { valueChanges: { subscribe(fn: (value: unknown) => void): void }; value: unknown; setValue(value: null, opts?: { emitEvent: boolean }): void },
    notRecordedControl: { valueChanges: { subscribe(fn: (value: unknown) => void): void }; value: unknown; setValue(value: boolean, opts?: { emitEvent: boolean }): void }
  ): void {
    valueControl.valueChanges.subscribe((value) => {
      if (value !== null && value !== '' && notRecordedControl.value) {
        notRecordedControl.setValue(false, { emitEvent: false });
      }
    });

    notRecordedControl.valueChanges.subscribe((checked) => {
      if (checked) {
        valueControl.setValue(null, { emitEvent: false });
      }
    });
  }

  /** True once the doctor has either entered a value or checked "Not recorded" for this vital. */
  isVitalAddressed(value: unknown, notRecorded: boolean): boolean {
    return notRecorded || (value !== null && value !== '');
  }

  get isTemperatureAddressed(): boolean {
    const raw = this.form.getRawValue();
    return this.isVitalAddressed(raw.temperatureValue, raw.temperatureNotRecorded);
  }

  get isBloodPressureAddressed(): boolean {
    const raw = this.form.getRawValue();
    return this.isVitalAddressed(raw.bloodPressureValue, raw.bloodPressureNotRecorded);
  }

  get isPulseAddressed(): boolean {
    const raw = this.form.getRawValue();
    return this.isVitalAddressed(raw.pulseValue, raw.pulseNotRecorded);
  }

  submit(): void {
    if (this.submitting || !this.patient) {
      return;
    }

    // BRD "Vitals Capture (Mandatory)": every vital must be explicitly addressed — a value or a
    // "Not recorded" check — before save. A single click resolves this, so it's never a hard block.
    if (!this.isTemperatureAddressed || !this.isBloodPressureAddressed || !this.isPulseAddressed) {
      this.showVitalsValidation = true;
      return;
    }

    // Any touched-but-incomplete medication row blocks save client-side (defense-in-depth
    // alongside the server's ValidateMedications rule); fully-blank rows are excluded silently.
    const hasIncompleteRow = this.medications.controls.some((_, index) => this.isMedicationRowIncomplete(index));
    if (hasIncompleteRow) {
      this.showMedicationValidation = true;
      return;
    }

    this.submitting = true;
    this.errorMessage = null;

    const raw = this.form.getRawValue();
    const medications = (raw.medications as Medication[])
      .filter((row) => row.name?.trim() || row.dosage?.trim() || row.frequency?.trim() || row.duration?.trim() || row.instructions?.trim())
      .map((row) => ({
        name: (row.name ?? '').trim(),
        dosage: (row.dosage ?? '').trim(),
        frequency: (row.frequency ?? '').trim(),
        duration: (row.duration ?? '').trim(),
        instructions: (row.instructions ?? '').trim()
      }));

    const vitals = {
      temperatureValue: raw.temperatureValue,
      temperatureNotRecorded: raw.temperatureNotRecorded,
      bloodPressureValue: raw.bloodPressureValue,
      bloodPressureNotRecorded: raw.bloodPressureNotRecorded,
      pulseValue: raw.pulseValue,
      pulseNotRecorded: raw.pulseNotRecorded,
      complaints: raw.complaints || undefined,
      diagnosis: raw.diagnosis || undefined,
      medications
    };

    if (this.isEditMode && this.id) {
      this.visitService.update(this.id, vitals).subscribe({
        next: (visit) => {
          this.submitting = false;
          this.savedVisit = visit;
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err?.error?.message ?? 'Could not save consultation. Please try again.';
        }
      });
      return;
    }

    this.visitService
      .create({
        patientId: this.patient.id,
        appointmentId: this.appointmentId ?? undefined,
        ...vitals
      })
      .subscribe({
        next: (visit) => {
          this.submitting = false;
          this.savedVisit = visit;
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err?.error?.message ?? 'Could not save consultation. Please try again.';
        }
      });
  }

  backLink(): string[] {
    return this.patient ? ['/patients', this.patient.id] : ['/dashboard'];
  }

  /** Fetches the server-generated prescription PDF and opens it in a new tab (falls back to
   * triggering a download if pop-ups are blocked) — read-only action, no data written. */
  printPrescription(visitId: string): void {
    if (this.printing) {
      return;
    }
    this.printing = true;
    this.printError = false;

    this.prescriptionService.getPrescriptionPdf(visitId).subscribe({
      next: (blob) => {
        this.printing = false;
        const url = window.URL.createObjectURL(blob);
        const opened = window.open(url, '_blank');
        if (!opened) {
          const link = document.createElement('a');
          link.href = url;
          link.download = `prescription-${visitId}.pdf`;
          link.click();
        }
        setTimeout(() => window.URL.revokeObjectURL(url), 30000);
      },
      error: () => {
        this.printing = false;
        this.printError = true;
      }
    });
  }
}
