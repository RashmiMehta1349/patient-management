import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Route guard reused unmodified by every later module's protected routes. If the
 * in-memory token already indicates an authenticated state, allow immediately; otherwise
 * (e.g. on a hard refresh) fall back to a server-side session check before deciding.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return auth.checkSession().pipe(
    map((result) => {
      if (result.authenticated) {
        return true;
      }
      return router.createUrlTree(['/login']);
    }),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
