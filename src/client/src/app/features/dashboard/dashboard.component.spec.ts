import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { AuthService } from '../../core/auth/auth.service';
import { RecentPatientsService } from '../../core/patients/recent-patients.service';

describe('DashboardComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'me').and.returnValue(
      of({ id: '1', email: 'doc@example.com', lastLoginAt: new Date().toISOString() })
    );
  }

  it('renders the empty state when there are zero recently viewed patients', () => {
    setup();
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No recently viewed patients yet.');
    expect(fixture.nativeElement.querySelector('.recent-patients-list')).toBeFalsy();
  });

  it('renders up to 5 recently viewed patients, most-recent-first, each linking to its profile', () => {
    setup();
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    spyOn(recentPatientsService, 'list').and.returnValue([
      { id: '2', fullName: 'Zack Adams', phoneNumber: '555-000-0002', viewedAt: new Date().toISOString() },
      { id: '1', fullName: 'Amy Baker', phoneNumber: '555-000-0001', viewedAt: new Date().toISOString() }
    ]);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();

    const links: HTMLAnchorElement[] = Array.from(fixture.nativeElement.querySelectorAll('.recent-patient-link'));
    expect(links.length).toBe(2);
    expect(links[0].getAttribute('href')).toBe('/patients/2');
    expect(links[0].textContent).toContain('Zack Adams');
    expect(links[1].getAttribute('href')).toBe('/patients/1');
  });
});
