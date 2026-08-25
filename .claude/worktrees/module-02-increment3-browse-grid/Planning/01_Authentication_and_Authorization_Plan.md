# Module 1: Authentication & Authorization — Technical Plan

## 1. Module Overview

Authentication & Authorization is the entry gate for the entire Patient Management Application. The BRD scopes Phase 1 to a **single, pre-provisioned physician account** — no self-registration, no roles, no multi-user access — so this module's job is narrow but critical: verify the doctor's identity before any clinical screen renders, keep the session alive only while the doctor is active, let the doctor recover access via a password-reset flow if credentials are lost, and ensure all traffic carrying credentials or patient data travels over HTTPS. Every other module (Patient Management through Administration) sits behind this gate; nothing in the application should be reachable without a valid, active session.

## 2. Business Requirements

Traced to `BRD\Doc_BRD_Final.md` → Non-Functional Requirements → Security, and `Modules\01_Authentication_and_Authorization.md`:

- "Secure login (single user authentication)" — BRD Security NFR.
- "Account is pre-provisioned (no self-registration); password recovery flow is included in Phase 1" — BRD Security NFR.
- "Simple inactivity-based session timeout only; no concurrent-session handling needed" — BRD Security NFR.
- "Data encryption (at rest and in transit) as a general best-practice expectation (e.g., HTTPS, encrypted storage); no specific regulatory/compliance standard targeted" — BRD Security NFR.
- "Exactly one user account exists in Phase 1; there is no in-app account creation UI" — Modules\01 §5 Business Rules.
- "The account is pre-provisioned by the development/deployment team, not by the end user" — Modules\01 §5.
- "Passwords must never be stored or transmitted in plain text (hashed at rest, encrypted in transit)" — Modules\01 §5.
- "A password reset must invalidate any previously issued session tokens for security" — Modules\01 §5.
- Explicitly Out of Scope (do not build): user registration/sign-up, multiple accounts/roles, concurrent-session detection/device management, 2FA, OAuth/SSO — Modules\01 §3.
- No audit/log table required — Modules\01 §7 ("audit logging is explicitly out of scope" per CLAUDE.md scope list too).

## 3. Assumptions / Open Questions for Product Owner

The BRD and Module doc intentionally leave some implementation parameters undecided. Rather than deciding unilaterally, the following are called out; the plan below proceeds on the stated default (chosen for lowest risk/complexity, consistent with the prior scaffold referenced in `CLAUDE.md`) but these should be confirmed:

| # | Open Item | Default Assumed in This Plan | Rationale |
|---|---|---|---|
| A1 | Session mechanism: JWT (bearer token, client-stored) vs. server-side cookie session | **JWT bearer token**, stored client-side (per prior scaffold pattern referenced in `CLAUDE.md`: "session token kept in `localStorage`, HTTP interceptor for Bearer auth") | Matches the documented prior working convention; simplest to implement statelessly in ASP.NET Core; BRD does not mandate a mechanism. |
| A2 | Inactivity timeout duration | **20 minutes** | BRD says "simple inactivity-based session timeout only" but does not specify a duration. 20 minutes is a reasonable clinical-app default; must be confirmed with Product Owner before go-live. |
| A3 | Password reset delivery mechanism (real email send vs. dev-only stub/console log) | **Console/log-only stub for now** — the reset link is written to console/logs, not sent via real email. No SMTP/email provider is chosen yet; that decision is explicitly deferred and out of scope for this module's implementation. The `IEmailSender` abstraction is still implemented so a real provider can be swapped in later without touching the auth flow's business logic. | Confirmed with Product Owner: defer picking an email provider; single-user clinic has no SMTP infra provisioned. Modules\01 says "e.g., email-based reset link or admin-issued reset" — the stub satisfies this for Phase 1 dev/demo purposes. |
| A4 | Reset token / JWT lifetime values | Reset token: **30 minutes**; access token: **matches inactivity timeout (20 min sliding)** | Not specified in BRD; short-lived reset tokens reduce replay risk. |
| A5 | Where the pre-provisioned account credentials come from at deployment | **Seed script / environment variables at deploy time**, no in-app creation UI | Consistent with "pre-provisioned... not by the end user" (Modules\01 §5). |

