import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PatientService } from '../patient.service';
import { PatientResponse } from '../patient.models';

/** Read-only Patient Profile / View screen at /patients/{id} (Module 2 plan §7, workflow §3.3). */
@Component({
  selector: 'app-patient-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './patient-profile.component.html',
  styleUrl: './patient-profile.component.scss'
})
export class PatientProfileComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);

  patient: PatientResponse | null = null;
  loading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.patientService.getById(id).subscribe({
      next: (patient) => {
        this.patient = patient;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load this patient.';
      }
    });
  }

  get ageDisplay(): string {
    if (!this.patient) {
      return '—';
    }
    if (this.patient.dateOfBirth) {
      const dob = new Date(this.patient.dateOfBirth);
      const age = Math.floor((Date.now() - dob.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
      return `${age} yrs (DOB: ${dob.toLocaleDateString()})`;
    }
    if (this.patient.approxAgeAtEntry != null) {
      return `${this.patient.approxAgeAtEntry} yrs (approx., entered as of ${
        this.patient.entryDate ? new Date(this.patient.entryDate).toLocaleDateString() : '—'
      })`;
    }
    return '—';
  }
}
