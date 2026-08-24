import { PrismaClient, Session, User } from '@prisma/client';
import { env } from '../../config/env';
import { hashPassword, validatePasswordPolicy, verifyPassword } from './password.util';
import { generateRawToken, hashToken, signSessionToken, verifySessionToken } from './token.util';

export interface LoginResult {
  ok: boolean;
  token?: string;
  user?: Pick<User, 'id' | 'email' | 'lastLoginAt'>;
}

export interface SessionValidationResult {
  ok: boolean;
  reason?: 'invalid_token' | 'not_found' | 'revoked' | 'expired' | 'inactive';
  session?: Session;
  user?: User;
}

/**
 * Auth business logic, independent of HTTP (unit-testable per plan §4
 * "Validation placement"). Takes a PrismaClient (or compatible mock) so it
 * can be exercised in unit tests without a real database.
 */
export class AuthService {
  constructor(private readonly prisma: PrismaClient) {}

  private inactivityWindowMs(): number {
    return env.sessionInactivityTimeoutMinutes * 60 * 1000;
  }

  private hardTtlMs(): number {
    return env.sessionHardTtlHours * 60 * 60 * 1000;
  }

  /** 3.1 Login: verify credentials, create a session, issue a token. */
  async login(email: string, password: string): Promise<LoginResult> {
    const user = await this.prisma.user.findUnique({ where: { email } });

    // Generic failure for both "unknown user" and "wrong password" — no user enumeration.
    if (!user) {
      // Perform a dummy hash comparison so response timing doesn't reveal
      // whether the account exists.
      await verifyPassword(password, '$2b$12$invalidsaltinvalidsaltinvalidsaOx0Y1Q1Q1Q1Q1Q1Q1Q1Q1Q');
      return { ok: false };
    }

    const passwordMatches = await verifyPassword(password, user.passwordHash);
    if (!passwordMatches) {
      return { ok: false };
    }

    const now = new Date();
    const rawSecret = generateRawToken();
    const session = await this.prisma.session.create({
      data: {
        userId: user.id,
        tokenHash: hashToken(rawSecret),
        createdAt: now,
        lastActivityAt: now,
        expiresAt: new Date(now.getTime() + this.hardTtlMs())
      }
    });

    await this.prisma.user.update({
      where: { id: user.id },
      data: { lastLoginAt: now }
    });

    const token = signSessionToken(session.id, rawSecret);

    return {
      ok: true,
      token,
      user: { id: user.id, email: user.email, lastLoginAt: now }
    };
  }

  /**
   * 3.2 Session validation. Checks token signature, session existence,
   * revocation, hard expiry, and inactivity window. Updates
   * `last_activity_at` on success (rolling window).
   */
  async validateSession(rawToken: string): Promise<SessionValidationResult> {
    const payload = verifySessionToken(rawToken);
    if (!payload) {
      return { ok: false, reason: 'invalid_token' };
    }

    const session = await this.prisma.session.findUnique({ where: { id: payload.sid } });
    if (!session) {
      return { ok: false, reason: 'not_found' };
    }

    if (hashToken(payload.sec) !== session.tokenHash) {
      return { ok: false, reason: 'invalid_token' };
    }

    if (session.revokedAt) {
      return { ok: false, reason: 'revoked' };
    }

    const now = new Date();

    if (session.expiresAt.getTime() <= now.getTime()) {
      return { ok: false, reason: 'expired' };
    }

    if (now.getTime() - session.lastActivityAt.getTime() > this.inactivityWindowMs()) {
      return { ok: false, reason: 'inactive' };
    }

    const updatedSession = await this.prisma.session.update({
      where: { id: session.id },
      data: { lastActivityAt: now }
    });

    const user = await this.prisma.user.findUnique({ where: { id: session.userId } });
    if (!user) {
      return { ok: false, reason: 'not_found' };
    }

    return { ok: true, session: updatedSession, user };
  }

  /** 3.3 Logout: revoke the session referenced by the given token. */
  async logout(rawToken: string): Promise<void> {
    const payload = verifySessionToken(rawToken);
    if (!payload) return;

    const session = await this.prisma.session.findUnique({ where: { id: payload.sid } });
    if (!session || hashToken(payload.sec) !== session.tokenHash) return;

    await this.prisma.session.update({
      where: { id: session.id },
      data: { revokedAt: new Date() }
    });
  }

  /**
   * 3.4 Request a password reset. Always returns normally (uniform response
   * is enforced by the controller) — creates a token record only if the
   * email matches a user, to avoid user enumeration.
   */
  async requestPasswordReset(email: string): Promise<{ rawToken: string; userId: string } | null> {
    const user = await this.prisma.user.findUnique({ where: { email } });
    if (!user) return null;

    const rawToken = generateRawToken();
    const expiresAt = new Date(Date.now() + env.passwordResetTokenTtlMinutes * 60 * 1000);

    await this.prisma.passwordResetToken.create({
      data: {
        userId: user.id,
        tokenHash: hashToken(rawToken),
        expiresAt
      }
    });

    return { rawToken, userId: user.id };
  }

  /**
   * 3.5 Complete a password reset: validate token, enforce password policy,
   * update password_hash, mark token used, revoke ALL sessions for the user.
   */
  async confirmPasswordReset(
    rawToken: string,
    newPassword: string
  ): Promise<{ ok: true } | { ok: false; reason: 'invalid_token' | 'expired_or_used' | 'weak_password'; message?: string }> {
    const policy = validatePasswordPolicy(newPassword);
    if (!policy.valid) {
      return { ok: false, reason: 'weak_password', message: policy.message };
    }

    const tokenHash = hashToken(rawToken);
    const resetToken = await this.prisma.passwordResetToken.findFirst({
      where: { tokenHash }
    });

    if (!resetToken) {
      return { ok: false, reason: 'invalid_token' };
    }

    const now = new Date();
    if (resetToken.usedAt || resetToken.expiresAt.getTime() <= now.getTime()) {
      return { ok: false, reason: 'expired_or_used' };
    }

    const newHash = await hashPassword(newPassword);

    await this.prisma.$transaction([
      this.prisma.user.update({
        where: { id: resetToken.userId },
        data: { passwordHash: newHash }
      }),
      this.prisma.passwordResetToken.update({
        where: { id: resetToken.id },
        data: { usedAt: now }
      }),
      this.prisma.session.updateMany({
        where: { userId: resetToken.userId, revokedAt: null },
        data: { revokedAt: now }
      })
    ]);

    return { ok: true };
  }
}