**Action:** These five items should be confirmed with the Product Owner before or during implementation kickoff (A2–A4 have low blast radius and can be adjusted via config later; A1 and A3 affect architecture and should be settled first).

## 4. Workflows

### 4.1 Login
1. Doctor navigates to the app; unauthenticated request to any protected route redirects to `/login`.
2. Doctor enters email/username + password.
3. Client sends credentials over HTTPS to `POST /api/auth/login`.
4. Server looks up the single user by email/username, verifies password hash (constant-time compare via ASP.NET Core Identity/`PasswordHasher`).
5. On success: server issues a signed JWT (claims: user id, email, issued-at, expiry) and updates `LastLoginAt`/`LastActivityAt`.
6. On failure (wrong password or unknown user): server returns a generic 401 with a non-specific message ("Invalid email or password") — never reveals which field was wrong.
7. Client stores the token (per A1) and redirects to the dashboard.

### 4.2 Session Validation / Inactivity Timeout
1. Every authenticated API call passes the bearer token; server middleware validates signature and expiry.
2. Client-side HTTP interceptor attaches the token to every request and tracks last user activity (mouse/keyboard/API activity).
3. If no activity for the configured inactivity window (A2), the client proactively logs the user out and redirects to `/login` with a "session expired" message; server-side, an expired/invalid token on any call returns 401, and the client's interceptor catches global 401s and forces the same redirect (belt-and-suspenders — client timer for UX, server expiry for enforcement).
4. No refresh-token/silent-renewal flow is introduced — matches "simple inactivity-based session timeout only."

### 4.3 Password Recovery — Request Reset
1. Doctor clicks "Forgot password" on the login screen, enters their email.
2. Client calls `POST /api/auth/forgot-password`.
3. Server always responds with a generic success message regardless of whether the email matches (avoids user enumeration) — since there is only one account, this is a minor concern but kept as good practice.
4. If the email matches the single provisioned account: server generates a single-use, cryptographically random reset token, hashes it before storing, sets an expiry (A4), and sends/stubs a reset email containing a link with the raw token as a query parameter.

### 4.4 Password Recovery — Reset Password
1. Doctor opens the reset link, lands on `/reset-password?token=...`.
2. Doctor enters a new password (+ confirmation); client calls `POST /api/auth/reset-password` with the raw token and new password.
3. Server hashes the incoming token and compares against the stored hash, checks expiry and single-use (not already consumed).
4. On success: server updates the password hash, invalidates the reset token (marks consumed), and invalidates all previously issued session tokens (per Business Rule) — implemented by bumping a `TokenVersion`/`SecurityStamp` claim on the user record that is checked on every JWT validation.
5. Doctor is redirected to login with a success message.

### 4.5 Logout
1. Doctor clicks "Logout."
2. Client calls `POST /api/auth/logout` (best-effort server-side no-op or token-version bump if "logout everywhere" is desired — not required by BRC since no concurrent sessions) and clears the client-stored token.
3. Client redirects to `/login`.

## 5. Architecture Approach

