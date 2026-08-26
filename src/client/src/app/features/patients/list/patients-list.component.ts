import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';

const DEFAULT_PAGE_SIZE = 10;
export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];
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
  private readonly route = inject(ActivatedRoute);
  private readonly searchTermChanged = new Subject<string>();

  patients: Patient[] = [];
  loading = true;
  error = false;

  searchTerm = '';
  page = 1;
  totalCount = 0;
  pageSize = DEFAULT_PAGE_SIZE;
  readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

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

    // Module 7 (Search & Navigation) — supports the global search widget's "View all N results"
    // hand-off (§8/§10 task 5): pre-populates the search box from an optional `query` route
    // param. Behaves identically to today when absent (regression-safe).
    //
    // Subscribed rather than read once from the snapshot: a second "View all" hand-off while this
    // component instance is still alive (same route, only the query param changes) does not
    // re-run ngOnInit, so reading the snapshot once would leave the grid showing the first
    // search's stale results.
    this.route.queryParamMap.subscribe((params) => {
      this.searchTerm = params.get('query') || '';
      this.page = 1;
      this.fetch();
    });
  }

  onSearchTermChange(value: string): void {
    this.searchTerm = value;
    this.searchTermChanged.next(value);
  }

  onPageSizeChange(value: number): void {
    this.pageSize = value;
    this.page = 1;
    this.fetch();
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
