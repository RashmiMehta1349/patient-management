import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  ResetPasswordRequest,
  SessionCheckResponse
} from './auth.models';

/**
 * Owns the in-memory access token (never localStorage/sessionStorage, to reduce XSS
 * exfiltration risk per the plan's Client-Storage decision). A hard page refresh clears
 * this state by design; AuthGuard's bootstrap /session check re-establishes a fresh
 * server-validated state rather than trusting stale client state.
 *
 * This service, AuthGuard, and AuthInterceptor are the shared baseline every later
 * module's routes/API calls reuse unmodified.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;
  private accessToken: string | null = null;

  /** Reactive authentication state for guards/UI (e.g. shell logout control visibility). */
  readonly isAuthenticated = signal<boolean>(false);
  readonly currentUserEmail = signal<string | null>(null);

  constructor(private readonly http: HttpClient) {}

  getAccessToken(): string | null {
    return this.accessToken;
  }

  login(usernameOrEmail: string, password: string): Observable<LoginResponse> {
    const body: LoginRequest = { usernameOrEmail, password };
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, body).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
        this.isAuthenticated.set(true);
      })
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(
      tap(() => this.clearLocalState())
    );
  }

  /** Clears client-side auth state without an API call (e.g. after a 401 from the interceptor). */
  clearLocalState(): void {
    this.accessToken = null;
    this.isAuthenticated.set(false);
    this.currentUserEmail.set(null);
  }

  forgotPassword(email: string): Observable<MessageResponse> {
    const body: ForgotPasswordRequest = { email };
    return this.http.post<MessageResponse>(`${this.baseUrl}/forgot-password`, body);
  }

  resetPassword(token: string, newPassword: string): Observable<MessageResponse> {
    const body: ResetPasswordRequest = { token, newPassword };
    return this.http.post<MessageResponse>(`${this.baseUrl}/reset-password`, body);
  }

  /** Used by AuthGuard on app bootstrap / hard refresh to check "am I still logged in". */
  checkSession(): Observable<SessionCheckResponse> {
    return this.http.get<SessionCheckResponse>(`${this.baseUrl}/session`).pipe(
      tap((response) => {
        this.isAuthenticated.set(response.authenticated);
        this.currentUserEmail.set(response.email);
      })
    );
  }
}
