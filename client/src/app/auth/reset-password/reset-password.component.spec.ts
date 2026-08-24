import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ResetPasswordComponent } from './reset-password.component';

describe('ResetPasswordComponent', () => {
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let component: ResetPasswordComponent;
  let httpMock: HttpTestingController;
  let router: Router;

  function setup(queryParams: Record<string, string>) {
    return TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } }
        }
      ]
    }).compileComponents();
  }

  afterEach(() => httpMock?.verify());

  it('shows the "link no longer valid" state when no token is present in the URL', async () => {
    await setup({});
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.linkInvalid).toBe(true);
  });

  it('submits the new password with the token from the URL and redirects to login on success', async () => {
    await setup({ token: 'valid-token' });
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();

    component.form.setValue({ newPassword: 'NewPassword2', confirmPassword: 'NewPassword2' });
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/password-reset/confirm`);
    expect(req.request.body.token).toBe('valid-token');
    req.flush({ message: 'Password updated successfully.' });

    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { reason: 'password-updated' } });
  });

  it('flags mismatched password confirmation without calling the server', async () => {
    await setup({ token: 'valid-token' });
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    component.form.setValue({ newPassword: 'NewPassword2', confirmPassword: 'Different1' });
    expect(component.form.invalid).toBe(true);

    component.submit();
    httpMock.expectNone(`${environment.apiBaseUrl}/auth/password-reset/confirm`);
  });

  it('shows the "link no longer valid" state when the server rejects an expired/used token', async () => {
    await setup({ token: 'expired-token' });
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    component.form.setValue({ newPassword: 'NewPassword2', confirmPassword: 'NewPassword2' });
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/password-reset/confirm`);
    req.flush(
      { error: 'INVALID_OR_EXPIRED_TOKEN', message: 'This link is no longer valid.' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(component.linkInvalid).toBe(true);
  });
});