- **Server framework**: ASP.NET Core Web API (.NET), using ASP.NET Core Identity's `PasswordHasher<TUser>` for hashing (PBKDF2-based, salted) rather than a custom hashing scheme — avoids inventing crypto, satisfies "hashed at rest."
- **Session mechanism**: Stateless JWT bearer tokens (per Assumption A1), signed with a server-held symmetric key (or asymmetric in production), validated via ASP.NET Core's `AddAuthentication().AddJwtBearer()` middleware. Chosen over server-side session cookies because it matches the documented prior scaffold convention and keeps the API stateless/simple for a single-user app; CSRF is a non-issue since the token is not cookie-based.
- **Forced logout on reset**: implemented via a `SecurityStamp` (GUID) column on `Users`, embedded as a JWT claim at issuance and re-checked on every request; password reset regenerates the stamp, which invalidates all previously issued tokens without needing a token blacklist/store.
- **Transport security**: HTTPS enforced at the ASP.NET Core middleware level (`UseHttpsRedirection`, HSTS in production) and at the reverse-proxy/hosting layer; no plaintext HTTP endpoint accepts credentials.
- **Encryption at rest**: password hash and reset-token hash are the only sensitive columns in this module; both stored hashed (not encrypted-reversible), consistent with "never stored... in plain text." Broader database-at-rest encryption is an infrastructure concern owned by Module 9 (Data Backup & Reliability), not duplicated here.
- **Validation placement**: input shape/format validation (required fields, email format, password complexity minimums) happens both client-side (fast feedback) and server-side (source of truth, via model validation attributes/FluentValidation) — client-side validation is never trusted alone.
- **No user-management CRUD**: because there is exactly one account and no in-app creation UI, there is intentionally no `POST /api/users` or admin user-management endpoint in this module. The single account is created by a seed step at deployment.
- **Synchronous flow throughout**: login/reset/logout are all short-lived, low-throughput operations (single user) — no async job queue or background processing needed; email sending for reset links can be fire-and-forget but does not need a durable queue given expected volume (i.e., essentially never).

## 6. Database Entities

### `Users` table (single row in Phase 1, but modeled as a normal table for straightforward EF Core mapping and future extensibility)

| Field | Type | Notes |
|---|---|---|
| `Id` | GUID (PK) | Primary key. |
| `Email` | nvarchar(256), unique index | Used as login identifier. |
| `PasswordHash` | nvarchar(max) | Output of `PasswordHasher<TUser>` (includes algorithm marker + salt). |
| `SecurityStamp` | nvarchar(64) | GUID string; regenerated on password reset to invalidate prior tokens. |
| `LastLoginAt` | datetime2, nullable | Set on successful login. |
| `LastActivityAt` | datetime2, nullable | Updated on each authenticated request (best-effort, for diagnostics only — not used for server-enforced timeout, which relies on JWT expiry). |
| `CreatedAt` | datetime2 | Set at seed time. |
| `UpdatedAt` | datetime2, nullable | Set on any update (password change, etc.). |

### `PasswordResetTokens` table

| Field | Type | Notes |
|---|---|---|
| `Id` | GUID (PK) | Primary key. |
| `UserId` | GUID (FK → `Users.Id`) | Required. |
| `TokenHash` | nvarchar(256) | SHA-256 hash of the raw token; raw token never stored. |
| `ExpiresAt` | datetime2 | Now + reset-token lifetime (Assumption A4, default 30 min). |
| `ConsumedAt` | datetime2, nullable | Set when the token is successfully used; null = still valid/unused. |
| `CreatedAt` | datetime2 | Issued timestamp. |

**Indexes**: unique index on `Users.Email`; index on `PasswordResetTokens.UserId` and on `PasswordResetTokens.TokenHash` (lookup path during reset).

**FK relationships**: `PasswordResetTokens.UserId` → `Users.Id` (cascade delete, though in practice the single user row is never deleted).

No session/token table is required since JWTs are stateless and self-validating (expiry + `SecurityStamp` claim check against the DB row).

## 7. APIs

All endpoints under `/api/auth`. None require auth except where noted.

| Method | Path | Purpose | Auth Required |
|---|---|---|---|
| POST | `/api/auth/login` | Validate credentials, issue JWT | No |
| POST | `/api/auth/logout` | Client-side token discard; optional server no-op/telemetry | Yes |
| GET | `/api/auth/me` | Return current authenticated user's basic profile (email, last login) — used by client on app load to confirm session validity | Yes |
| POST | `/api/auth/forgot-password` | Request a password reset link/token for the given email | No |
| POST | `/api/auth/reset-password` | Consume a reset token and set a new password | No (token itself is the credential) |

All other application endpoints (Modules 2–10) require the `Authorize` attribute with a valid, unexpired, `SecurityStamp`-matching JWT — enforced globally via default policy, not opted into per-controller.

## 8. UI / Screens

