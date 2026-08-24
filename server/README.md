# PMA Server — Module 1: Authentication & Authorization

Backend for the Patient Management Application. This module implements login,
logout, session/inactivity-timeout handling, and self-service password reset,
per `Planning\01_Authentication_and_Authorization_Plan.md`.

## Prerequisites

- Node.js 18+ (project developed/tested with the environment's installed Node version)
- A PostgreSQL instance reachable via `DATABASE_URL`

## Setup

```bash
cd server
npm install
cp .env.example .env   # then edit values, especially SESSION_JWT_SECRET
npx prisma migrate dev --name init   # creates users, sessions, password_reset_tokens tables
```

## Seeding the pre-provisioned doctor account

Per the BRD/Module 1 doc, there is exactly one user account in Phase 1 and no
in-app registration UI. The account must be seeded once, out-of-band:

```bash
SEED_DOCTOR_EMAIL="doctor@example.com" SEED_DOCTOR_PASSWORD="ChooseAStrongPassword1" npm run prisma:seed
```

If the env vars are omitted, documented local-dev-only defaults are used
(`doctor@example.com` / `ChangeMe123!`) — **never use these defaults outside
local development.** The seed script is idempotent: re-running it for an
email that already exists is a no-op.

## Running

```bash
npm run dev     # ts-node-dev, local development
npm run build && npm start   # production build
```

## Environment variables

See `.env.example` for the full list with inline documentation. Highlights:

| Variable | Purpose |
|---|---|
| `DATABASE_URL` | PostgreSQL connection string (Prisma format). |
| `SESSION_JWT_SECRET` | Signs session tokens. Must be a long random secret in any non-local environment. |
| `SESSION_INACTIVITY_TIMEOUT_MINUTES` | Idle timeout before a session is rejected (default 15, confirmed Product Owner decision). |
| `SESSION_HARD_TTL_HOURS` | Hard ceiling on session lifetime regardless of activity (defense in depth). |
| `PASSWORD_RESET_TOKEN_TTL_MINUTES` | How long a password-reset link remains valid. |
| `FORCE_HTTPS` | App-layer HTTPS enforcement safety net; set to `false` only for local dev without a TLS-terminating proxy in front of the app. Must be `true` (default) elsewhere. |
| `TRUST_PROXY` | Set `true` when running behind a reverse proxy that terminates TLS and sets `X-Forwarded-Proto`. |
| `CORS_ORIGIN` | Allowed origin for the Angular frontend. |
| `SEED_DOCTOR_EMAIL` / `SEED_DOCTOR_PASSWORD` | Used only by `npm run prisma:seed`. |

## Password reset email delivery — dev-only stub

Per the confirmed project decision, real email provider integration is
**deferred and out of scope for this build**. `src/modules/auth/email.service.ts`
logs the reset link (including the raw, single-use token) to the console
instead of sending an email. This satisfies the plan's requirement for a
swappable `EmailService` interface — a real SMTP/transactional-email
implementation can be substituted later (`DevStubEmailService` -> e.g.
`SmtpEmailService`) without touching `auth.service.ts` or `auth.controller.ts`.

## Tests

```bash
npm test              # all tests
npm run test:unit
npm run test:integration
```

Note: this environment has no live PostgreSQL/Docker available, so
`tests/integration/auth.routes.test.ts` runs against an in-memory
Prisma-compatible fake rather than a real database (see the comment at the
top of that file). When a real Postgres test database is available, remove
the `jest.mock('../../src/db/prisma', ...)` call and point `DATABASE_URL` at
a disposable test database; the same assertions should hold.

## HTTPS

The primary enforcement point is expected to be a TLS-terminating reverse
proxy / hosting layer (HSTS, HTTP->HTTPS redirect). `src/middleware/https-enforce.middleware.ts`
adds an application-level safety net: any request without `req.secure` or an
`x-forwarded-proto: https` header is rejected with 403 unless `FORCE_HTTPS=false`.
