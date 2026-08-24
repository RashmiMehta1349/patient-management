# Architecture Baseline (established by Module 1: Authentication & Authorization)

This document records the conventions later module plans/implementations should reuse
rather than re-decide, per `Planning\01_Authentication_and_Authorization_Plan.md` ("Baseline
Established for Later Modules").

## Stack
- Frontend: Angular 18 (standalone components, functional guards/interceptors), `/frontend`.
- Backend: ASP.NET Core Web API on .NET 8, EF Core, SQL Server, `/backend`.

## Solution layout (`/backend`)
- `PatientMgmt.Domain` — shared entities (`/Entities`), DTOs (`/Contracts`), configuration
  option classes (`/Options`). No dependencies on other projects. Later modules add their
  own entities/DTOs here.
- `PatientMgmt.DataAccess` — `AppDbContext`, EF Core `/Migrations`, `/Repositories`
  (interface + implementation pairs, e.g. `IUserRepository`/`UserRepository`). Later modules
  add repositories here and register their `DbSet<T>` in `AppDbContext.OnModelCreating`.
- `PatientMgmt.BusinessLogic` — framework-agnostic service classes containing all business
  rules (e.g. `/Auth/AuthService.cs`). Depends on DataAccess repository *interfaces*, not
  EF Core directly, so it stays unit-testable with fakes/mocks.
- `PatientMgmt.Api` — thin controllers (`/Controllers`), cross-cutting middleware
  (`/Middleware`), `Program.cs` (DI wiring, pipeline order, HTTPS/HSTS/CORS/rate limiting).
  Controllers must not contain business rules — delegate to BusinessLogic services.
- `PatientMgmt.Seed` — console provisioning tool pattern (not a UI) for any one-time/admin
  data setup later modules might need; Module 1's instance seeds the single doctor account.
- `PatientMgmt.BusinessLogic.Tests` (xUnit + Moq) — unit tests for service classes, isolated
  from EF Core/HTTP via mocked repository interfaces and an `IClock` abstraction for
  time-dependent logic.
- `PatientMgmt.Api.IntegrationTests` (xUnit + `WebApplicationFactory<Program>` +
  EF Core InMemory) — full-pipeline tests hitting real HTTP endpoints against a swapped-in
  InMemory database. See `CustomWebApplicationFactory` for the pattern to copy: swap
  `DbContextOptions<AppDbContext>` for InMemory, inject `Testing`-only config, replace
  external-integration services (email, etc.) with capturing test doubles.

## API convention
- Versioned base path: `/api/v1/<module-area>/...` (e.g. `/api/v1/auth/...`).
- Controllers are thin; all business rules live in `PatientMgmt.BusinessLogic` services;
  all persistence lives behind `PatientMgmt.DataAccess` repository interfaces.
- Generic, non-specific error responses wherever revealing failure specifics would create
  an enumeration or information-disclosure risk (see `AuthController` for the pattern).

## Auth pattern (every later module's protected endpoints/routes reuse this unmodified)
- **API tier:** `[Authorize]` + the shared `JwtSessionMiddleware`
  (`PatientMgmt.Api/Middleware/JwtSessionMiddleware.cs`), registered in `Program.cs` right
  after `UseAuthentication()`. This middleware is the single place that enforces idle-timeout
  and session revocation (e.g. post password-reset) for every authenticated request across
  every module — do not re-implement session validation per-controller.
- **Angular tier:** every protected route sits under the route tree guarded by
  `core/auth/auth.guard.ts` (`authGuard`), and every HTTP call goes through
  `core/auth/auth.interceptor.ts` (`authInterceptor`), both wired once in `app.config.ts` /
  `app.routes.ts`. Later feature modules add routes as children of the existing
  `ShellComponent` route in `app.routes.ts` rather than creating a parallel guarded tree.
- **Shared UI shell:** `core/shell/shell.component.ts` is the authenticated chrome
  (header + logout) later modules' feature screens render inside via `<router-outlet>`.

## Configuration-driven settings
Business-tunable values (idle timeout, reset token lifetime, rate-limit thresholds, JWT
lifetimes) live in `PatientMgmt.Domain/Options` classes bound from `appsettings.json`
sections (see `AuthOptions`, `SmtpOptions`) — never hardcoded constants — so they can change
without a code deploy. Later modules should follow the same `IOptions<T>` pattern for their
own tunables.

## Security baseline (non-negotiable defaults for any module touching patient data)
- HTTPS/TLS end-to-end (`UseHttpsRedirection` + `UseHsts` outside Development).
- Passwords/secrets hashed with a salted, adaptive algorithm (PBKDF2-HMAC-SHA256 here);
  never logged, never returned in API responses.
- Generic, enumeration-safe error messages on any endpoint whose failure mode could leak
  account/data existence.
- Basic rate limiting on any public, unauthenticated endpoint (see `AddRateLimiter` +
  `[EnableRateLimiting("auth")]` in `Program.cs`/`AuthController.cs`) as defensive coding.
