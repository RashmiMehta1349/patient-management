import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';

describe('LoginComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the component', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('marks the form invalid when fields are empty', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    expect(component.form.invalid).toBeTrue();
  });

  it('shows a generic error message on login failure', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'login').and.returnValue(throwError(() => new Error('401')));

    component.form.setValue({ email: 'doc@example.com', password: 'wrong' });
    component.submit();

    expect(component.errorMessage).toBe('Invalid email or password.');
    expect(component.submitting).toBeFalse();
  });

  it('navigates to the dashboard on successful login', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    spyOn(authService, 'login').and.returnValue(
      of({ token: 't', expiresAtUtc: new Date().toISOString(), email: 'doc@example.com' })
    );
    const navigateSpy = spyOn(router, 'navigateByUrl');

    component.form.setValue({ email: 'doc@example.com', password: 'correct' });
    component.submit();

    expect(navigateSpy).toHaveBeenCalledWith('/dashboard');
  });
});
