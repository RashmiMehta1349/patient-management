import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { RecentPatientsService } from '../patients/recent-patients.service';
import {
  CurrentUser,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  ResetPasswordRequest
} from './auth.models';

const TOKEN_STORAGE_KEY = 'pma_auth_token';

/**
 * Owns the client-side session: token storage (localStorage, per Assumption A1), login/logout
 * API calls, and a reactive `isAuthenticated` signal the rest of the app can read.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  /** Reactive flag other parts of the UI (e.g., nav bar) can read. */
  readonly isAuthenticated = signal<boolean>(this.hasToken());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly recentPatientsService: RecentPatientsService
  ) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiBaseUrl}/auth/login`, request).pipe(
      tap((response) => {
        this.setToken(response.token);
        this.isAuthenticated.set(true);
      })
    );
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBaseUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBaseUrl}/auth/reset-password`, request);
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.apiBaseUrl}/auth/me`);
  }

  logout(navigateToLogin = true, message?: string): void {
    // Best-effort server call; client-side token discard is what actually ends the session
    // (stateless JWT — no server-side session to revoke for a normal logout).
    if (this.hasToken()) {
      this.http.post(`${this.apiBaseUrl}/auth/logout`, {}).subscribe({
        error: () => undefined
      });
    }

    this.clearToken();
    this.isAuthenticated.set(false);
    // Recently-viewed data is unencrypted localStorage (Module 7 §12) — cleared on logout so it
    // does not persist across a session boundary on a shared/public machine.
    this.recentPatientsService.clear();

    if (navigateToLogin) {
      this.router.navigate(['/login'], message ? { queryParams: { message } } : undefined);
    }
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
  }

  private clearToken(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  }

  private hasToken(): boolean {
    return !!this.getToken();
  }
}
