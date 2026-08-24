import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, AuthUser } from '../auth/auth.service';

/**
 * Placeholder application dashboard/landing screen. Module 1 only needs to
 * expose the app shell's Logout control and prove a valid session reaches a
 * protected screen; the real dashboard is built by later modules.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  currentUser: AuthUser | null = null;

  ngOnInit(): void {
    this.authService.me().subscribe({
      next: (user) => (this.currentUser = user)
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      // Even if the server call fails, clear local state and navigate away.
      error: () => {
        this.authService.clearLocalSession();
        this.router.navigate(['/login']);
      }
    });
  }
}
