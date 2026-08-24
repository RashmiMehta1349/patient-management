import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/auth/auth.models';

/**
 * Minimal placeholder landing screen. Header nav/logout now live in AppShellComponent
 * (Increment 3 revision, §9b.2/§9b.4) — this component just shows a greeting and calls
 * GET /api/auth/me. Inactivity-timer start also moved to the shell (single-owner decision,
 * §9b.2), since the shell — not this dashboard — is now present on every authenticated screen.
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

  currentUser: CurrentUser | null = null;

  ngOnInit(): void {
    this.authService.me().subscribe({
      next: (user) => (this.currentUser = user)
    });
  }
}
