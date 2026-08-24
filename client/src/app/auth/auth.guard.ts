import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Route guard for protected routes: redirects unauthenticated users to
 * Login. Local presence of a token is used as a fast client-side check;
 * the server remains authoritative and any stale/expired/revoked token is
 * caught by the auth interceptor's global 401 handling on the first API call.
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getToken()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};