- **Login screen** (`/login`): email + password fields, "Forgot password?" link, generic inline error banner on failure, submit-disabled while in flight.
- **Forgot Password screen** (`/forgot-password`): single email field, generic confirmation message on submit ("If that email is registered, a reset link has been sent") regardless of match outcome.
- **Reset Password screen** (`/reset-password?token=...`): new password + confirm-password fields, client-side match/complexity check, success state redirecting to login; explicit error state if the token is invalid/expired ("This reset link is invalid or has expired — request a new one").
- **Session-expired interstitial**: not a full screen, but a shared toast/banner triggered by the HTTP interceptor's global 401 handler, shown just before redirecting to `/login`.
- **Auth guard**: route guard applied to every non-auth route; unauthenticated access attempts redirect to `/login` and (optionally) preserve the originally requested URL for post-login redirect.
- **Logout control**: a persistent nav-bar/header element (visible on all authenticated screens) triggering the logout workflow.

## 9. Dependencies

- **Upstream**: None — this is the foundational module (per dependency graph, Module 1 has no incoming dependencies).
- **Downstream**: Every other module depends on this one for gated access — Patient Management (2), Appointment Management (3), Consultation & Clinical Records (4), Prescription & Medication Management (5), Patient History (6), Search & Navigation (7), Data Export (8), and Administration (10) all require a valid session before their screens/APIs are reachable. Data Backup & Reliability (9) is largely infra-level and does not directly consume this module's APIs but relies on the `Users` table being included in backup scope.

## 10. Implementation Tasks

1. Scaffold the .NET solution and projects (see File Structure §11) under `src/`; wire up project references (Api → Application → Domain; Infrastructure → Application/Domain).
2. Define `User` and `PasswordResetToken` domain entities in `PatientManagement.Domain`.
3. Configure EF Core `DbContext`, migrations, and connection string in `PatientManagement.Infrastructure`; create initial migration for `Users` and `PasswordResetTokens`.
4. Implement the seed step (console command or startup-conditional seeder) that creates the single provisioned account from configuration/environment variables (email + initial password), never via an API.
5. Implement password hashing via `PasswordHasher<User>`; write hash/verify wrapper in Application layer.
6. Implement JWT issuance service (claims: `sub`, `email`, `security_stamp`, `exp`) and configure `AddJwtBearer` validation middleware, including `SecurityStamp` claim re-check against the DB on each request.
7. Implement `POST /api/auth/login` endpoint + Application-layer command/handler; generic error on failure.
8. Implement `POST /api/auth/forgot-password`: generate token, hash+store, send/stub email; always return generic success response.
9. Implement `POST /api/auth/reset-password`: validate token (hash match, not expired, not consumed), update password hash, regenerate `SecurityStamp`, mark token consumed.
10. Implement `GET /api/auth/me` and `POST /api/auth/logout`.
11. Configure global `[Authorize]` default policy and HTTPS redirection/HSTS middleware in `Program.cs`.
12. Build Angular auth module: login, forgot-password, reset-password components/screens.
13. Build Angular `auth.interceptor` (attach bearer token, global 401 handling → redirect to login) and `auth.guard` (route protection).
14. Build client-side inactivity timer (resets on user activity, triggers logout at the configured threshold — Assumption A2).
15. Add configuration for reset-token/JWT lifetimes and email delivery mechanism (dev stub vs. real provider) via environment-specific settings.
16. Write unit, integration, and E2E tests per §12 Test Strategy.
17. Confirm Assumptions A1–A5 with Product Owner; adjust config/plan if any default is rejected.

## 11. File Structure

Multi-tier layout under a top-level `src/`, per project convention (Angular client + multi-project .NET solution):

