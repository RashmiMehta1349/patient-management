import { NextFunction, Request, Response } from 'express';
import { User } from '@prisma/client';
import { prisma } from '../../db/prisma';
import { AuthService } from './auth.service';

export interface AuthenticatedRequest extends Request {
  user?: User;
  sessionId?: string;
}

const authService = new AuthService(prisma);

function extractToken(req: Request): string | null {
  const header = req.headers.authorization;
  if (header && header.startsWith('Bearer ')) {
    return header.substring('Bearer '.length);
  }
  // Fallback: HttpOnly cookie named "session_token" (per plan §7 UI notes).
  const cookieToken = (req as any).cookies?.session_token;
  return cookieToken || null;
}

/**
 * Session-validation middleware (plan §4/§6). Protects any route it is
 * mounted on: rejects with 401 if the token is missing, invalid, revoked,
 * expired, or idle past the inactivity window; otherwise attaches the
 * authenticated user and updates last_activity_at.
 */
export async function requireSession(req: AuthenticatedRequest, res: Response, next: NextFunction): Promise<void> {
  const token = extractToken(req);
  if (!token) {
    res.status(401).json({ error: 'UNAUTHENTICATED', message: 'No session token provided.' });
    return;
  }

  const result = await authService.validateSession(token);
  if (!result.ok || !result.user || !result.session) {
    res.status(401).json({ error: 'SESSION_INVALID', message: 'Session is invalid or has expired.' });
    return;
  }

  req.user = result.user;
  req.sessionId = result.session.id;
  next();
}
