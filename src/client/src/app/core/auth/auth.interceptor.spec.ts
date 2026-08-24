import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('attaches the bearer token when present', () => {
    localStorage.setItem('pma_auth_token', 'my-token');

    http.get('/api/patients').subscribe();

    const req = httpMock.expectOne('/api/patients');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush({});
  });

  it('does not attach a header when no token is stored', () => {
    http.get('/api/patients').subscribe();

    const req = httpMock.expectOne('/api/patients');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('forces logout and redirect on a global 401', () => {
    localStorage.setItem('pma_auth_token', 'my-token');
    const logoutSpy = spyOn(authService, 'logout');

    http.get('/api/patients').subscribe({ error: () => undefined });

    const req = httpMock.expectOne('/api/patients');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).toHaveBeenCalledWith(true, 'session-expired');
  });

  it('does not trigger the global-401 handler for the login endpoint itself', () => {
    const logoutSpy = spyOn(authService, 'logout');

    http.post(`${environment.apiBaseUrl}/auth/login`, {}).subscribe({ error: () => undefined });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).not.toHaveBeenCalled();
  });
});
