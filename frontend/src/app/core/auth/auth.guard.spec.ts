import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, UrlTree } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { SessionCheckResponse } from './auth.models';

class FakeAuthService {
  private authenticated = false;
  checkSessionResult$: Observable<SessionCheckResponse> = of({ authenticated: false, email: null });

  isAuthenticated = (): boolean => this.authenticated;
  setAuthenticated(value: boolean): void {
    this.authenticated = value;
  }
  checkSession(): Observable<SessionCheckResponse> {
    return this.checkSessionResult$;
  }
}

describe('authGuard', () => {
  let fakeAuth: FakeAuthService;
  let router: Router;

  function runGuard() {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  }

  beforeEach(() => {
    fakeAuth = new FakeAuthService();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: fakeAuth }]
    });
    router = TestBed.inject(Router);
  });

  it('allows navigation immediately if already authenticated in-memory', () => {
    fakeAuth.setAuthenticated(true);

    const result = runGuard();

    expect(result).toBeTrue();
  });

  it('falls back to a server session check and allows if authenticated', (done) => {
    fakeAuth.checkSessionResult$ = of({ authenticated: true, email: 'doctor@example.com' });

    const result$ = runGuard() as Observable<boolean | UrlTree>;
    result$.subscribe((value) => {
      expect(value).toBeTrue();
      done();
    });
  });

  it('redirects to /login when the server session check says unauthenticated', (done) => {
    fakeAuth.checkSessionResult$ = of({ authenticated: false, email: null });

    const result$ = runGuard() as Observable<boolean | UrlTree>;
    result$.subscribe((value) => {
      expect(router.serializeUrl(value as UrlTree)).toContain('/login');
      done();
    });
  });

  it('redirects to /login when the server session check errors', (done) => {
    fakeAuth.checkSessionResult$ = throwError(() => new Error('network error'));

    const result$ = runGuard() as Observable<boolean | UrlTree>;
    result$.subscribe((value) => {
      expect(router.serializeUrl(value as UrlTree)).toContain('/login');
      done();
    });
  });
});
