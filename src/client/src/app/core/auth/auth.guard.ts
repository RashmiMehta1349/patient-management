import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { RETURN_URL_STORAGE_KEY } from './return-url';

/**
 * Route guard applied to every non-auth route. Unauthenticated access attempts redirect to
 * /login; the originally requested URL is kept in sessionStorage (not the query string) for
 * post-login redirect, so the address bar just shows a plain /login.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  sessionStorage.setItem(RETURN_URL_STORAGE_KEY, state.url);
  return router.createUrlTree(['/login']);
};
