import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('login stores the access token in memory and flips isAuthenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();

    service.login('doctor@example.com', 'Password1!').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ accessToken: 'fake-token', expiresAt: new Date().toISOString() });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.getAccessToken()).toBe('fake-token');
  });

  it('logout clears local state on success', () => {
    (service as unknown as { accessToken: string | null }).accessToken = 'fake-token';
    service.isAuthenticated.set(true);

    service.logout().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/logout`);
    req.flush(null);

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.getAccessToken()).toBeNull();
  });

  it('clearLocalState resets token and authenticated flag without an API call', () => {
    (service as unknown as { accessToken: string | null }).accessToken = 'fake-token';
    service.isAuthenticated.set(true);

    service.clearLocalState();

    expect(service.getAccessToken()).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('forgotPassword posts to the forgot-password endpoint', () => {
    service.forgotPassword('doctor@example.com').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/forgot-password`);
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'If that account exists, a reset link has been sent.' });
  });
});
