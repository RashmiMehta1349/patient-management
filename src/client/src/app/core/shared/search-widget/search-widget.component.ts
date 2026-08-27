import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { PatientService } from '../../patients/patient.service';
import { Patient } from '../../patients/patients.models';
import { RecentPatientsService } from '../../patients/recent-patients.service';
import { RecentPatient } from '../../patients/recent-patients.models';

const SEARCH_DEBOUNCE_MS = 300;
const DROPDOWN_PAGE_SIZE = 8;

/**
 * Global patient search, reachable from the app shell header on every authenticated screen
 * (Module 7 — Search & Navigation, Increment 1, plan §8/§10 tasks 2-4). Reuses the existing
 * GET /api/patients?query= endpoint with a small pageSize, matching the debounced pattern
 * already used by patients-list.component. Shows Recently Viewed as the default (pre-keystroke)
 * content (Increment 2, plan §10 task 10).
 */
@Component({
  selector: 'app-search-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-widget.component.html'
})
export class SearchWidgetComponent {
  private readonly patientService = inject(PatientService);
  private readonly recentPatientsService = inject(RecentPatientsService);
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef);
  private readonly searchTermChanged = new Subject<string>();

  searchTerm = '';
  isOpen = false;
  loading = false;
  searched = false;
  results: Patient[] = [];
  totalCount = 0;
  highlightedIndex = -1;

  readonly dropdownPageSize = DROPDOWN_PAGE_SIZE;

  constructor() {
    this.searchTermChanged.pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()).subscribe((term) => {
      if (!term.trim()) {
        this.searched = false;
        this.results = [];
        this.totalCount = 0;
        this.loading = false;
        return;
      }

      this.loading = true;
      this.patientService.list({ query: term, page: 1, pageSize: this.dropdownPageSize }).subscribe({
        next: (result) => {
          this.loading = false;
          this.searched = true;
          this.results = result.items;
          this.totalCount = result.totalCount;
          this.highlightedIndex = -1;
        },
        error: () => {
          this.loading = false;
          this.searched = true;
          this.results = [];
          this.totalCount = 0;
        }
      });
    });
  }

  get recentPatients(): RecentPatient[] {
    return this.recentPatientsService.list();
  }

  get showRecent(): boolean {
    return !this.searchTerm.trim() && !this.loading;
  }

  get showViewAll(): boolean {
    return this.searched && this.totalCount > this.dropdownPageSize;
  }

  onFocus(): void {
    this.isOpen = true;
  }

  onInputChange(value: string): void {
    this.searchTerm = value;
    this.isOpen = true;
    this.searchTermChanged.next(value);
  }

  select(patient: Patient | RecentPatient): void {
    this.close();
    this.router.navigate(['/patients', patient.id]);
  }

  viewAllResults(): void {
    const term = this.searchTerm;
    this.close();
    this.router.navigate(['/patients'], { queryParams: { query: term } });
  }

  onKeydown(event: KeyboardEvent): void {
    const items = this.currentItems();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (items.length > 0) {
        this.highlightedIndex = (this.highlightedIndex + 1) % items.length;
      }
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (items.length > 0) {
        this.highlightedIndex = (this.highlightedIndex - 1 + items.length) % items.length;
      }
    } else if (event.key === 'Enter') {
      if (this.highlightedIndex >= 0 && this.highlightedIndex < items.length) {
        event.preventDefault();
        this.select(items[this.highlightedIndex]);
      }
    } else if (event.key === 'Escape') {
      this.close();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.close();
    }
  }

  private close(): void {
    this.isOpen = false;
    this.highlightedIndex = -1;
  }

  private currentItems(): (Patient | RecentPatient)[] {
    return this.showRecent ? this.recentPatients : this.results;
  }
}
