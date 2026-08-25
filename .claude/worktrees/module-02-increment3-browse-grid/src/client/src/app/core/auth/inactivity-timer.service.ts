import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart'] as const;

/**
 * Client-side UX timer only — resets on user activity and proactively logs the doctor out
 * after the configured inactivity window (Assumption A2, default 20 min) so the app doesn't
 * wait for the next failed API call to notice the session has gone stale. The server-side JWT
 * expiry remains the authoritative enforcement point; this timer cannot be relied on alone
 * (a user could disable it via devtools) and exists purely to improve the experience.
 */
@Injectable({ providedIn: 'root' })
export class InactivityTimerService implements OnDestroy {
  private timeoutId: ReturnType<typeof setTimeout> | null = null;
  private started = false;
  private readonly boundReset = () => this.resetTimer();

  constructor(
    private readonly ngZone: NgZone,
    private readonly authService: AuthService
  ) {}

  start(): void {
    if (this.started) {
      return;
    }
    this.started = true;

    this.ngZone.runOutsideAngular(() => {
      ACTIVITY_EVENTS.forEach((eventName) =>
        window.addEventListener(eventName, this.boundReset, { passive: true })
      );
      this.resetTimer();
    });
  }

  stop(): void {
    this.started = false;
    ACTIVITY_EVENTS.forEach((eventName) => window.removeEventListener(eventName, this.boundReset));
    if (this.timeoutId !== null) {
      clearTimeout(this.timeoutId);
      this.timeoutId = null;
    }
  }

  ngOnDestroy(): void {
    this.stop();
  }

  private resetTimer(): void {
    if (this.timeoutId !== null) {
      clearTimeout(this.timeoutId);
    }

    this.timeoutId = setTimeout(
      () => {
        this.ngZone.run(() => {
          this.stop();
          this.authService.logout(true, 'session-expired');
        });
      },
      environment.inactivityTimeoutMinutes * 60 * 1000
    );
  }
}
