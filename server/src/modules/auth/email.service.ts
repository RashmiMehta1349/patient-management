/**
 * Password-reset email delivery.
 *
 * DEV-ONLY STUB per confirmed project decision: real email provider
 * integration is deferred and out of scope for this build. Instead of
 * sending an email, the reset link (containing the raw, single-use token)
 * is logged so a developer/tester can retrieve it manually.
 *
 * The interface is intentionally small and swappable so a real transport
 * (SMTP / transactional email service) can be substituted later without
 * touching auth.service.ts or auth.controller.ts.
 */
export interface EmailService {
  sendPasswordResetEmail(toEmail: string, rawToken: string): Promise<void>;
}

export class DevStubEmailService implements EmailService {
  async sendPasswordResetEmail(toEmail: string, rawToken: string): Promise<void> {
    const resetLink = `${process.env.CLIENT_APP_URL || 'http://localhost:4200'}/reset-password?token=${rawToken}`;
    // eslint-disable-next-line no-console
    console.log(
      `[DEV EMAIL STUB] Password reset requested for ${toEmail}.\n` +
        `  Reset link (would be emailed in production): ${resetLink}\n` +
        `  Raw token: ${rawToken}`
    );
  }
}

export const emailService: EmailService = new DevStubEmailService();
