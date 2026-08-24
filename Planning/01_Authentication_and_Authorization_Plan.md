# Module 1: Authentication & Authorization — Technical Planning Document

> Source module spec: `Modules\01_Authentication_and_Authorization.md`
> Source BRD: `BRD\Doc_BRD_Final.md`
> This module has no upstream dependencies and is built first. Several decisions here (stack choice, layering pattern, API conventions, auth guard pattern) are called out at the end as the **architectural baseline** the remaining nine modules should reuse rather than re-decide.

---

## 0. Assumptions Log (flagged per agent instructions)

The BRD and module spec deliberately leave some implementation details open. Rather than blocking on every one, reasonable, low-risk defaults appropriate for a single-user, single-clinic app are selected below and explicitly labeled as **assumptions for Product Owner sign-off**, not silent decisions:

| # | Open item | BRD/Module says | Assumption made | Rationale |
|---|---|---|---|---|
| A1 | Backend stack | Not specified anywhere in BRD/Modules | **ASP.NET Core Web API (C#, .NET 8 LTS) + Entity Framework Core + SQL Server** | Mainstream, strongly-typed, first-class layered-architecture support (controllers/services/repositories), mature auth libraries (ASP.NET Core Identity primitives, JWT), good fit for a small single-clinic deployment, and aligns with the Windows-based dev/deploy environment already in use for this project. Any equivalent mainstream stack (Node/NestJS, Spring Boot) would also satisfy the constraint — this is the one selected and used consistently across all module plans. |
| A2 | Password hashing algorithm | "Passwords must never be stored... in plain text (hashed at rest)" — algorithm unspecified | **ASP.NET Core Identity's PBKDF2-HMAC-SHA256 hasher** (or bcrypt as a documented alternative) | Industry-standard, salted, adaptive work factor, no external dependency; PBKDF2 ships natively with the chosen stack. |
| A3 | Session/token mechanism | Spec says "issues a session/token" without naming a scheme | **JWT access token (short-lived) + server-tracked refresh/session record used to enforce idle timeout** | JWTs are stateless and pair naturally with an Angular SPA + API tier; a lightweight server-side "active session" row is still needed because JWTs alone can't be revoked on password reset (Business Rule: reset must invalidate prior sessions) or track last-activity time for idle timeout. |
| A4 | Inactivity timeout duration | "Simple inactivity-based session timeout only" — no number given | **15 minutes of inactivity** (configurable via app setting) | Common clinical-software default balancing security of PHI against a solo physician's workflow interruptions; must be confirmed with Product Owner. |
| A5 | Password reset delivery mechanism | "e.g., email-based reset link or admin-issued reset" — BRD leaves it as an example, not a decision | **Email-based reset link** via a transactional email provider (e.g., SMTP relay/SendGrid), time-limited single-use token | Matches the BRD's own example and requires no manual developer/admin intervention per Acceptance Criteria ("without developer intervention"). Requires Product Owner to supply/approve an email sending mechanism for the deployment environment. |
| A6 | Reset token lifetime | Not specified | **30 minutes** | Standard reset-link window; balances usability against exposure risk. |
| A7 | Failed-login handling (lockout/throttling) | Not mentioned in BRD (out of scope: no brute-force/lockout policy stated) | **Basic IP/account-level rate limiting on the login and reset endpoints** (e.g., 5 attempts / 15 min) as a defensive-coding minimum, without building a lockout UI/policy | BRD doesn't ask for account lockout, but omitting all throttling on an internet-facing login endpoint would conflict with the general "encryption/security best practice" NFR. Flagged as a lightweight addition, not a new feature area — should be confirmed as acceptable rather than assumed silently.

These seven items should be confirmed with the Product Owner before or during Sprint 1; none of them change scope, they only fill in mechanics the BRD left as examples or silent.

---

## 1. Module Overview

The Authentication & Authorization module is the single gate through which the solo physician user accesses the Patient Management Application. Because the BRD defines exactly one pre-provisioned user with no roles, no self-registration, and no multi-user concerns, this module is deliberately minimal: it verifies identity at login, protects an idle session with a timeout, allows self-service password recovery without developer involvement, and ensures all credential and patient-data traffic is encrypted in transit. Despite its simplicity, it is architecturally foundational — no other module's screens or APIs should be reachable without a valid, active session, and this module establishes the multi-tier project shape (Angular presentation tier, API tier, business-logic tier, data-access tier, database tier) that every subsequent module will build inside.

---

## 2. Business Requirements

Traced to `Modules\01_Authentication_and_Authorization.md` and `BRD\Doc_BRD_Final.md`:

- Login screen with username/email + password (Module spec §4.1; BRD NFR-Security "Secure login (single user authentication)").
- Password recovery/reset flow, self-service, no developer intervention required (Module spec §4.2–4.3; BRD NFR-Security "Account is pre-provisioned... password recovery flow is included in Phase 1").
- Inactivity-based session timeout only, no concurrent-session handling (Module spec §4.4; BRD NFR-Security "Simple inactivity-based session timeout only; no concurrent-session handling needed").
- Manual logout (Module spec §4.5).
- All auth and subsequent traffic over HTTPS/TLS (Module spec §4.6; BRD NFR-Security "Data encryption (at rest and in transit)").
- Exactly one user account, pre-provisioned by the dev/deploy team, no in-app account creation UI (Module spec §5, Business Rules).
- Passwords never stored/transmitted in plain text (Module spec §5).
- A password reset invalidates any previously issued session tokens (Module spec §5).
- Generic error message on invalid login, no field-level hinting (Module spec §10, Acceptance Criteria).
- Login/session checks must not noticeably affect the < 2s page-load target (Module spec §11; BRD NFR-Performance).

**Explicitly out of scope** (must not be planned in): user registration/self-sign-up, multiple accounts/roles, concurrent-session/device management, 2FA, OAuth/SSO (Module spec §3, "Out of Scope"; BRD "Out of Scope" — no receptionist/multi-user access). If any future request asks for these, it is a scope change and should be redirected to the brainstorming-agent, not planned here.

---

## 3. Workflows

### 3.1 Login
1. User navigates to the app; Angular route guard detects no valid session and redirects to `/login`.
2. User submits username/email + password via the login form.
3. API tier validates input shape (non-empty, basic format), forwards to the business-logic (auth service) tier.
4. Auth service loads the single `User` record by username/email, verifies the password hash.
5. On success: auth service creates a session record (last-activity timestamp = now), issues a short-lived JWT access token bound to that session ID, and returns it to the client. Client stores the token in memory (not localStorage, to reduce XSS exfiltration risk) and navigates to the dashboard.
6. On failure (unknown user or bad password): return one generic error ("Invalid username or password") regardless of which check failed; log the attempt server-side for rate-limiting purposes only (no audit log per BRD scope).

### 3.2 Session Validation on Each Request
1. Angular HTTP interceptor attaches the bearer token to every outgoing API call.
2. API tier's auth middleware validates JWT signature/expiry, then checks the associated session record's `last_activity_at` against the configured idle timeout (A4: 15 min).
3. If valid and within timeout: request proceeds; `last_activity_at` is refreshed.
4. If expired (idle timeout exceeded or token expired) or session invalidated (e.g., post password-reset): return 401; Angular interceptor catches this, clears local auth state, and redirects to `/login` with a "session expired" message.

### 3.3 Password Recovery (Request)
1. User selects "Forgot password" on the login screen, enters their registered email.
2. API accepts the request and always returns a generic success message ("If that account exists, a reset link has been sent") regardless of whether the email matches — avoids user enumeration.
3. If it matches the single provisioned account, business-logic tier generates a single-use, cryptographically random reset token, hashes it before storage, sets expiry (A6: 30 min), and triggers an email send containing a reset link with the raw token.

### 3.4 Password Reset (Completion)
1. User clicks the emailed link, lands on `/reset-password?token=...`.
2. User submits a new password (client-side + server-side complexity validation).
3. API validates the token (hash match, not expired, not already used), and business-logic tier updates the stored password hash, marks the token used, and invalidates all existing session records for the user (Business Rule: reset invalidates prior sessions).
4. User is redirected to login with a confirmation message.

### 3.5 Logout
1. User selects "Logout."
2. Client calls a logout endpoint; API invalidates the current session record server-side and the client discards the in-memory token, then redirects to `/login`.

---

## 4. Architecture Approach

The application uses a **multi-tier architecture** with clear separation of concerns, established here and reused by every subsequent module:

- **Presentation tier** — Angular SPA. Owns UI state, routing, and an `AuthGuard` + `HTTP interceptor` pair that every future module's routes/services will reuse unmodified.
- **API tier** — ASP.NET Core Web API controllers. Thin: model binding, input-shape validation, HTTP status mapping, and calling into the business-logic tier. No business rules live here.
- **Business-logic tier** — plain C# service classes (e.g., `AuthService`, `PasswordResetService`) containing all rules from §5 of the module spec (hashing, token generation, timeout logic, invalidation on reset). Framework-agnostic where practical, to keep it testable in isolation.
- **Data-access tier** — repository classes over Entity Framework Core (`IUserRepository`, `ISessionRepository`), isolating the business-logic tier from persistence/query details. Later modules add their own repositories following the same interface + implementation pattern.
- **Database tier** — SQL Server (or equivalent RDBMS), with encryption at rest via Transparent Data Encryption (TDE) or disk-level encryption, satisfying the BRD's "encryption at rest" NFR.

**Key decisions and rationale:**
- **JWT + server-tracked session row (A3):** pure stateless JWT cannot be revoked before natural expiry, which conflicts with the Business Rule that a password reset must invalidate prior sessions, and with enforcing idle-timeout (JWT expiry alone is a hard cap, not an idle timer). A minimal `Sessions` table bridges this: the JWT carries a session ID; the API checks that session's `last_activity_at` and `is_valid` flag on every request. This keeps the token itself small/stateless while preserving revocability.
- **Validation placement:** input shape/format validation happens at the API tier (fail fast, cheap); business rule validation (credential correctness, token expiry, password reuse policy if any) happens in the business-logic tier so it's unit-testable without spinning up HTTP.
- **Synchronous flow throughout:** login, reset, and session checks are all short, low-volume, single-user operations — no async/queue-based processing is warranted at this app's scale (BRD Scalability NFR: "single clinic with moderate patient volume").
- **Generic error responses:** both invalid-login and forgot-password-with-unknown-email paths return identical, non-specific messages, per the Acceptance Criteria and standard practice to prevent account/email enumeration.
- **Token storage on client:** in-memory (Angular service state), not `localStorage`/`sessionStorage`, to reduce exposure to XSS-based token theft; token is re-acquired via login rather than persisted across a hard page refresh, or optionally paired with an httpOnly refresh cookie if the Product Owner wants "stay logged in across refresh" (flagged as an open UX question, not assumed).

---

## 5. Database Entities

| Table | Field | Type | Notes |
|---|---|---|---|
| **Users** | `Id` | UNIQUEIDENTIFIER / int, PK | Exactly one row per Business Rule; no admin UI to add more in Phase 1. |
| | `Email` | nvarchar(256), unique, not null | Used as login identifier and reset-link destination. |
| | `Username` | nvarchar(100), unique, nullable | Optional alternate login identifier if login screen supports "username or email." |
| | `PasswordHash` | nvarchar(512), not null | PBKDF2/bcrypt hash (A2); never plaintext. |
| | `LastLoginAt` | datetime2, nullable | Set on successful login. |
| | `CreatedAt` | datetime2, not null | Set at provisioning time (seed/migration), not via UI. |
| **PasswordResetTokens** | `Id` | UNIQUEIDENTIFIER, PK | |
| | `UserId` | FK → Users.Id, not null | Indexed. |
| | `TokenHash` | nvarchar(512), not null | Store hash of token, not raw value, so a DB leak doesn't expose usable tokens. |
| | `ExpiresAt` | datetime2, not null | Now + 30 min (A6). |
| | `UsedAt` | datetime2, nullable | Set once consumed; null token is unusable if already used. |
| | `CreatedAt` | datetime2, not null | |
| **Sessions** | `Id` | UNIQUEIDENTIFIER, PK | Referenced by the JWT's `sid` claim. |
| | `UserId` | FK → Users.Id, not null | Indexed. |
| | `IssuedAt` | datetime2, not null | |
| | `LastActivityAt` | datetime2, not null | Updated on each authenticated request; drives idle timeout. |
| | `IsValid` | bit, not null, default 1 | Flipped to 0 on logout or password reset (invalidation). |
| | `ExpiresAt` | datetime2, not null | Hard cap (e.g., JWT lifetime), independent of idle timeout. |

**Indexes:** `Users.Email` (unique), `Users.Username` (unique, filtered where not null), `PasswordResetTokens.UserId`, `PasswordResetTokens.TokenHash` (unique), `Sessions.UserId`, `Sessions.Id` (PK, used as lookup key on every request — keep hot).

**FK relationships:** `PasswordResetTokens.UserId` → `Users.Id` (cascade delete not needed given single-user, but restrict is safer); `Sessions.UserId` → `Users.Id`.

No audit/log table — explicitly out of scope per Module spec §7.

---

## 6. APIs

All endpoints under `/api/auth`, versioned base path convention (`/api/v1/...`) established here for reuse by later modules.

| Method | Path | Purpose | Auth required |
|---|---|---|---|
| POST | `/api/v1/auth/login` | Validate credentials, issue JWT + create session record | No (public, rate-limited) |
| POST | `/api/v1/auth/logout` | Invalidate current session | Yes (bearer token) |
| POST | `/api/v1/auth/forgot-password` | Accept email, trigger reset email if account matches | No (public, rate-limited) |
| POST | `/api/v1/auth/reset-password` | Accept token + new password, update hash, invalidate sessions | No (token itself is the credential; public endpoint, rate-limited) |
| GET | `/api/v1/auth/session` | Lightweight "am I still logged in" check used by the Angular guard on app bootstrap | Yes (bearer token) |

All endpoints require HTTPS/TLS (enforced at the hosting/reverse-proxy level, redirect HTTP→HTTPS) per Business Rule and BRD Security NFR. No request/response schemas are specified here in code form per the planning-agent's constraints — each payload is described in prose in the workflows above (§3).

---

## 7. UI / Screens

- **Login screen** (`/login`) — username/email field, password field (masked, show/hide toggle), submit button, generic inline error area, "Forgot password?" link. No "Sign up" link (out of scope).
- **Forgot Password screen** (`/forgot-password`) — single email field, submit button, generic confirmation message shown regardless of match outcome.
- **Reset Password screen** (`/reset-password?token=...`) — new password + confirm-password fields, client-side strength/match validation, submit button, success/failure state (expired/used token shows a clear "request a new link" prompt).
- **Session-expired interstitial** — not a distinct route, but a shared toast/banner triggered by the HTTP interceptor on any 401, before redirecting to `/login`.
- **Logout control** — a persistent header/nav element (shared shell component) available on every authenticated screen; this shell is the first piece of shared chrome later modules' screens will render inside.

---

## 8. Dependencies

- **Upstream:** none — this is the foundational module (per Module spec §8 and the dependency graph).
- **Downstream:** every other module (Patient Management, Appointment Management, Consultation & Clinical Records, Prescription & Medication Management, Patient History, Search & Navigation, Data Export, Administration) sits behind this module's `AuthGuard` and bearer-token convention; none of their screens/APIs should be reachable without a valid session issued here.

---

## 9. Implementation Tasks

1. Scaffold the multi-tier solution: Angular workspace (presentation tier) + ASP.NET Core Web API solution with `Api`, `BusinessLogic`, `DataAccess`, and `Domain`/`Contracts` projects (API tier / business-logic tier / data-access tier separation).
2. Configure HTTPS enforcement (redirect + HSTS) at the API host and confirm the dev/deploy reverse proxy also terminates TLS correctly.
3. Create `Users`, `PasswordResetTokens`, `Sessions` tables via EF Core migrations; write a seed/provisioning script (not a UI) to create the single doctor account with a hashed initial password.
4. Implement `IUserRepository`, `ISessionRepository`, `IPasswordResetTokenRepository` in the data-access tier.
5. Implement `AuthService` (login, logout, session validation, idle-timeout check) in the business-logic tier; unit test in isolation from EF Core using an in-memory/fake repository.
6. Implement `PasswordResetService` (request + complete flows, token generation/hashing/expiry) in the business-logic tier.
7. Implement `AuthController` exposing the five endpoints in §6; keep controllers thin (delegate to services).
8. Implement JWT issuance/validation middleware, wiring session-ID claim lookup against the `Sessions` table on every request; implement idle-timeout enforcement in this middleware.
9. Implement basic rate limiting on `login`, `forgot-password`, `reset-password` endpoints (A7).
10. Wire up email delivery for the reset link (SMTP/provider integration point — configuration-driven, pluggable) (A5).
11. Build Angular `AuthService` (token storage in memory, login/logout/forgot/reset API calls), `AuthGuard` (route protection), and `AuthInterceptor` (attach bearer token, catch 401 → redirect).
12. Build the four screens/components in §7, including client-side validation and generic-error display.
13. Add integration tests covering the full login → protected-call → idle-timeout → 401 → redirect cycle, and the full forgot → email(mock) → reset → old-session-invalidated cycle.
14. Confirm the < 2s page-load / auth-check performance target with a basic load check on the login and session-check endpoints.
15. Document the shared `AuthGuard`/`AuthInterceptor`/multi-tier folder pattern (see §10) so later module plans can reference it instead of re-deriving it.

---

## 10. File Structure

Indicative, framework-agnostic-in-spirit but reflecting the chosen Angular + ASP.NET Core stack. This layout is the **baseline** later modules should extend (adding their own controller/service/repository/feature folders alongside these).

```
/patient-management-app
│
├── /frontend                          (Presentation tier — Angular)
│   ├── /src
│   │   ├── /app
│   │   │   ├── /core
│   │   │   │   ├── /auth
│   │   │   │   │   ├── auth.service.ts
│   │   │   │   │   ├── auth.guard.ts        <- reused unmodified by all later modules' routes
│   │   │   │   │   ├── auth.interceptor.ts  <- reused unmodified for every module's API calls
│   │   │   │   │   └── auth.models.ts
│   │   │   │   └── /shell                  (shared authenticated layout: header, logout, nav)
│   │   │   ├── /features
│   │   │   │   └── /auth
│   │   │   │       ├── login/
│   │   │   │       ├── forgot-password/
│   │   │   │       └── reset-password/
│   │   │   └── app-routing.module.ts
│   │   └── environments/               (API base URL config per environment)
│   └── angular.json
│
├── /backend                            (API / Business-Logic / Data-Access / Domain tiers)
│   ├── /PatientMgmt.Api                (API tier)
│   │   ├── /Controllers
│   │   │   └── AuthController.cs
│   │   ├── /Middleware
│   │   │   └── JwtSessionMiddleware.cs
│   │   ├── Program.cs / Startup.cs     (HTTPS/HSTS, DI wiring, rate limiting config)
│   │   └── appsettings.json            (token lifetime, idle timeout, email provider config)
│   │
│   ├── /PatientMgmt.BusinessLogic      (Business-logic tier)
│   │   ├── /Auth
│   │   │   ├── AuthService.cs
│   │   │   ├── PasswordResetService.cs
│   │   │   └── PasswordHasher.cs
│   │   └── /Interfaces
│   │       └── IAuthService.cs
│   │
│   ├── /PatientMgmt.DataAccess         (Data-access tier)
│   │   ├── /Repositories
│   │   │   ├── UserRepository.cs
│   │   │   ├── SessionRepository.cs
│   │   │   └── PasswordResetTokenRepository.cs
│   │   ├── /Migrations
│   │   └── AppDbContext.cs
│   │
│   └── /PatientMgmt.Domain             (shared entities/contracts, referenced by all tiers/modules)
│       ├── /Entities
│       │   ├── User.cs
│       │   ├── Session.cs
│       │   └── PasswordResetToken.cs
│       └── /Contracts                  (DTOs for API requests/responses)
│
└── /database
    └── seed-provisioned-user.sql       (or EF migration seed) — creates the single doctor account
```

---

## 11. Security Considerations

- Passwords hashed with a salted, adaptive algorithm (A2); never logged, never returned in any API response.
- Reset tokens stored hashed, single-use, time-limited (A6); raw token only ever exists in the emailed link and transiently in the request body.
- Generic error messages on both login failure and forgot-password (no enumeration signal), per Acceptance Criteria.
- All traffic over HTTPS/TLS end-to-end (BRD Security NFR: encryption in transit); HSTS enabled to prevent downgrade.
- JWTs short-lived and scoped to a revocable session record so password reset can invalidate prior access immediately (Business Rule).
- Idle-timeout enforced server-side (not just client-side timer) so a stolen/replayed token can't outlive inactivity policy.
- Basic rate limiting on public auth endpoints (A7) as defensive coding against credential-stuffing/brute force, without introducing an out-of-scope lockout feature.
- Encryption at rest for the database (TDE or disk-level) covering `PasswordHash`, `TokenHash`, and session data, per BRD Security NFR.
- No sensitive data (password, token) ever appears in application logs.

---

## 12. Test Strategy

**Unit tests (business-logic tier, isolated from EF Core/HTTP):**
- `AuthService`: correct credentials → session created; incorrect password → generic failure; unknown user → same generic failure; idle-timeout boundary (just under/over threshold).
- `PasswordResetService`: token generation is unique/random; expired token rejected; already-used token rejected; valid token updates hash and invalidates all sessions for that user.
- Password hasher: same password produces different hashes (salting); verify() round-trips correctly.

**Integration tests (API tier + real test database):**
- POST `/login` with valid/invalid credentials → correct status codes and body shape.
- Authenticated GET to a protected sample endpoint with an expired-by-idle session → 401.
- POST `/forgot-password` for existing vs. non-existing email → identical response shape/timing (no enumeration leak).
- POST `/reset-password` with valid token → subsequent use of a pre-reset JWT is rejected (session invalidation confirmed).
- Rate-limit threshold on `/login` triggers a 429 after configured attempt count.

**End-to-end (UI-driven):**
- Full login → dashboard redirect happy path.
- Invalid login shows single generic error, no field-specific hints.
- Idle for configured timeout → next click redirects to login with session-expired messaging.
- Forgot password → (mocked email capture) → reset link → set new password → login with new password succeeds, old password fails.
- Logout clears session and blocks back-navigation to protected screens.

**Performance:**
- Login and session-check endpoints respond well within the <2s page-load budget under expected single-user load (BRD Performance NFR) — measured via a basic load/latency check, not full load testing given single-user scale.

---

## 13. Acceptance Criteria

(Restated from Module spec §10, each independently testable)

- Given valid credentials, the doctor can log in and reach the application dashboard.
- Given invalid credentials, login is rejected with a single generic error message that does not indicate which field was wrong.
- After the configured inactivity period (A4: 15 minutes, pending Product Owner confirmation), the session expires and the next user action redirects to the login screen.
- The doctor can request a password reset and successfully set a new password end-to-end without any developer/admin intervention.
- All requests to the application — login, session checks, and every subsequent API call — occur over HTTPS; no credentials or patient data are ever observed traveling unencrypted.
- Following a password reset, any session token issued before the reset is rejected on its next use.
- Login/session-check latency does not visibly regress the application's <2 second page-load target.

---

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Idle timeout duration (A4) not confirmed by Product Owner | Wrong value ships; either too disruptive or too lax for PHI protection | Surface A4 for explicit sign-off before Sprint 1 close; make the value a configuration setting, not a hardcoded constant, so it can change without a redeploy of logic. |
| Email delivery mechanism (A5) unavailable/unconfigured in the deployment environment | Password recovery — an Acceptance Criterion — silently fails | Confirm SMTP/email-provider credentials are available before this module is marked done; add a clear server-side error/alert if email dispatch fails, and design the email sender as a swappable interface so an admin-issued-code fallback could be added later without redesign. |
| JWT-in-memory storage means a hard page refresh logs the user out unless a refresh mechanism is added | UX friction for a single, presumably long-session, clinical user | Flag as an open UX question for Product Owner: acceptable trade-off for security, or add an httpOnly refresh-token cookie. Documented here rather than decided unilaterally. |
| No rate limiting requested by BRD, but omitting it is a real exposure on a login endpoint | Brute-force credential guessing against the single account | Add lightweight rate limiting (A7) as defensive coding, explicitly flagged as an addition beyond BRD text, not a scope expansion of user-facing functionality. |
| Session/token revocation logic (hybrid JWT + DB session) adds complexity beyond a "simple" module as the BRD frames it | Over-engineering risk for a single-user app | Keep the `Sessions` table minimal (5 fields) and the check a single indexed lookup per request; avoid building broader session-management UI, which stays explicitly out of scope. |
| Stack choice (A1) locks architectural conventions for all 9 remaining modules | Reversing this later is expensive | Confirm A1 with Product Owner/tech lead before Module 2 planning begins, since every subsequent module's plan will assume this stack's layering and conventions. |

---

## Baseline Established for Later Modules

Because this is the first module built, the following are now the **de facto architectural conventions** every subsequent module's plan and implementation should reuse rather than re-decide:

1. **Stack:** Angular (frontend) + ASP.NET Core Web API/C# (backend), EF Core + SQL Server.
2. **Multi-tier project layout:** `/frontend` (Angular) and `/backend` solution split into `Api` (controllers), `BusinessLogic` (services), `DataAccess` (repositories + EF `DbContext`), `Domain` (shared entities/DTOs) — later modules add their own controllers/services/repositories into these same four projects rather than creating parallel structures.
3. **API convention:** versioned base path `/api/v1/<module-area>/...`; controllers thin, business rules in services, persistence in repositories.
4. **Auth pattern:** every protected endpoint validates the bearer JWT + session record via the shared `JwtSessionMiddleware`; every protected Angular route sits behind the shared `AuthGuard`, and every HTTP call goes through the shared `AuthInterceptor`. Later modules should not re-implement auth checks; they depend on this middleware/guard pair.
5. **Shared UI shell:** the authenticated layout (header, logout control, nav) built here is the chrome later modules' feature screens render inside.
6. **Security baseline:** HTTPS end-to-end, encryption at rest, hashed/salted secrets, generic error messaging — carried forward as non-negotiable defaults for any module handling patient data.
