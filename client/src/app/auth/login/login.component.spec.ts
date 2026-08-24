import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('navigates to /dashboard on successful login', () => {
    component.form.setValue({ email: 'doctor@example.com', password: 'CorrectHorse1' });
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    req.flush({ token: 'fake-token', user: { id: '1', email: 'doctor@example.com', lastLoginAt: null } });

    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('shows a single generic error message on failed login, regardless of cause', () => {
    component.form.setValue({ email: 'doctor@example.com', password: 'wrong' });
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    req.flush({ error: 'INVALID_CREDENTIALS', message: 'Invalid email or password' }, { status: 401, statusText: 'Unauthorized' });

    expect(component.errorMessage).toBe('Invalid email or password');
  });

  it('does not submit when the form is invalid', () => {
    component.form.setValue({ email: '', password: '' });
    component.submit();
    httpMock.expectNone(`${environment.apiBaseUrl}/auth/login`);
  });
});
