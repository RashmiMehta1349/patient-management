import { NextFunction, Request, Response } from 'express';
import { env } from '../config/env';

/**
 * Application-layer HTTPS enforcement (safety net).
 *
 * Per Planning\01_Authentication_and_Authorization_Plan.md §0 / §11, HTTPS/TLS
 * termination is expected to happen at the reverse-proxy / hosting layer, but
 * this middleware provides a defense-in-depth safety net inside the app itself:
 * any request that arrives without TLS (as reported by `req.secure` or the
 * `x-forwarded-proto` header set by a TLS-terminating proxy) is rejected.
 *
 * Disabled only when FORCE_HTTPS=false, which should be used strictly for
 * local development without a TLS-terminating proxy in front of the app.
 */
export function httpsEnforce(req: Request, res: Response, next: NextFunction): void {
  if (!env.forceHttps) {
    next();
    return;
  }

  const forwardedProto = req.headers['x-forwarded-proto'];
  const isSecure = req.secure || forwardedProto === 'https';

  if (!isSecure) {
    res.status(403).json({
      error: 'HTTPS_REQUIRED',
      message: 'This API only accepts requests over HTTPS.'
    });
    return;
  }

  next();
}
