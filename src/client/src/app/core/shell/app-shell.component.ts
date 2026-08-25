import { Component, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { InactivityTimerService } from '../auth/inactivity-timer.service';
import { SearchWidgetComponent } from '../shared/search-widget/search-widget.component';

/**
 * Persistent header navigation shown on every authenticated screen (Increment 3 revision,
 * §9b.2). Renders nothing when there is no active session, so it never leaks the nav/logout
 * affordance onto login/forgot-password/reset-password. Owns the inactivity-timer start now
 * that it — not DashboardComponent — is the component present on every authenticated screen
 * (single-owner decision per §9b.2, to avoid a double timer). Starts the timer reactively off
 * the isAuthenticated signal (not just on init) since AppShellComponent is mounted once at app
 * bootstrap, before a not-yet-logged-in user's session exists.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, SearchWidgetComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly inactivityTimer = inject(InactivityTimerService);

  readonly isAuthenticated = this.authService.isAuthenticated;

  constructor() {
    effect(() => {
      if (this.isAuthenticated()) {
        this.inactivityTimer.start();
      }
    });
  }

  logout(): void {
    this.authService.logout(true);
  }
}
