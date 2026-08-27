import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { RETURN_URL_STORAGE_KEY } from '../../../core/auth/return-url';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submitting = false;
  errorMessage: string | null = null;
  sessionExpiredMessage: string | null = null;
  readonly passwordVisible = signal(false);

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  constructor() {
    const message = this.route.snapshot.queryParamMap.get('message');
    if (message === 'session-expired') {
      this.sessionExpiredMessage = 'Your session has expired. Please log in again.';
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting = false;
        const returnUrl = sessionStorage.getItem(RETURN_URL_STORAGE_KEY) ?? '/dashboard';
        sessionStorage.removeItem(RETURN_URL_STORAGE_KEY);
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.submitting = false;
        // Generic message regardless of failure reason — never reveal which field was wrong.
        this.errorMessage = 'Invalid email or password.';
      }
    });
  }
}
