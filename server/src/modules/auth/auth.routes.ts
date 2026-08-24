import { Router } from 'express';
import rateLimit from 'express-rate-limit';
import * as authController from './auth.controller';
import { requireSession } from './auth.middleware';

const router = Router();

/**
 * Rate limiting / basic brute-force throttling (plan §11, Task 18).
 * Reasonable low-cost hardening; no account lockout / CAPTCHA / 2FA (out of scope).
 */
const loginLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 10,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'RATE_LIMITED', message: 'Too many login attempts. Please try again later.' }
});

const passwordResetLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 5,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'RATE_LIMITED', message: 'Too many password reset requests. Please try again later.' }
});

router.post('/login', loginLimiter, authController.login);
router.post('/logout', requireSession, authController.logout);
router.get('/me', requireSession, authController.me);
router.post('/password-reset/request', passwordResetLimiter, authController.requestPasswordReset);
router.post('/password-reset/confirm', passwordResetLimiter, authController.confirmPasswordReset);

export default router;
