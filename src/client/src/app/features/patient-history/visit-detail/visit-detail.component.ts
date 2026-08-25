import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { VisitService } from '../../../core/visits/visit.service';
import { Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';

/**
 * Module 6 (Patient History) — read-only Visit Detail view (Increment 2, plan §5/§8, Open Question 1
 * resolved in favor of a dedicated component rather than the edit form in a "view mode"). Renders a
 * past visit's vitals, complaints, diagnosis, and medications as static text: no form controls, no
 * submit action, no path back to a write operation (R8/AC2/AC6). "Print Prescription" reuses the
 * same PrescriptionService flow as the Consultations list and the Consultation form's own print
 * action; "Back" returns to the patient's Consultations/History list.
 */
@Component({
  selector: 'app-visit-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './visit-detail.component.html',
  styleUrl: './visit-detail.component.scss'
})
export class VisitDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly visitService = inject(VisitService);
  private readonly prescriptionService = inject(PrescriptionService);

  id: string | null = null;
  loading = true;
  notFound = false;
  error = false;
  visit: Visit | null = null;

  printing = false;
  printError = false;

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

    this.visitService.getById(id).subscribe({
      next: (visit) => {
        this.loading = false;
        this.visit = visit;
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

  backLink(): string[] {
    return this.visit ? ['/patients', this.visit.patientId] : ['/dashboard'];
  }

  /** Read-only action (R7 of the plan) — fetches the server-generated prescription PDF and opens
   * it in a new tab, falling back to a download if pop-ups are blocked. Writes nothing. */
  printPrescription(): void {
    if (this.printing || !this.visit) {
      return;
    }
    this.printing = true;
    this.printError = false;

    const visitId = this.visit.id;
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
