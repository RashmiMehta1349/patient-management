import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { InactivityTimerService } from './inactivity-timer.service';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('InactivityTimerService', () => {
  let service: InactivityTimerService;
  let authService: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    service = TestBed.inject(InactivityTimerService);
    authService = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.stop();
    httpMock.verify();
  });

  it('logs out after the configured inactivity window with no activity', fakeAsync(() => {
    const logoutSpy = spyOn(authService, 'logout');

    service.start();
    tick(environment.inactivityTimeoutMinutes * 60 * 1000 + 1000);

    expect(logoutSpy).toHaveBeenCalledWith(true, 'session-expired');
  }));

  it('does not log out if activity resets the timer before expiry', fakeAsync(() => {
    const logoutSpy = spyOn(authService, 'logout');

    service.start();
    tick(environment.inactivityTimeoutMinutes * 60 * 1000 - 1000);
    window.dispatchEvent(new Event('mousemove'));
    tick(2000);

    expect(logoutSpy).not.toHaveBeenCalled();

    service.stop();
  }));
});
