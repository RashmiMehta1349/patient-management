import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { RecentPatientsService } from '../patients/recent-patients.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('login() stores the token and flips isAuthenticated on success', () => {
    expect(service.isAuthenticated()).toBeFalse();

    service.login({ email: 'doc@example.com', password: 'secret' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'abc123', expiresAtUtc: new Date().toISOString(), email: 'doc@example.com' });

    expect(service.getToken()).toBe('abc123');
    expect(service.isAuthenticated()).toBeTrue();
  });

  it('logout() clears the token, flips isAuthenticated, and navigates to /login', () => {
    localStorage.setItem('pma_auth_token', 'existing-token');
    const navigateSpy = spyOn(router, 'navigate');

    service.logout();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`);
    req.flush({});

    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    expect(navigateSpy).toHaveBeenCalledWith(['/login'], undefined);
  });

  it('logout() clears recently-viewed patients (Module 7 §12 mitigation)', () => {
    localStorage.setItem('pma_auth_token', 'existing-token');
    const recentPatientsService = TestBed.inject(RecentPatientsService);
    recentPatientsService.record({ id: '1', fullName: 'Amy Baker', phoneNumber: '555-000-0001' });
    expect(recentPatientsService.list().length).toBe(1);

    service.logout();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`).flush({});

    expect(recentPatientsService.list()).toEqual([]);
  });

  it('logout() with a message passes it as a query param', () => {
    localStorage.setItem('pma_auth_token', 'existing-token');
    const navigateSpy = spyOn(router, 'navigate');

    service.logout(true, 'session-expired');

    httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`).flush({});

    expect(navigateSpy).toHaveBeenCalledWith(['/login'], { queryParams: { message: 'session-expired' } });
  });
});