```
src/
  client/                                  # Angular application
    src/
      app/
        core/
          auth/
            auth.service.ts
            auth.guard.ts
            auth.interceptor.ts
            inactivity-timer.service.ts
        features/
          auth/
            login/
              login.component.ts / .html / .scss
            forgot-password/
              forgot-password.component.ts / .html / .scss
            reset-password/
              reset-password.component.ts / .html / .scss
      environments/
        environment.ts
        environment.prod.ts
    angular.json
    package.json

  server/
    PatientManagement.sln
    PatientManagement.Api/                 # presentation/host tier
      Controllers/
        AuthController.cs
      Middleware/
        (HTTPS/HSTS config lives in Program.cs)
      Program.cs
      appsettings.json
      appsettings.Development.json

    PatientManagement.Application/         # business logic / use cases
      Auth/
        Commands/
          LoginCommand.cs
          ForgotPasswordCommand.cs
          ResetPasswordCommand.cs
        Services/
          IJwtTokenService.cs
          IPasswordHasherService.cs
          IEmailSender.cs
        Dtos/
          LoginRequestDto.cs
          LoginResponseDto.cs
          ForgotPasswordRequestDto.cs
          ResetPasswordRequestDto.cs

    PatientManagement.Domain/              # entities, no framework dependencies
      Entities/
        User.cs
        PasswordResetToken.cs

    PatientManagement.Infrastructure/       # EF Core, external services
      Persistence/
        PatientManagementDbContext.cs
        Migrations/
        Configurations/
          UserConfiguration.cs
          PasswordResetTokenConfiguration.cs
      Services/
        JwtTokenService.cs
        PasswordHasherService.cs
        SmtpEmailSender.cs / DevConsoleEmailSender.cs
      Seed/
        UserSeeder.cs

    PatientManagement.Tests/
      Unit/
        Auth/
          LoginCommandTests.cs
          ResetPasswordCommandTests.cs
      Integration/
        Auth/
          AuthEndpointsTests.cs
```

## 12. Security Considerations

Tied to BRD Security NFRs (`Doc_BRD_Final.md` → Non-Functional Requirements → Security):

- **Password storage**: hashed via `PasswordHasher<User>` (PBKDF2, salted, iteration count per current .NET defaults) — satisfies "passwords must never be stored... in plain text."
- **Transport encryption**: HTTPS enforced end-to-end (`UseHttpsRedirection`, HSTS); no endpoint in this module accepts plaintext HTTP — satisfies "data encryption... in transit."
- **Generic error responses**: login failures and forgot-password requests return identical, non-specific messages regardless of which part failed or whether the account exists — mitigates user enumeration and credential-guessing feedback loops (Modules\01 Acceptance Criteria: "no hint on which field is wrong").
- **Reset token handling**: tokens are single-use (`ConsumedAt`), time-limited (Assumption A4), and stored hashed (never the raw token) — reduces replay/interception risk even if the token table is exposed.
- **Forced re-authentication on reset**: `SecurityStamp` invalidation ensures all previously issued JWTs stop working immediately after a password reset — satisfies "a password reset must invalidate any previously issued session tokens."
- **No plaintext secrets in source control**: JWT signing key, seed-account initial password, and SMTP credentials are read from environment variables / secrets configuration, never hardcoded or committed.
- **Rate limiting / brute-force**: not explicitly required by the BRD, but a lightweight login-attempt throttle (e.g., basic rate limiting middleware) is recommended as a defense-in-depth measure given this endpoint is internet-facing; flagged here as a low-cost addition, not a hard BRD requirement — implement if time allows, otherwise note as a follow-up.
- **Session timeout enforcement**: server-side JWT expiry is the authoritative enforcement point (client inactivity timer is UX-only) — satisfies "inactivity-based session timeout" without relying on a client that could be tampered with.
- **No account enumeration via timing**: forgot-password handler performs equivalent work (or a deliberate delay) whether or not the email matches, to avoid timing-based enumeration — minor given single-user scope, but low-cost to include.

## 13. Test Strategy

**Unit tests** (Application layer, mocked infrastructure):
- Login command: valid credentials → success + token issuance; wrong password → generic failure; unknown email → generic failure (same message/shape as wrong password).
- Password hashing service: hash-then-verify round trip succeeds; verify fails against a tampered hash.
- Forgot-password command: matching email → token generated, hashed, stored, email-send invoked; non-matching email → no token created, but same generic response returned.
- Reset-password command: valid unexpired unconsumed token → password updated, `SecurityStamp` regenerated, token marked consumed; expired token → rejected; already-consumed token → rejected; malformed token → rejected.

