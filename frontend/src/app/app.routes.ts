import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './core/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      )
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      )
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'patients',
        loadComponent: () =>
          import('./features/patients/patient-search/patient-search.component').then(
            (m) => m.PatientSearchComponent
          )
      },
      {
        path: 'patients/new',
        loadComponent: () =>
          import('./features/patients/patient-add/patient-add.component').then(
            (m) => m.PatientAddComponent
          )
      },
      {
        path: 'patients/:id/edit',
        loadComponent: () =>
          import('./features/patients/patient-edit/patient-edit.component').then(
            (m) => m.PatientEditComponent
          )
      },
      {
        path: 'patients/:id',
        loadComponent: () =>
          import('./features/patients/patient-profile/patient-profile.component').then(
            (m) => m.PatientProfileComponent
          )
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
      // Later modules add their protected routes here as siblings of 'dashboard',
      // all riding on this same ShellComponent + authGuard.
    ]
  },
  { path: '**', redirectTo: 'login' }
];
