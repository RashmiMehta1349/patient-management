import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { InactivityTimerService } from '../../core/auth/inactivity-timer.service';
import { CurrentUser } from '../../core/auth/auth.models';

/**
 * Minimal placeholder landing screen for Module 1 — subsequent modules (Patient Management
 * onward) replace/extend this. Its role here is to prove the auth gate works end-to-end:
 * a valid session reaches this screen, calls GET /api/auth/me, and exposes the persistent
 * logout control required by the plan's UI/Screens section.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly inactivityTimer = inject(InactivityTimerService);

  currentUser: CurrentUser | null = null;

  ngOnInit(): void {
    this.inactivityTimer.start();
    this.authService.me().subscribe({
      next: (user) => (this.currentUser = user)
    });
  }

  logout(): void {
    this.authService.logout(true);
  }
}
