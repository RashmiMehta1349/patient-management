import { Request, Response } from 'express';
import { z } from 'zod';
import { prisma } from '../../db/prisma';
import { AuthService } from './auth.service';
import { emailService } from './email.service';
import { AuthenticatedRequest } from './auth.middleware';

const authService = new AuthService(prisma);

const loginSchema = z.object({
  email: z.string().min(1, 'Email is required'),
  password: z.string().min(1, 'Password is required')
});

const GENERIC_LOGIN_ERROR = 'Invalid email or password';

function extractTokenFromRequest(req: Request): string | null {
  const header = req.headers.authorization;
  if (header && header.startsWith('Bearer ')) {
    return header.substring('Bearer '.length);
  }
  return (req as any).cookies?.session_token || null;
}

/** POST /api/auth/login */
export async function login(req: Request, res: Response): Promise<void> {
  const parsed = loginSchema.safeParse(req.body);
  if (!parsed.success) {
    // Field-shape errors are legitimate (missing fields) — not a credential
    // hint, so a distinct message here does not violate no-enumeration rules.
    res.status(400).json({ error: 'VALIDATION_ERROR', message: 'Email and password are required.' });
    return;
  }

  const result = await authService.login(parsed.data.email, parsed.data.password);
  if (!result.ok) {
    res.status(401).json({ error: 'INVALID_CREDENTIALS', message: GENERIC_LOGIN_ERROR });
    return;
  }

  res.status(200).json({
    token: result.token,
    user: result.user
  });
}

/** POST /api/auth/logout */
export async function logout(req: AuthenticatedRequest, res: Response): Promise<void> {
  const token = extractTokenFromRequest(req);
  if (token) {
    await authService.logout(token);
  }
  res.status(204).send();
}

/** GET /api/auth/me */
export async function me(req: AuthenticatedRequest, res: Response): Promise<void> {
  if (!req.user) {
    res.status(401).json({ error: 'UNAUTHENTICATED' });
    return;
  }
  res.status(200).json({
    id: req.user.id,
    email: req.user.email,
    lastLoginAt: req.user.lastLoginAt
  });
}

const requestResetSchema = z.object({
  email: z.string().min(1, 'Email is required')
});

const UNIFORM_RESET_REQUEST_MESSAGE = 'If that email is registered, a reset link has been sent.';

/** POST /api/auth/password-reset/request */
export async function requestPasswordReset(req: Request, res: Response): Promise<void> {
  const parsed = requestResetSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: 'VALIDATION_ERROR', message: 'Email is required.' });
    return;
  }

  const result = await authService.requestPasswordReset(parsed.data.email);
  if (result) {
    // Fire-and-forget; failures are logged, not surfaced to the client
    // (would otherwise leak whether the account exists).
    emailService.sendPasswordResetEmail(parsed.data.email, result.rawToken).catch((err) => {
      // eslint-disable-next-line no-console
      console.error('Failed to send password reset email:', err);
    });
  }

  // Uniform response regardless of match — no user enumeration (plan §3.4).
  res.status(200).json({ message: UNIFORM_RESET_REQUEST_MESSAGE });
}

const confirmResetSchema = z
  .object({
    token: z.string().min(1, 'Reset token is required'),
    newPassword: z.string().min(1, 'New password is required'),
    confirmPassword: z.string().min(1, 'Password confirmation is required')
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword']
  });

/** POST /api/auth/password-reset/confirm */
export async function confirmPasswordReset(req: Request, res: Response): Promise<void> {
  const parsed = confirmResetSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({
      error: 'VALIDATION_ERROR',
      message: parsed.error.issues[0]?.message || 'Invalid request.'
    });
    return;
  }

  const result = await authService.confirmPasswordReset(parsed.data.token, parsed.data.newPassword);
  if (!result.ok) {
    if (result.reason === 'weak_password') {
      res.status(400).json({ error: 'WEAK_PASSWORD', message: result.message });
      return;
    }
    res.status(400).json({
      error: 'INVALID_OR_EXPIRED_TOKEN',
      message: 'This link is no longer valid. Please request a new password reset.'
    });
    return;
  }

  res.status(200).json({ message: 'Password updated successfully. Please sign in with your new password.' });
}
