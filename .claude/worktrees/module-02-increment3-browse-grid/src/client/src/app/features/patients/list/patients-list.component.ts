import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

const PAGE_SIZE = 25;
const SEARCH_DEBOUNCE_MS = 300;

/**
 * Browse-all patient grid, revised from the original list/search component (Increment 3
 * revision, §9b.3). Default data source is the paginated browse-all call; an optional search
 * box layered on top calls the same service method with a `query` param and resets to page 1.
 */
@Component({
  selector: 'app-patients-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './patients-list.component.html',
  styleUrl: './patients-list.component.scss'
})
export class PatientsListComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly searchTermChanged = new Subject<string>();

  patients: Patient[] = [];
  loading = true;
  error = false;

  searchTerm = '';
  page = 1;
  totalCount = 0;
  readonly pageSize = PAGE_SIZE;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.totalCount === 0;
  }

  ngOnInit(): void {
    this.searchTermChanged.pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()).subscribe(() => {
      this.page = 1;
      this.fetch();
    });

    this.fetch();
  }

  onSearchTermChange(value: string): void {
    this.searchTerm = value;
    this.searchTermChanged.next(value);
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page -= 1;
      this.fetch();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page += 1;
      this.fetch();
    }
  }

  retry(): void {
    this.fetch();
  }

  private fetch(): void {
    this.loading = true;
    this.error = false;

    this.patientService
      .list({ query: this.searchTerm || undefined, page: this.page, pageSize: this.pageSize })
      .subscribe({
        next: (result) => {
          this.loading = false;
          this.patients = result.items;
          this.totalCount = result.totalCount;
          this.page = result.page;
        },
        error: () => {
          this.loading = false;
          this.error = true;
        }
      });
  }
}
