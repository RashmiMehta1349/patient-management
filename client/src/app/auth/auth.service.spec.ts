import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('stores the session token and flips isAuthenticated on successful login', () => {
    expect(service.isAuthenticated()).toBe(false);

    service.login('doctor@example.com', 'CorrectHorse1').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'fake-token', user: { id: '1', email: 'doctor@example.com', lastLoginAt: null } });

    expect(service.getToken()).toBe('fake-token');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('clears local session state on logout', () => {
    localStorage.setItem('pma_session_token', 'existing-token');
    service = TestBed.inject(AuthService);

    service.logout().subscribe();
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`);
    req.flush(null);

    expect(service.getToken()).toBeNull();
  });

  it('requests a password reset without requiring an existing session', () => {
    service.requestPasswordReset('doctor@example.com').subscribe((res) => {
      expect(res.message).toContain('registered');
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/password-reset/request`);
    req.flush({ message: 'If that email is registered, a reset link has been sent.' });
  });
});