**Integration tests** (API + real EF Core against a test database, e.g., SQLite in-memory or a dockerized SQL Server test instance):
- `POST /api/auth/login` end-to-end with seeded user: 200 + JWT on success, 401 on failure.
- `GET /api/auth/me` with valid token → 200 with user info; with missing/expired/tampered token → 401.
- Full forgot-password → reset-password → login-with-new-password round trip.
- After a password reset, a JWT issued before the reset is rejected on a subsequent authenticated call (verifies `SecurityStamp` check).
- Any protected endpoint (stub controller or an existing Module 2+ endpoint once available) returns 401 without a token and 200 with a valid one.

**E2E tests** (Angular + API, e.g., Playwright/Cypress against a running stack):
- User logs in with valid credentials and lands on the dashboard.
- User enters wrong password, sees a generic error, remains on login.
- User requests password reset, follows the (stubbed) email link, sets a new password, logs in successfully with the new password, and confirms the old password no longer works.
- User is idle past the configured inactivity threshold and is redirected to login with a session-expired message on the next action.
- User logs out manually and is redirected to login; back-navigation to a protected route redirects to login again (no cached authenticated view).

**Performance tests**:
- Login and `GET /api/auth/me` response time under typical load stays well within the BRD's global "< 2 second page load" NFR — target sub-300ms for these specific calls given single-user, low-volume expected traffic.

## 14. Acceptance Criteria

Directly aligned to `Modules\01_Authentication_and_Authorization.md` §10 and BRD Security NFRs:

- Given valid credentials, the doctor can log in and reach the application dashboard.
- Given invalid credentials (wrong password or unknown account), login is rejected with a single generic error message that does not indicate which field was incorrect.
- After the configured inactivity period elapses, the current session is no longer accepted by the server, and the next user action (client-detected or server-401-triggered) redirects to the login screen.
- The doctor can request a password reset, receive a reset link/token, and successfully set a new password without any developer/manual database intervention.
- Following a password reset, any session token issued before the reset is rejected by the server on subsequent requests.
- All requests carrying credentials or patient data occur over HTTPS; no plaintext HTTP path exists for these operations.
- No screen other than Login / Forgot Password / Reset Password renders any content without a valid, currently-accepted session token.
- No in-app UI exists for creating additional user accounts or assigning roles (out-of-scope items are verifiably absent, not just unused).

## 15. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| BRD/Module doc leave session mechanism (JWT vs cookie), timeout duration, and reset-email delivery undecided (Assumptions A1–A4) | Rework of client storage strategy or config if Product Owner disagrees with defaults | Explicitly documented as assumptions in §3; confirm with Product Owner before/at implementation kickoff; timeout duration and token lifetimes are externalized to config so they can change without code changes. |
| No production email provider configured for password-reset delivery | Reset flow works in dev (console stub) but not demonstrably in production until an SMTP/email provider is chosen | `IEmailSender` abstraction with swappable implementation (dev stub vs. SMTP/SendGrid) isolates this decision; flagged as a deployment-readiness item, not a code blocker. |
| Single point of failure: if the one account's credentials are lost and email delivery is unavailable, doctor could be locked out | Total loss of application access | Seed script supports re-provisioning credentials via a deployment-level command (not an in-app API) as a break-glass path; document this operational procedure alongside deployment runbook (owned partly by Module 9/Administration). |
| Client-side inactivity timer could be disabled/tampered with by the user (browser devtools) | Perceived security gap if relied upon alone | Server-side JWT expiry is the authoritative enforcement mechanism (§12); client timer is UX-only, explicitly documented as such. |
| Storing JWT in `localStorage` is vulnerable to XSS token theft (vs. httpOnly cookie) | If an XSS vulnerability exists elsewhere in the app, tokens could be exfiltrated | Accepted trade-off per Assumption A1 (matches prior scaffold convention); mitigate via standard Angular XSS hygiene (no `innerHTML` binding of untrusted content, CSP headers) — flag for code-review-agent to verify no unsafe DOM sinks are introduced in later modules. |
| No audit/rate-limiting on login attempts (audit logging explicitly out of scope per BRD) | Vulnerable to unthrottled brute-force attempts against the single account | Recommend lightweight rate-limiting middleware as defense-in-depth (§12); not a hard BRD requirement, so scoped as optional/follow-up rather than blocking acceptance. |
