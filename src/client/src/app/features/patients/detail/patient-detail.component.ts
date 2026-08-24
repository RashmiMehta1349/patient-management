import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

/**
 * Read-only Patient Profile view (Increment 2). Renders placeholder navigation anchors for
 * Appointments/Consultations/History (Modules 3/4/6) — not wired to real data yet. No delete
 * action anywhere on this screen per the approved plan (§9a.7, AC6).
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

  id: string | null = null;
  loading = true;
  notFound = false;
  error = false;
  patient: Patient | null = null;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    if (!this.id) {
      this.notFound = true;
      this.loading = false;
      return;
    }

    this.load(this.id);
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
}
