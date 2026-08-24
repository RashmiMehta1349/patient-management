import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PatientService } from '../patient.service';
import { PatientResponse } from '../patient.models';

/** Search/list screen at /patients (Module 2 plan §7). */
@Component({
  selector: 'app-patient-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './patient-search.component.html',
  styleUrl: './patient-search.component.scss'
})
export class PatientSearchComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    term: ['']
  });

  results: PatientResponse[] = [];
  recentPatients: PatientResponse[] = [];
  searched = false;
  loading = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.patientService.getRecent(10).subscribe({
      next: (patients) => (this.recentPatients = patients),
      error: () => {
        // Non-critical: recent-patients hook failing shouldn't block the search screen.
      }
    });
  }

  search(): void {
    const term = this.form.getRawValue().term.trim();
    if (!term) {
      this.results = [];
      this.searched = false;
      return;
    }

    this.loading = true;
    this.errorMessage = null;
    this.patientService.search(term).subscribe({
      next: (patients) => {
        this.results = patients;
        this.searched = true;
        this.loading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage = 'Unable to search patients right now. Please try again.';
      }
    });
  }

  openPatient(id: string): void {
    this.router.navigate(['/patients', id]);
  }

  registerWithSearchTerm(): void {
    const term = this.form.getRawValue().term.trim();
    this.router.navigate(['/patients/new'], { queryParams: term ? { name: term } : {} });
  }

  ageOrDob(patient: PatientResponse): string {
    if (patient.dateOfBirth) {
      const dob = new Date(patient.dateOfBirth);
      const age = Math.floor((Date.now() - dob.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
      return `${age} yrs`;
    }
    if (patient.approxAgeAtEntry != null) {
      return `${patient.approxAgeAtEntry} yrs (approx)`;
    }
    return '—';
  }
}
