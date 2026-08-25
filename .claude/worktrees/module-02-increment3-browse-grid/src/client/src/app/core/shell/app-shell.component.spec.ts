import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AppShellComponent } from './app-shell.component';
import { AuthService } from '../auth/auth.service';

describe('AppShellComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
  }

  it('renders the Patients nav tab with a link to /patients when authenticated', () => {
    setup();
    const authService = TestBed.inject(AuthService);
    authService.isAuthenticated.set(true);

    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const navLink: HTMLAnchorElement = fixture.nativeElement.querySelector('.nav-tab');
    expect(navLink).toBeTruthy();
    expect(navLink.getAttribute('href')).toBe('/patients');
  });

  it('renders the logout button and calls AuthService.logout(true) on click', () => {
    setup();
    const authService = TestBed.inject(AuthService);
    authService.isAuthenticated.set(true);
    const logoutSpy = spyOn(authService, 'logout');

    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const logoutButton: HTMLButtonElement = fixture.nativeElement.querySelector('.logout-button');
    logoutButton.click();

    expect(logoutSpy).toHaveBeenCalledWith(true);
  });

  it('does not render when there is no authenticated session', () => {
    setup();
    const authService = TestBed.inject(AuthService);
    authService.isAuthenticated.set(false);

    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const header = fixture.nativeElement.querySelector('.app-header');
    expect(header).toBeFalsy();
  });
});
