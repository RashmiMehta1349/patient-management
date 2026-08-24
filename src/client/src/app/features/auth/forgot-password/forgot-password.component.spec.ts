import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../../../core/auth/auth.service';

describe('ForgotPasswordComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows the generic confirmation message on success', () => {
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'forgotPassword').and.returnValue(of({ message: 'ok' }));

    component.form.setValue({ email: 'doc@example.com' });
    component.submit();

    expect(component.submitted).toBeTrue();
  });

  it('shows the same generic confirmation even if the request errors (no enumeration signal)', () => {
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'forgotPassword').and.returnValue(throwError(() => new Error('network')));

    component.form.setValue({ email: 'doc@example.com' });
    component.submit();

    expect(component.submitted).toBeTrue();
  });
});
