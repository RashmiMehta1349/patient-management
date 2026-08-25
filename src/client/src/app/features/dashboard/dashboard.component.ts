import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/auth/auth.models';
import { RecentPatientsService } from '../../core/patients/recent-patients.service';
import { RecentPatient } from '../../core/patients/recent-patients.models';

/**
 * Minimal placeholder landing screen. Header nav/logout now live in AppShellComponent
 * (Increment 3 revision, §9b.2/§9b.4) — this component just shows a greeting and calls
 * GET /api/auth/me. Inactivity-timer start also moved to the shell (single-owner decision,
 * §9b.2), since the shell — not this dashboard — is now present on every authenticated screen.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly recentPatientsService = inject(RecentPatientsService);

  currentUser: CurrentUser | null = null;
  recentPatients: RecentPatient[] = [];

  ngOnInit(): void {
    this.authService.me().subscribe({
      next: (user) => (this.currentUser = user)
    });

    this.recentPatients = this.recentPatientsService.list();
  }
}
