import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { Appointment } from '../../../core/appointments/appointments.models';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';
import { Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';
import { RecentPatientsService } from '../../../core/patients/recent-patients.service';
import { DataExportService } from '../../../core/data-export/data-export.service';
import { triggerDownload } from '../../../core/shared/download/trigger-download';

/**
 * Read-only Patient Profile view (Increment 2), extended in Increment 3 to wire the previously
 * disabled "Appointments" placeholder to a real patient-scoped appointment list
 * (approved plan §7/§9 task 24, AC10), and in Module 4 to wire the "Consultations" placeholder to
 * a real patient-scoped visit list (approved plan §7, AC8). Module 6 (Patient History) extends this
 * same Consultations section in place with a date-range filter and a Complaints line, and removes
 * the dead "History (coming soon)" placeholder — history is not a separate destination (plan §5/§8).
 * No delete action anywhere on this screen per the approved plan (§9a.7, AC6).
 */
@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly patientService = inject(PatientService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly visitService = inject(VisitService);
  private readonly prescriptionService = inject(PrescriptionService);
  private readonly recentPatientsService = inject(RecentPatientsService);
  private readonly dataExportService = inject(DataExportService);

  id: string | null = null;
  loading = true;
  notFound = false;
  error = false;
  patient: Patient | null = null;

  appointments: Appointment[] = [];
  appointmentsLoading = true;
  appointmentsError = false;

  visits: Visit[] = [];
  visitsLoading = true;
  visitsError = false;

  /** Module 6 (Patient History) — date-range filter state (native `<input type="date">` values,
   * 'yyyy-MM-dd' or empty string). Filtering is server-side (§5 of the plan): changing either value
   * re-fetches from the extended GET /api/visits endpoint rather than filtering client-side. */
  fromDate = '';
  toDate = '';

  printingVisitId: number | null = null;

  /** Module 8 (Data Export) — patient-level export state (plan §8). includeHistory defaults off,
   * matching the server's default (§5 Open Question 3): opt-in via this checkbox. */
  includeHistoryInExport = false;
  exportingPatient = false;
  exportError = false;

  ngOnInit(): void {
    // Angular reuses this component instance across patients when navigating between
    // /patients/:id routes (e.g. via the Module 7 global search widget), so `id` must be read
    // reactively from paramMap rather than once from the route snapshot — otherwise the screen
    // keeps showing the previously loaded patient's data.
    this.route.paramMap.subscribe((params) => {
      this.id = params.get('id');
      if (!this.id) {
        this.notFound = true;
        this.loading = false;
        return;
      }

      // Reset per-patient state (Module 6 date filter) so a stale filter from the previous
      // patient doesn't silently apply to the newly selected one.
      this.fromDate = '';
      this.toDate = '';

      this.load(this.id);
      this.loadAppointments(this.id);
      this.loadVisits(this.id);
    });
  }

  private loadVisits(patientId: string): void {
    this.visitsLoading = true;
    this.visitsError = false;

    this.visitService.listByPatientId(patientId, this.fromDate || undefined, this.toDate || undefined).subscribe({
      next: (visits) => {
        this.visitsLoading = false;
        this.visits = visits;
      },
      error: () => {
        this.visitsLoading = false;
        this.visitsError = true;
      }
    });
  }

  /** True while either From or To is set — distinguishes the "no visits in range" empty state
   * from the "patient has no consultations at all" empty state. */
  get isFiltered(): boolean {
    return !!(this.fromDate || this.toDate);
  }

  /** Re-fetches the visit list from the server whenever the From/To filter changes (Module 6). */
  onFilterChange(): void {
    if (this.id) {
      this.loadVisits(this.id);
    }
  }

  clearFilter(): void {
    this.fromDate = '';
    this.toDate = '';
    this.onFilterChange();
  }

  private loadAppointments(patientId: string): void {
    this.appointmentsLoading = true;
    this.appointmentsError = false;

    this.appointmentService.listByPatientId(patientId).subscribe({
      next: (appointments) => {
        this.appointmentsLoading = false;
        this.appointments = appointments;
      },
      error: () => {
        this.appointmentsLoading = false;
        this.appointmentsError = true;
      }
    });
  }

  private load(id: string): void {
    this.loading = true;
    this.notFound = false;
    this.error = false;

    this.patientService.getById(id).subscribe({
      next: (patient) => {
        this.loading = false;
        this.patient = patient;
        // Module 7 (Search & Navigation) — single recording point for "recently viewed" (§5):
        // recorded here regardless of how the doctor arrived at this profile.
        this.recentPatientsService.record({
          id: patient.id,
          fullName: patient.fullName,
          phoneNumber: patient.phoneNumber
        });
      },
      error: (err) => {
        this.loading = false;
        if (err?.status === 404) {
          this.notFound = true;
        } else {
          this.error = true;
        }
      }
    });
  }

  retry(): void {
    if (this.id) {
      this.load(this.id);
    }
  }

  /** Fetches the server-generated prescription PDF for a given visit and opens it in a new tab —
   * read-only action, mirrors ConsultationFormComponent.printPrescription. */
  printPrescription(visitId: number): void {
    if (this.printingVisitId) {
      return;
    }
    this.printingVisitId = visitId;

    this.prescriptionService.getPrescriptionPdf(visitId).subscribe({
      next: (blob) => {
        this.printingVisitId = null;
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
        this.printingVisitId = null;
      }
    });
  }

  /** Read-only action (R7) — downloads this patient's export file (Module 8). Scoped strictly to
   * this one patient's own data; includeHistoryInExport controls whether a summarized visit
   * history section is appended (server default: off). */
  exportPatientCsv(): void {
    if (this.exportingPatient || !this.patient) {
      return;
    }
    this.exportingPatient = true;
    this.exportError = false;
    const patientId = this.patient.id;

    this.dataExportService.exportPatientCsv(patientId, this.includeHistoryInExport).subscribe({
      next: (blob) => {
        this.exportingPatient = false;
        triggerDownload(blob, `patient-${patientId}-export.csv`);
      },
      error: () => {
        this.exportingPatient = false;
        this.exportError = true;
      }
    });
  }

  exportPatientPdf(): void {
    if (this.exportingPatient || !this.patient) {
      return;
    }
    this.exportingPatient = true;
    this.exportError = false;
    const patientId = this.patient.id;

    this.dataExportService.exportPatientPdf(patientId, this.includeHistoryInExport).subscribe({
      next: (blob) => {
        this.exportingPatient = false;
        triggerDownload(blob, `patient-${patientId}-export.pdf`);
      },
      error: () => {
        this.exportingPatient = false;
        this.exportError = true;
      }
    });
  }
}
