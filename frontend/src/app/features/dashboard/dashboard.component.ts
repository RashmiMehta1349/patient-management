import { Component } from '@angular/core';

/**
 * Placeholder landing screen behind AuthGuard. Real dashboard content is built by the
 * Patient Management module; this exists so Module 1's login → protected-route flow is
 * independently demonstrable/testable without depending on later modules.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: `<h1>Dashboard</h1>
    <p>You are signed in. Patient Management module screens render here.</p>`
})
export class DashboardComponent {}
