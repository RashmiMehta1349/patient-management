import bcrypt from 'bcrypt';
import { env } from '../../config/env';

/**
 * Password hashing / verification helpers.
 * Uses bcrypt so that passwords are never stored or compared in plain text
 * (BRD Non-Functional Requirements → Security).
 */
export async function hashPassword(plainPassword: string): Promise<string> {
  return bcrypt.hash(plainPassword, env.bcryptCostFactor);
}

export async function verifyPassword(plainPassword: string, hash: string): Promise<boolean> {
  return bcrypt.compare(plainPassword, hash);
}

/**
 * Minimum password complexity policy (Module 1 doc / plan §3.5).
 * Kept intentionally simple: minimum length + at least one letter and one
 * number. Not specified precisely in the BRD; this is a reasonable, low-risk
 * default and easy to strengthen later without touching callers.
 */
export function validatePasswordPolicy(password: string): { valid: boolean; message?: string } {
  if (typeof password !== 'string' || password.length < 8) {
    return { valid: false, message: 'Password must be at least 8 characters long.' };
  }
  if (!/[A-Za-z]/.test(password) || !/[0-9]/.test(password)) {
    return { valid: false, message: 'Password must contain at least one letter and one number.' };
  }
  return { valid: true };
}
