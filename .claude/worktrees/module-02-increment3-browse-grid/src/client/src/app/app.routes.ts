import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
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
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  },
  {
    path: 'patients',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/list/patients-list.component').then((m) => m.PatientsListComponent)
  },
  {
    path: 'patients/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/form/patient-form.component').then((m) => m.PatientFormComponent)
  },
  // Ordering matters: 'patients/new' (above) must be declared before the parameterized
  // 'patients/:id' routes below, or Angular's route matcher would resolve /patients/new to the
  // detail component with id: 'new' — see Planning\02_Patient_Management_Plan.md §9a.8.
  {
    path: 'patients/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/form/patient-form.component').then((m) => m.PatientFormComponent)
  },
  {
    path: 'patients/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/patients/detail/patient-detail.component').then((m) => m.PatientDetailComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
