import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthUser {
  id: string;
  email: string;
  lastLoginAt: string | null;
}

export interface LoginResponse {
  token: string;
  user: AuthUser;
}

const SESSION_TOKEN_KEY = 'pma_session_token';

/**
 * Frontend API client + session state for Module 1 (Authentication).
 *
 * The session token is stored client-side (localStorage) and attached to
 * every request by AuthInterceptor. The server remains the source of truth
 * for validity (expiry, inactivity, revocation) — this service never
 * decides a session is valid on its own, it only reacts to 401 responses.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  /** Reactive flag other parts of the app (e.g. the guard, app shell) can read. */
  readonly isAuthenticated = signal<boolean>(!!this.getToken());

  constructor(private readonly http: HttpClient) {}

  getToken(): string | null {
    return localStorage.getItem(SESSION_TOKEN_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(SESSION_TOKEN_KEY, token);
    this.isAuthenticated.set(true);
  }

  /** Clears local session state. Does not call the server. */
  clearLocalSession(): void {
    localStorage.removeItem(SESSION_TOKEN_KEY);
    this.isAuthenticated.set(false);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { email, password })
      .pipe(tap((res) => this.setToken(res.token)));
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(tap(() => this.clearLocalSession()));
  }

  me(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.baseUrl}/me`);
  }

  requestPasswordReset(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/password-reset/request`, { email });
  }

  confirmPasswordReset(
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/password-reset/confirm`, {
      token,
      newPassword,
      confirmPassword
    });
  }
}
