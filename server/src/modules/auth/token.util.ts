import crypto from 'crypto';
import jwt from 'jsonwebtoken';
import { env } from '../../config/env';

/**
 * Session tokens are signed JWTs whose payload embeds the session ID.
 * The signature proves the token wasn't tampered with; the session ID is
 * then looked up in the `sessions` table on every request so that sessions
 * remain server-revocable (logout, password reset) — see plan §4/§11.
 */
export interface SessionTokenPayload {
  sid: string; // session id
  sec: string; // raw session secret component (hashed copy stored server-side as sessions.token_hash)
}

/**
 * Signs a session token embedding the session id and a random secret
 * component. Only the hash of the secret is stored server-side (defense in
 * depth: a leaked `sessions` table row alone does not yield a usable token,
 * mirroring password hashing discipline per plan §5/§11).
 */
export function signSessionToken(sessionId: string, rawSecret: string): string {
  const payload: SessionTokenPayload = { sid: sessionId, sec: rawSecret };
  return jwt.sign(payload, env.sessionJwtSecret, { algorithm: 'HS256' });
}

export function verifySessionToken(token: string): SessionTokenPayload | null {
  try {
    const decoded = jwt.verify(token, env.sessionJwtSecret, { algorithms: ['HS256'] });
    if (typeof decoded === 'object' && decoded !== null && 'sid' in decoded) {
      return decoded as SessionTokenPayload;
    }
    return null;
  } catch {
    return null;
  }
}

/** Hash a raw single-use secret (session token or reset token) for storage. */
export function hashToken(rawToken: string): string {
  return crypto.createHash('sha256').update(rawToken).digest('hex');
}

/** Generate a cryptographically random raw token (for password reset links). */
export function generateRawToken(): string {
  return crypto.randomBytes(32).toString('hex');
}
