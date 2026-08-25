import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { Appointment } from '../../../core/appointments/appointments.models';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';
import { Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';

/**
 * Read-only Patient Profile view (Increment 2), extended in Increment 3 to wire the previously
 * disabled "Appointments" placeholder to a real patient-scoped appointment list
 * (approved plan §7/§9 task 24, AC10), and in Module 4 to wire the "Consultations" placeholder to
 * a real patient-scoped visit list (approved plan §7, AC8). History remains a placeholder (Module 6).
 * No delete action anywhere on this screen per the approved plan (§9a.7, AC6).
 */
@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly patientService = inject(PatientService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly visitService = inject(VisitService);
  private readonly prescriptionService = inject(PrescriptionService);

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

  printingVisitId: string | null = null;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    if (!this.id) {
      this.notFound = true;
      this.loading = false;
      return;
    }

    this.load(this.id);
    this.loadAppointments(this.id);
    this.loadVisits(this.id);
  }

  private loadVisits(patientId: string): void {
    this.visitsLoading = true;
    this.visitsError = false;

    this.visitService.listByPatientId(patientId).subscribe({
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
  printPrescription(visitId: string): void {
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
}
