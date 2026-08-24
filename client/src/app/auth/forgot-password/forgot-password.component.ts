import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required]]
  });

  submitting = false;
  // Uniform confirmation message shown regardless of whether the email
  // matched an account — plan §3.4 / §7, no user enumeration.
  confirmationMessage: string | null = null;

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const { email } = this.form.getRawValue();

    this.authService.requestPasswordReset(email).subscribe({
      next: (res) => {
        this.submitting = false;
        this.confirmationMessage = res.message;
      },
      error: () => {
        this.submitting = false;
        // Even on an unexpected error, avoid leaking account existence;
        // show the same generic confirmation.
        this.confirmationMessage = 'If that email is registered, a reset link has been sent.';
      }
    });
  }
}
