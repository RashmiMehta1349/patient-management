# Provisioning the Single Doctor Account

Per Module 1 Business Rule, exactly one user account exists in Phase 1 and there is no
in-app account-creation UI. The account must be created by the dev/deploy team using the
seed console tool (preferred, because it applies the correct PBKDF2 hash) rather than a raw
SQL `INSERT`, since a hand-written SQL insert cannot correctly produce the salted PBKDF2 hash.

## Usage

```
cd backend
dotnet run --project PatientMgmt.Seed -- "doctor@example.com" "doctor" "<temporary-strong-password>"
```

Or via environment variables (useful for CI/deploy scripts, avoids the password appearing in
shell history):

```
set SEED_USER_EMAIL=doctor@example.com
set SEED_USER_USERNAME=doctor
set SEED_USER_PASSWORD=<temporary-strong-password>
set PATIENTMGMT_CONNECTION=<target connection string>
dotnet run --project PatientMgmt.Seed
```

The tool:
1. Applies any pending EF Core migrations (`db.Database.Migrate()`), creating `Users`,
   `Sessions`, `PasswordResetTokens` tables if they don't yet exist.
2. Refuses to run if a `Users` row already exists (idempotent / single-account guard).
3. Hashes the supplied password with the same `PasswordHasher` (PBKDF2-HMAC-SHA256) used at
   runtime, and inserts the single `User` row.

After provisioning, communicate the temporary password to the doctor out-of-band (not via
this repo) and encourage an immediate password reset via the in-app "Forgot password" flow.
