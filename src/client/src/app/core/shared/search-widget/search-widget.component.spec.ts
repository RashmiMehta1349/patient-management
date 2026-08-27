import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { SearchWidgetComponent } from './search-widget.component';
import { PatientService } from '../../patients/patient.service';
import { RecentPatientsService } from '../../patients/recent-patients.service';
import { PagedResult, Patient } from '../../patients/patients.models';

describe('SearchWidgetComponent', () => {
  const makePatient = (id: number, fullName: string): Patient => ({
    id,
    fullName,
    dateOfBirth: '1990-05-15',
    age: 36,
    gender: 'Female',
    countryCode: '+91',
    phoneNumber: '9876543210',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  });

  const pagedResult = (items: Patient[], totalCount: number): PagedResult<Patient> => ({
    items,
    totalCount,
    page: 1,
    pageSize: 8
  });

  function setup() {
    TestBed.configureTestingModule({
      imports: [SearchWidgetComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
  }

  it('typing a partial term renders matching results within the debounce window', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(
      of(pagedResult([makePatient(1, 'Amy Baker')], 1))
    );

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('Amy');
    tick(300);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.dropdown-item');
    expect(items.length).toBe(1);
    expect(items[0].textContent).toContain('Amy Baker');
  }));

  it('a term with zero matches shows the "No patients found" empty state', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([], 0)));

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('zzz');
    tick(300);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.dropdown-empty');
    expect(emptyState.textContent).toContain('No patients found');
  }));

  it('clearing the input reverts to Recently Viewed default content', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient(1, 'Amy Baker')], 1)));
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    spyOn(recentPatientsService, 'list').and.returnValue([
      { id: 2, fullName: 'Zack Adams', countryCode: '+91', phoneNumber: '5550002', viewedAt: new Date().toISOString() }
    ]);

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('Amy');
    tick(300);
    component.onInputChange('');
    tick(300);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Zack Adams');
  }));

  it('"View all N results" appears only when totalCount exceeds the dropdown size and links to /patients?query=', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient(1, 'Amy Baker')], 10)));
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('Amy');
    tick(300);
    fixture.detectChanges();

    const viewAll: HTMLElement = fixture.nativeElement.querySelector('.dropdown-view-all');
    expect(viewAll).toBeTruthy();
    viewAll.click();

    expect(navigateSpy).toHaveBeenCalledWith(['/patients'], { queryParams: { query: 'Amy' } });
  }));

  it('does not show "View all" when totalCount is within the dropdown size', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient(1, 'Amy Baker')], 1)));

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('Amy');
    tick(300);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.dropdown-view-all')).toBeFalsy();
  }));

  it('selecting a result navigates to /patients/:id', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient(1, 'Amy Baker')], 1)));
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('Amy');
    tick(300);
    fixture.detectChanges();

    const item: HTMLElement = fixture.nativeElement.querySelector('.dropdown-item');
    item.click();

    expect(navigateSpy).toHaveBeenCalledWith(['/patients', '1']);
  }));

  it('arrow-key navigation plus Enter selects the highlighted result', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(
      of(pagedResult([makePatient(1, 'Amy Baker'), makePatient(2, 'Zack Adams')], 2))
    );
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onInputChange('a');
    tick(300);
    fixture.detectChanges();

    component.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    component.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    component.onKeydown(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(navigateSpy).toHaveBeenCalledWith(['/patients', '2']);
  }));

  it('Escape closes the dropdown', fakeAsync(() => {
    setup();
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'list').and.returnValue(of(pagedResult([makePatient(1, 'Amy Baker')], 1)));

    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    component.onFocus();

    component.onKeydown(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(component.isOpen).toBeFalse();
  }));

  it('click-outside closes the dropdown', () => {
    setup();
    const fixture = TestBed.createComponent(SearchWidgetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    component.onFocus();
    expect(component.isOpen).toBeTrue();

    component.onDocumentClick({ target: document.body } as unknown as MouseEvent);

    expect(component.isOpen).toBeFalse();
  });
});
