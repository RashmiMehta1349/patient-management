import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { PatientsListComponent } from './patients-list.component';
import { PatientService } from '../../../core/patients/patient.service';
import { PagedResult, Patient } from '../../../core/patients/patients.models';

describe('PatientsListComponent', () => {
  const makePatient = (id: string, fullName: string): Patient => ({
    id,
    fullName,
    dateOfBirth: '1990-05-15',
    age: 36,
    gender: 'Female',
    phoneNumber: '555-123-4567',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  });

  const pagedResult = (items: Patient[], totalCount: number, page = 1, pageSize = 25): PagedResult<Patient> => ({
    items,
    totalCount,
    page,
    pageSize
  });

  function setup(queryParam?: string) {
    TestBed.configureTestingModule({
      imports: [PatientsListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParam ? { query: queryParam } : {}) },
            queryParamMap: of(convertToParamMap(queryParam ? { query: queryParam } : {}))
          }
        }
      ]
    });
  }

  it('renders a grid row per patient from a mocked list() response', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    const patients = [makePatient('1', 'Amy Baker'), makePatient('2', 'Zack Adams')];
    spyOn(patientService, 'list').and.returnValue(of(pagedResult(patients, 2)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('Add Patient button routes to /patients/new', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    fixture.detectChanges();

    const addLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.add-patient-button');
    expect(addLink.getAttribute('href')).toBe('/patients/new');
  });

  it('edit icon routes to /patients/{id}/edit', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    const patients = [makePatient('11111111-1111-1111-1111-111111111111', 'Amy Baker')];
    spyOn(patientService, 'list').and.returnValue(of(pagedResult(patients, 1)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    fixture.detectChanges();

    const editLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.edit-icon');
    expect(editLink.getAttribute('href')).toBe('/patients/11111111-1111-1111-1111-111111111111/edit');
  });

  it('patient name links to /patients/{id} (detail view)', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    const patients = [makePatient('11111111-1111-1111-1111-111111111111', 'Amy Baker')];
    spyOn(patientService, 'list').and.returnValue(of(pagedResult(patients, 1)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    fixture.detectChanges();

    const nameLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.patient-name-link');
    expect(nameLink.getAttribute('href')).toBe('/patients/11111111-1111-1111-1111-111111111111');
    expect(nameLink.textContent?.trim()).toBe('Amy Baker');
  });

  it('renders empty state with an Add Patient shortcut when there are zero patients', () => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.querySelector('.add-patient-link')).toBeTruthy();
  });

  it('search box re-invokes list() with the typed query, debounced, and resets to page 1', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    listSpy.calls.reset();

    component.page = 3;
    component.onSearchTermChange('Jane');
    tick(300);

    expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: 'Jane', page: 1 }));
  }));

  it('clearing the search box re-invokes list() with no query, reset to page 1', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

    const fixture = TestBed.createComponent(PatientsListComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    listSpy.calls.reset();

    component.onSearchTermChange('');
    tick(300);

    expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: undefined, page: 1 }));
  }));

  describe('deep-linked query param (Module 7 §8/§10 task 5)', () => {
    it('pre-populates the search box and fetches with the query when a `query` route param is present', () => {
      setup('Jane');
      const patientService = TestBed.inject(PatientService);
      const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      expect(component.searchTerm).toBe('Jane');
      expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: 'Jane', page: 1 }));
    });

    it('behaves identically to today (browse-all) when no `query` param is present', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      expect(component.searchTerm).toBe('');
      expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: undefined, page: 1 }));
    });
  });

  describe('successive query-param navigations to the same route instance (Module 7 code review fix)', () => {
    it('re-fetches with the second search term when the query param changes while the component instance persists', () => {
      const queryParamMap$ = new Subject<ReturnType<typeof convertToParamMap>>();
      TestBed.configureTestingModule({
        imports: [PatientsListComponent],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          {
            provide: ActivatedRoute,
            useValue: {
              snapshot: { queryParamMap: convertToParamMap({ query: 'foo' }) },
              queryParamMap: queryParamMap$.asObservable()
            }
          }
        ]
      });

      const patientService = TestBed.inject(PatientService);
      const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();
      queryParamMap$.next(convertToParamMap({ query: 'foo' }));
      fixture.detectChanges();

      expect(component.searchTerm).toBe('foo');
      expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: 'foo', page: 1 }));

      const barResults = [makePatient('9', 'Bar Result')];
      listSpy.and.returnValue(of(pagedResult(barResults, 1)));

      // Doctor runs a second global search and clicks "View all N results" for a different term
      // while this same PatientsListComponent instance stays alive (same route, query-only change).
      queryParamMap$.next(convertToParamMap({ query: 'bar' }));
      fixture.detectChanges();

      expect(component.searchTerm).toBe('bar');
      expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ query: 'bar', page: 1 }));
      const rows = fixture.nativeElement.querySelectorAll('tbody tr');
      expect(rows.length).toBe(1);
      expect(rows[0].textContent).toContain('Bar Result');
    });
  });

  describe('pagination', () => {
    it('Next button calls list() with page incremented and renders the new page items', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      const page1 = [makePatient('1', 'Patient A')];
      const page2 = [makePatient('2', 'Patient B')];
      const listSpy = spyOn(patientService, 'list').and.returnValue(of(pagedResult(page1, 30, 1, 25)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      fixture.detectChanges();

      listSpy.and.returnValue(of(pagedResult(page2, 30, 2, 25)));
      const nextButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('.pagination button')[1];
      nextButton.click();
      fixture.detectChanges();

      expect(listSpy).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2 }));
      const rows = fixture.nativeElement.querySelectorAll('tbody tr');
      expect(rows.length).toBe(1);
    });

    it('Previous is disabled on page 1', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient('1', 'A')], 30, 1, 25)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      fixture.detectChanges();

      const prevButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('.pagination button')[0];
      expect(prevButton.disabled).toBeTrue();
    });

    it('Next is disabled on the last page', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient('1', 'A')], 25, 1, 25)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      fixture.detectChanges();

      const nextButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('.pagination button')[1];
      expect(nextButton.disabled).toBeTrue();
    });

    it('Page X of Y label reflects page and computed total pages', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient('1', 'A')], 51, 2, 25)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      fixture.detectChanges();

      const label: HTMLElement = fixture.nativeElement.querySelector('.pagination span');
      expect(label.textContent).toContain('Page 2 of 3');
    });

    it('renders empty-row state for a page beyond the total without erroring', () => {
      setup();
      const patientService = TestBed.inject(PatientService);
      spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 30, 3, 25)));

      const fixture = TestBed.createComponent(PatientsListComponent);
      expect(() => fixture.detectChanges()).not.toThrow();
      const rows = fixture.nativeElement.querySelectorAll('tbody tr');
      expect(rows.length).toBe(0);
    });
  });
});
