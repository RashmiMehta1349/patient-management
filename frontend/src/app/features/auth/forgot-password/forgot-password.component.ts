import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submitting = false;
  // Always shown on success, regardless of whether the email matched an account (no enumeration).
  confirmationMessage: string | null = null;

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const { email } = this.form.getRawValue();

    this.auth.forgotPassword(email).subscribe({
      next: (response) => {
        this.submitting = false;
        this.confirmationMessage = response.message;
      },
      error: () => {
        this.submitting = false;
        // Even on an unexpected error, avoid leaking anything account-specific.
        this.confirmationMessage = 'If that account exists, a reset link has been sent.';
      }
    });
  }
}
