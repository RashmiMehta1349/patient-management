import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../../../core/auth/auth.service';

function configureWithToken(token: string | null) {
  return TestBed.configureTestingModule({
    imports: [ResetPasswordComponent],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { queryParamMap: convertToParamMap(token ? { token } : {}) }
        }
      }
    ]
  }).compileComponents();
}

describe('ResetPasswordComponent', () => {
  let httpMock: HttpTestingController;

  afterEach(() => httpMock?.verify());

  it('shows the invalid-link message when no token is present in the URL', async () => {
    await configureWithToken(null);
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ResetPasswordComponent);
    expect(fixture.componentInstance.missingToken).toBeTrue();
  });

  it('flags mismatched passwords', async () => {
    await configureWithToken('raw-token');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ResetPasswordComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ newPassword: 'Password123!', confirmPassword: 'Different123!' });

    expect(component.form.errors?.['passwordsMismatch']).toBeTrue();
  });

  it('shows success and schedules redirect on valid reset', async () => {
    await configureWithToken('raw-token');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ResetPasswordComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'resetPassword').and.returnValue(of({ message: 'ok' }));

    component.form.setValue({ newPassword: 'Password123!', confirmPassword: 'Password123!' });
    component.submit();

    expect(component.success).toBeTrue();
  });

  it('shows the invalid/expired message on failure', async () => {
    await configureWithToken('raw-token');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ResetPasswordComponent);
    const component = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    spyOn(authService, 'resetPassword').and.returnValue(throwError(() => new Error('400')));

    component.form.setValue({ newPassword: 'Password123!', confirmPassword: 'Password123!' });
    component.submit();

    expect(component.errorMessage).toContain('invalid or has expired');
  });
});
