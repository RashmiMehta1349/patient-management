import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { RETURN_URL_STORAGE_KEY } from './return-url';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  function runGuard(url: string) {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url } as RouterStateSnapshot)
    );
  }

  it('allows navigation when authenticated', () => {
    authService.isAuthenticated.set(true);

    const result = runGuard('/dashboard');

    expect(result).toBeTrue();
  });

  it('redirects to /login, storing the return URL in sessionStorage rather than the query string', () => {
    authService.isAuthenticated.set(false);
    const createUrlTreeSpy = spyOn(router, 'createUrlTree').and.callThrough();

    const result = runGuard('/dashboard');

    expect(result instanceof UrlTree).toBeTrue();
    expect(createUrlTreeSpy).toHaveBeenCalledWith(['/login']);
    expect(sessionStorage.getItem(RETURN_URL_STORAGE_KEY)).toBe('/dashboard');
  });
});
