import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return newPassword === confirmPassword ? null : { mismatch: true };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  token = this.route.snapshot.queryParamMap.get('token') ?? '';

  form = this.fb.nonNullable.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  submitting = false;
  errorMessage: string | null = null;
  linkInvalid = false;

  constructor() {
    if (!this.token) {
      this.linkInvalid = true;
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = null;
    const { newPassword, confirmPassword } = this.form.getRawValue();

    this.authService.confirmPasswordReset(this.token, newPassword, confirmPassword).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/login'], { queryParams: { reason: 'password-updated' } });
      },
      error: (err) => {
        this.submitting = false;
        if (err?.error?.error === 'INVALID_OR_EXPIRED_TOKEN') {
          this.linkInvalid = true;
        } else {
          this.errorMessage = err?.error?.message || 'Unable to reset password. Please try again.';
        }
      }
    });
  }
}
