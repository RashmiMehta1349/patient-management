import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Attaches the bearer token to every outgoing request and handles global 401s by forcing
 * logout + redirect to /login with a session-expired message — the server-side JWT expiry
 * (SecurityStamp/exp check) is the authoritative enforcement point; this is the client-side
 * reaction to that enforcement.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  const authorizedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.endsWith('/auth/login')) {
        authService.logout(true, 'session-expired');
      }
      return throwError(() => error);
    })
  );
};
