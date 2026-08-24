import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

const GENERIC_ERROR = 'Invalid email or password';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  submitting = false;
  errorMessage: string | null = null;
  sessionExpiredMessage: string | null = null;
  passwordUpdatedMessage: string | null = null;
  showPassword = false;

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('reason') === 'session-expired') {
      this.sessionExpiredMessage = 'Your session has expired. Please sign in again.';
    }
    if (params.get('reason') === 'password-updated') {
      this.passwordUpdatedMessage = 'Password updated. Please sign in with your new password.';
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    const { email, password } = this.form.getRawValue();

    this.authService.login(email, password).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.submitting = false;
        // Deliberately generic, regardless of which field was wrong (no
        // field-specific hints) — plan §3.1 / §13 acceptance criteria.
        this.errorMessage = GENERIC_ERROR;
      }
    });
  }
}
