# Module 3: Appointment Management — Implementation Plan

## 1. Module Overview

Appointment Management is the bridge between the anchor `Patient` entity (Module 2, fully built) and the clinical consultation workflow (Module 4, not yet started). It gives the doctor a way to schedule a visit against an existing patient, see the day's plan at a glance in a daily list, and keep that list's statuses (`Scheduled` / `Completed` / `Cancelled` / `No-show`) accurate as the day unfolds. The BRD deliberately favors real-world clinic flexibility over rigid scheduling: overlapping time slots are *warned about, never blocked*, so the doctor can still fit in an urgent walk-in or a double-booked patient. This module introduces the first entity in the codebase with a required FK to `Patients` — every `Appointment` row must reference an existing patient, and the daily list is expected to be the doctor's primary "start of day" screen once built, foreshadowing the same navigation-anchor pattern already stubbed out on the Patient Detail screen ("Appointments" tab, currently a disabled placeholder per `Planning\02_Patient_Management_Plan.md` §9a.7).

This plan covers full Module 3 scope — Schedule, Daily List, Status Update, Overlap Detection — per `Modules\03_Appointment_Management.md`, sequenced into increments that mirror how Module 2 was built (schema + core create/read first, then richer UI/interaction), so a developer can pick up Increment 1 immediately after this plan is approved.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Schedule an appointment tied to an existing patient, with date/time | BRD `Functional Requirements → Appointment Management`; Modules\03 §4 #1 |
| R2 | View a daily appointment list, in time order | BRD `Functional Requirements → Appointment Management`; Modules\03 §4 #2, §10 |
| R3 | Update appointment status through Scheduled → Completed / Cancelled / No-show | BRD `Functional Requirements → Appointment Management`; Modules\03 §4 #3 |
| R4 | Warn (not block) on overlapping time slots, allow save to proceed | BRD `Functional Requirements → Appointment Management`: "warn the doctor of the conflict but allow it to be saved"; Modules\03 §4 #4, §5 |
| R5 | Every appointment must reference a patient — no anonymous/placeholder appointments | Modules\03 §5 |
| R6 | Status is a fixed four-value enum, no sub-statuses | Modules\03 §5 |
| R7 | No reminders/notifications triggered by status changes | Modules\03 §5; BRD Out of Scope: "Follow-up alerts/reminders" |
| R8 | Daily list should load within the < 2 second target | BRD `Non-Functional Requirements → Performance`; Modules\03 §11 |
| R9 | Status changes should be a one- or two-click action (fast consultation support) | Modules\03 §11 |
| R10 | Must be logged in (JWT-authenticated) to access appointment data | BRD Security NFR; module Dependencies (Auth) |

**Explicitly out of scope for this module** (do not build): automated reminders/alerts, multi-doctor or resource calendars, recurring appointment series, online/patient-initiated booking (Modules\03 §3 Out of Scope; BRD Out of Scope list). If asked for any of these, flag the conflict rather than building it.

**Explicit assumption flagged for Product Owner** (BRD/Modules\03 is silent on this): default appointment slot/duration length is not specified anywhere in the BRD or Modules\03 — the module description only says "date/time," not a duration or end-time field. This plan treats an appointment as a **single point-in-time entry (date + start time only, no explicit end time/duration)**, with overlap detection defined against a fixed, configurable assumed slot length (see §4, §9 Open Question 1) rather than an end time the doctor enters. This is a genuine open question, not a silently made call — Product Owner sign-off requested before Increment 1 closes.

## 3. Workflows

### 3.1 Schedule Appointment
1. Doctor reaches the "New Appointment" action from one of two entry points: the Daily Appointment List screen ("Add Appointment" button) or the Patient Detail screen's "Appointments" tab (now wired to real data instead of the disabled placeholder — see §7).
2. If entering from the Daily List, the patient must be selected via a patient picker (name/phone search, reusing `patient.service.ts`'s existing search); if entering from Patient Detail, the patient is pre-selected from the route context.
3. Doctor enters date, time, and optional notes.
4. Client performs inline validation (patient selected, date not blank, time not blank).
5. On submit, client calls `POST /api/appointments`. Before the API responds, or as part of the same response, the server performs overlap detection (§3.4) — this is advisory, so a warning does not block the request; the API creates the appointment either way and returns whether a conflict was detected so the client can surface the warning post-save or pre-confirm (see §4 for exact placement decision).
6. API validates payload server-side (patient exists, date/time present), persists a new `Appointment` row (`Status = Scheduled` by default), returns the created appointment.
7. Client navigates to (or refreshes) the Daily Appointment List for that date, or shows an inline confirmation with an overlap warning banner if one was detected.

### 3.2 View Daily Appointment List
1. Doctor navigates to "Appointments" from the header nav (new tab, alongside "Patients" — see §7).
2. Default view is today's date; a date picker/prev-next control lets the doctor navigate to other days.
3. Client calls `GET /api/appointments?date={yyyy-MM-dd}`.
4. API returns all appointments for that date, ordered by time ascending, each row showing patient name (joined), time, status, and a quick-action control.
5. Empty state ("No appointments scheduled for this day") with a shortcut to "Add Appointment" if the list is empty.

### 3.3 Update Appointment Status
1. From the Daily List, doctor selects a status-change control (e.g., a dropdown or one-click action buttons) on a given row.
2. Client calls `PATCH /api/appointments/{id}/status` (or `PUT`, see §4) with the new status value.
3. API validates the requested status is one of the four allowed values, updates the row, returns the updated appointment.
4. Client updates the row in place in the Daily List without a full page reload (R9 — one/two-click, fast).

### 3.4 Overlap Detection (embedded in Schedule Appointment)
1. On `POST /api/appointments` (create) — and, per the same business rule, on any `PUT` that changes an existing appointment's date/time — the API queries existing `Scheduled`-or-any-status appointments for the same date whose time window intersects the new appointment's assumed slot window.
2. If one or more overlaps are found, the API includes a non-blocking `hasOverlapWarning: true` (plus the conflicting appointment(s) summary) in the response payload alongside the successfully created/updated appointment — the save always succeeds regardless.
3. Client renders the warning as a dismissible banner/toast ("This overlaps with an existing appointment for {patient} at {time}") after the save completes, or as a pre-save confirmation dialog ("This time overlaps with an existing appointment — save anyway?") — see §4 for the chosen placement and rationale.
4. Cancelled/No-show appointments are excluded from overlap comparison (a cancelled slot is not really "occupying" the day) — flagged as an implementation interpretation of "existing appointment," not explicit in the BRD; reasonable default, confirm with Product Owner if it matters.

## 4. Architecture Approach

- **Layering**: Same Clean Architecture split as Module 2 — `PatientManagement.Domain\Entities\Appointment.cs`, `PatientManagement.Application\Appointments\` (DTOs, Commands, Queries, `IAppointmentRepository`, validation), `PatientManagement.Infrastructure\Persistence\Configurations\AppointmentConfiguration.cs` + repository + migration, `PatientManagement.Api\Controllers\AppointmentsController.cs`, tests under `PatientManagement.Tests\Unit\Appointments` / `Integration\Appointments`. This module lives in the same solution/assemblies as Patients, not a new project — matches the existing single-solution structure (no `AppointmentManagement.*` project split was introduced for Module 2's `Patients`, so Module 3 follows suit).
- **CQRS-lite via Commands/Queries**, matching the established pattern: `CreateAppointmentCommand`, `UpdateAppointmentStatusCommand`, `GetAppointmentsByDateQuery`. A full `UpdateAppointmentCommand` (edit date/time/notes) is included per Modules\03's overlap-detection business rule implicitly covering edits too, but is a lower-priority increment (see §9) since the BRD's primary described flows are Schedule + Status Update, not general reschedule; flagged as an assumption that reschedule is in scope by extension of "warn on overlap," not explicitly called out as its own functionality in Modules\03 §4.
- **`Result<T>` reuse**: extend the existing `PatientManagement.Application.Common.Result<T>` (already carries `IsNotFound`) for `CreateAppointmentCommand`/`UpdateAppointmentStatusCommand` outcomes — no new result type needed, consistent with Module 2's approach. Not-found here means either "appointment not found" (status update) or "patient not found" (create against a bad `PatientId`), both mapped to `Result<T>.NotFound(...)`.
- **Overlap detection placement — chosen approach**: computed server-side, inside `CreateAppointmentCommandHandler` / `UpdateAppointmentCommandHandler`, as a **non-blocking annotation on the success response**, not a pre-flight "check first, then confirm" round trip. Rationale: matches the BRD's literal wording ("warn the doctor of the conflict but allow it to be saved" — the save happens, the warning is informational), keeps the API contract simple (one call, one response, extra `hasOverlapWarning`/`conflictingAppointments` fields), and avoids a two-step client flow (check-then-commit) that risks a race condition between check and save with no real benefit in a single-user app. The client renders the warning as a post-save toast/banner (§3.4 step 3, first option), not a pre-save confirm dialog — simpler UX, and consistent with "advisory, never blocking."
- **Overlap window definition — assumption requiring Product Owner input**: since no end-time/duration is captured (see §2 assumption), overlap is computed as "same date, and the two appointments' start times are within `N` minutes of each other," where `N` is a fixed assumed slot length. This plan proposes **N = 30 minutes** as a reasonable default GP consultation slot, configurable via `appsettings.json` (`AppointmentOptions:SlotMinutes`) rather than hardcoded, so it can be tuned without a redeploy-and-recompile cycle. Flagged explicitly in Open Questions (§ below) — do not treat 30 as final without sign-off.
- **Validation placement**: client-side for responsiveness (patient selected, date/time present), server-side in the Application layer as the actual enforcement point (mirrors Module 2's `PatientValidation.cs` precedent) — extracted into a shared `AppointmentValidation.cs` static helper reused by create and status-update/edit handlers.
- **Patient existence check on create**: `CreateAppointmentCommandHandler` calls `IPatientRepository.GetByIdAsync(patientId)` (already exists from Module 2) before persisting — if the patient doesn't exist, return `Result<AppointmentDto>.Failure("Patient not found.")` (a `400`, since the client is expected to only ever submit a `PatientId` obtained from a real patient picker/context — this is a data-integrity guard, not an expected user-facing 404 flow, unlike Module 2's `GetById` 404 which is a normal navigation outcome).
- **Status transition validation**: the four statuses are a fixed enum (Modules\03 §5 — "no additional sub-statuses"); this plan does **not** enforce a strict state machine (e.g., blocking `Completed → Scheduled`) beyond validating the submitted value is one of the four allowed strings, since Modules\03/BRD do not specify transition rules — only that the doctor can "update appointment status" among the four. Flagged as a scope-limiting decision: if the Product Owner wants transition guards (e.g., can't un-cancel), that's a new requirement, not implied by the current docs.
- **Daily list query shape**: unpaginated for a single day — Modules\03 §11 targets a fast (< 2s), same-day list, and a single clinic's single day of appointments is inherently small volume (unlike Module 2's full patient roster, which justified pagination). No pagination on `GET /api/appointments?date=`, consistent with the low-volume, single-day-scoped nature of this screen.
- **Status update endpoint shape**: `PATCH /api/appointments/{id}/status` with a small `{ status: string }` body, rather than requiring the full `PUT` payload Module 2 used for patient edits — status change is the module's headline fast-interaction requirement (R9, "one- or two-click"), so a minimal dedicated endpoint avoids the client needing to resend date/time/notes just to flip a status. A separate `PUT /api/appointments/{id}` (full edit: date/time/notes) is a distinct, lower-priority endpoint (§9 Increment 3) for the reschedule case.
- **Rendering**: Angular standalone components under `features/appointments/*` (mirroring `features/patients/*`): `list` (daily list), `form` (schedule/edit), reusing the existing patient-picker pattern by leaning on `patient.service.ts`'s `list({query})` search rather than duplicating patient-lookup logic.
- **Auth**: All endpoints behind the existing `RequireAuthenticatedUser` fallback policy — no new anonymous surface.
- **Navigation integration**: adds an "Appointments" tab to `AppShellComponent`'s nav list (alongside "Patients"), and wires the previously-disabled "Appointments" placeholder link on `PatientDetailComponent` (§9a.7 of the Module 2 plan) to route to the Daily List pre-filtered/scoped to that patient (or, more precisely, to a patient-scoped appointment view — see §7 UI note on scope of this cross-navigation).

## 5. Database Entities

### `Appointments` table

| Field | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` (GUID), PK | Matches `Patient`/`User` PK convention |
| `PatientId` | `uniqueidentifier`, required, FK → `Patients.Id` | R5 — every appointment must reference a patient; no cascade delete needed since Module 2 has no delete endpoint (nothing to cascade from) |
| `AppointmentDate` | `date`, required | Split from time for efficient day-scoped querying (`WHERE AppointmentDate = @date`) — supports the daily-list query directly without a datetime-range predicate |
| `AppointmentTime` | `time`, required | Start time only, per the §2/§4 duration assumption |
| `Status` | `nvarchar(20)`, required | One of `Scheduled` / `Completed` / `Cancelled` / `NoShow` (stored as string for readability in raw DB queries, matching `Patient.Gender`'s string-picklist precedent rather than an EF-mapped C# enum with numeric storage) |
| `Notes` | `nvarchar(500)`, nullable | Modules\03 §7 lists `notes (optional)` |
| `CreatedAt` | `datetime2`, required | Audit/history ordering |
| `UpdatedAt` | `datetime2`, required | Bumped on every status change or edit |

**Indexes**: non-clustered index on `AppointmentDate` (daily list query, R8 performance target), non-clustered index on `PatientId` (patient-scoped appointment lookups, cross-navigation from Patient Detail, and future Module 4/6 joins). **FK relationship**: `Appointments.PatientId → Patients.Id`, required (`NOT NULL`), `ON DELETE NO ACTION`/restrict (no delete endpoint exists on `Patients`, so this is defensive rather than load-bearing today).

## 6. APIs

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `POST` | `/api/appointments` | Schedule a new appointment for an existing patient | Bearer JWT required | `201` + `AppointmentDto` (includes `hasOverlapWarning` + `conflictingAppointments` summary if applicable) | `400` invalid payload / unknown `PatientId` |
| `GET` | `/api/appointments?date={yyyy-MM-dd}` | Daily appointment list for the given date, time-ordered | Bearer JWT required | `200` + `AppointmentDto[]` | `400` invalid/missing date |
| `GET` | `/api/appointments/{id}` | Retrieve a single appointment (supports edit-form pre-population) | Bearer JWT required | `200` + `AppointmentDto` | `404` unknown id |
| `PATCH` | `/api/appointments/{id}/status` | Update status only (Scheduled/Completed/Cancelled/No-show) | Bearer JWT required | `200` + updated `AppointmentDto` | `400` invalid status value / `404` unknown id |
| `PUT` | `/api/appointments/{id}` | Full edit (date/time/notes) — reschedule flow, Increment 3 | Bearer JWT required | `200` + updated `AppointmentDto` (with overlap re-check) | `400` invalid payload / `404` unknown id |
| `GET` | `/api/appointments?patientId={id}` | Patient-scoped appointment list (cross-navigation from Patient Detail) | Bearer JWT required | `200` + `AppointmentDto[]`, time/date-ordered | — (empty array if none) |

All routes sit behind the existing fallback `RequireAuthenticatedUser` policy. `GET /api/appointments` accepts either `date` or `patientId` as a filter (mutually exclusive in practice, mirroring how Module 2's `GET /api/patients` branches on `query` presence) — exact single-vs-two-endpoint decision left to the developer to match whichever reads more clearly during implementation, but the contract (parameters and response shape) is fixed here. `404`/`400` bodies follow the established `{ message: "..." }` convention from `PatientsController`.

## 7. UI / Screens

- **App Shell nav update**: add an "Appointments" tab to `AppShellComponent`'s existing nav tab list (`core/shell/app-shell.component`), alongside "Patients," routing to `appointments` (Daily List).
- **Daily Appointment List screen** (`features/appointments/list`): date picker/prev-next-day controls (defaulting to today), table of the selected day's appointments (columns: Time, Patient Name, Status, quick status-change control, Notes indicator), "Add Appointment" button, empty state ("No appointments for this day" + Add Appointment shortcut), loading/error states matching the established Module 2 pattern.
- **Schedule/Edit Appointment form** (`features/appointments/form`), reused for both create and (Increment 3) reschedule-edit, mirroring the `patient-form.component`'s single-component-two-modes pattern (§9a.6 of the Module 2 plan): patient picker (autocomplete/search box reusing `patient.service.ts`, disabled/pre-filled when entering from Patient Detail context), date picker, time picker, optional notes textarea, Save/Cancel actions, inline overlap-warning banner shown after a save response includes `hasOverlapWarning: true`.
- **Patient Detail screen update** (`features/patients/detail`, existing component from Module 2): the currently-disabled "Appointments" placeholder link (Module 2 plan §9a.7) becomes a real, active link/tab routing to a patient-scoped appointment view — either a filtered instance of the Daily List component (query param `patientId`) or a compact inline list embedded on the Patient Detail page itself; developer's call on exact placement, but it must show that patient's upcoming/past appointments, not just a "coming soon" note.
- **Status change control** on the Daily List row: a compact dropdown (four options) or a set of icon buttons (Complete / Cancel / No-show, with "Scheduled" as the non-actionable default state) — one or two clicks per R9; exact widget choice is a developer/UX call, not BRD-mandated, but must satisfy the click-count constraint.

## 8. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — JWT/`auth.guard`/`auth.interceptor` already protect the pattern this module reuses. Patient Management (Module 2, built) — every appointment requires an existing `Patient`; the patient picker in the Schedule form calls `patient.service.ts`'s existing search (`list({query})`), and `IPatientRepository.GetByIdAsync` is reused server-side for the existence check.
- **Downstream**: Consultation & Clinical Records (Module 4) — a consultation is typically initiated from a `Scheduled` (or checked-in) appointment; Module 4 should not begin its own appointment-referencing work until this module's `Appointments` table/migration is merged, since it's expected to take a FK dependency on `Appointments.Id` (or at minimum reference `PatientId` + visit context). Search & Navigation (Module 7) may also surface "today's appointments" as part of quick navigation, once built.

## 9. Implementation Tasks

**Increment 1 — Schedule Appointment + Daily List + schema**
1. Add `Appointment` entity to `PatientManagement.Domain\Entities` (fields per §5; `Status` as a `string` picklist, matching `Patient.Gender`'s precedent — confirm no separate `AppointmentStatuses.cs` constants class is skipped; add one, mirroring `PatientGenders.cs`).
2. Add `AppointmentConfiguration` (EF Core Fluent API) to `PatientManagement.Infrastructure\Persistence\Configurations`, including the FK to `Patients` and both indexes (§5); register in `PatientManagementDbContext`.
3. Generate and apply EF Core Code-First migration (`AddAppointmentsTable`), following the `AddPatientsTable` migration's pattern.
4. Add `IAppointmentRepository` (Application layer) + `AppointmentRepository` implementation (Infrastructure), with `AddAsync`, `GetByIdAsync`, `GetByDateAsync(DateOnly date)`, `GetByPatientIdAsync(Guid patientId)`, `GetOverlappingAsync(DateOnly date, TimeOnly time, int slotMinutes, Guid? excludeAppointmentId)` (the last one powers §3.4/§4's overlap check, excluding the appointment being edited when this is reused for `PUT`).
5. Add `AppointmentOptions` (bound from `appsettings.json`, `SlotMinutes` default `30` — see §4 assumption), registered in `DependencyInjection.cs` alongside the existing options pattern (confirm how `AuthOptions` is registered in `Program.cs`/`DependencyInjection.cs` and mirror it).
6. Add `AppointmentStatuses.cs` constants (`Scheduled`, `Completed`, `Cancelled`, `NoShow`), `AppointmentValidation.cs` shared validator (patient selected, date present, time present, status is one of the four values on status-update).
7. Add `CreateAppointmentCommand` + handler (validates payload, checks `IPatientRepository.GetByIdAsync` for patient existence, runs overlap check via `GetOverlappingAsync`, persists with `Status = Scheduled`, returns `AppointmentDto` with `HasOverlapWarning`/`ConflictingAppointments`), `CreateAppointmentRequestDto`/`AppointmentDto`.
8. Add `GetAppointmentsByDateQuery` + handler (returns `AppointmentDto[]` ordered by `AppointmentTime`, joined/hydrated with `Patient.FullName` for display — either via a repository-level join or a follow-up patient lookup per row; developer's call on efficient implementation, but the returned DTO must include patient name, not just `PatientId`).
9. Add `AppointmentsController` with `POST /api/appointments` and `GET /api/appointments?date=`, wired to `RequireAuthenticatedUser`.
10. xUnit unit tests: `CreateAppointmentCommand` (valid input; missing patient/date/time; unknown `PatientId`; overlap detected → `HasOverlapWarning = true` but still succeeds; no overlap → `HasOverlapWarning = false`). `GetAppointmentsByDateQuery` (returns correct day's appointments in time order; empty day returns empty array).
11. xUnit integration tests for `POST`/`GET` end-to-end (auth required 401; create succeeds and appears in the same day's `GET`; overlap scenario returns the warning flag; unauthenticated requests rejected).
12. Angular: `appointment.service.ts` (`create()`, `listByDate(date)`), `appointments.models.ts` (`Appointment`, `CreateAppointmentRequest`), `features/appointments/list` (Daily List, calling `listByDate`), `features/appointments/form` (create-mode, patient picker + date/time + notes), route registration (`appointments`, `appointments/new`) in `app.routes.ts`, "Appointments" tab added to `app-shell.component`.
13. Angular unit/component tests: form (validation, submit success shows overlap warning banner when flagged, submit error handling), list (renders rows in time order, empty state, date navigation re-fetches).

**Increment 2 — Status Update**
14. Extend `IAppointmentRepository` with `UpdateStatusAsync` (or reuse the "load, mutate tracked entity, save" pattern from `PatientRepository.UpdateAsync`, per Module 2 plan §9a.1's precedent).
15. Add `UpdateAppointmentStatusCommand` + handler: loads the appointment (`Result.NotFound` if missing), validates the submitted status is one of the four allowed values, mutates `Status` + `UpdatedAt`, persists, returns updated `AppointmentDto`.
16. Wire `PATCH /api/appointments/{id}/status` on `AppointmentsController` (200 / 400 invalid status / 404 unknown id).
17. Add `GetAppointmentByIdQuery` + handler + `GET /api/appointments/{id}` (supports future edit-form pre-population and any direct-link scenarios), mirroring `GetPatientByIdQueryHandler`'s null-return convention (no `Result<T>` wrapper — not-found is a normal outcome for a GET-by-id).
18. Unit tests: `UpdateAppointmentStatusCommand` (valid transition for each of the 4 target statuses; invalid status string rejected; unknown id → `NotFound`). Integration tests: `PATCH` auth-required (401), 200 on valid status change with a subsequent `GET` reflecting it, 400 on invalid status value, 404 on unknown id.
19. Angular: add `updateStatus(id, status)` to `appointment.service.ts`; add a status-change control to the Daily List row (dropdown or icon buttons per §7), calling `updateStatus` and updating the row in place (no full list re-fetch, per R9's fast-interaction goal) — optimistic or refetch-on-success, developer's call, but must resolve within the same interaction (no navigation away).
20. Angular component tests: status control renders current status, invoking a status change calls the service with the correct id/value, row updates in place on success, error state surfaces inline without losing the row.

**Increment 3 — Reschedule/Edit + Patient-Scoped Cross-Navigation**
21. Add `UpdateAppointmentCommand` (full edit: date/time/notes) + handler, reusing `AppointmentValidation` and re-running the overlap check (excluding the appointment's own id from the overlap query via `excludeAppointmentId`), wire `PUT /api/appointments/{id}`.
22. Add `GetAppointmentsByPatientIdQuery` + handler + `GET /api/appointments?patientId=`, ordered by date/time (most recent or soonest first — developer's call, but consistent within the screen).
23. Unit/integration tests for edit (valid edit persists, overlap re-check works and excludes self, 404 on unknown id, 400 on invalid payload) and for patient-scoped list (returns only that patient's appointments, empty array if none, 401 without token).
24. Angular: extend `appointments/form` component to support edit-mode (route param, pre-population via `getById`, submit calls `update` instead of `create`) mirroring the Module 2 `patient-form` create/edit dual-mode pattern; wire the Patient Detail screen's "Appointments" placeholder (previously disabled per Module 2 plan §9a.7) to the new patient-scoped list/view.
25. Angular component tests: edit-mode form pre-population and submit-to-update; Patient Detail's Appointments tab renders that patient's appointments and is no longer the disabled placeholder.

**Cross-cutting**
26. Confirm the `SlotMinutes` overlap-window assumption (§2/§4, default 30) with Product Owner before Increment 1 sign-off — this directly affects the overlap-detection test fixtures and is the single largest open assumption in this plan.
27. Confirm status-change UI widget (dropdown vs. icon buttons) satisfies the "one- or two-click" NFR (R9) during Increment 2's UX review.

## 10. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Domain/
    Entities/
      Appointment.cs
  PatientManagement.Application/
    Appointments/
      Dtos/
        AppointmentDto.cs                 # includes HasOverlapWarning, ConflictingAppointments (create/edit responses)
        CreateAppointmentRequestDto.cs
        UpdateAppointmentRequestDto.cs    # Increment 3
        UpdateAppointmentStatusRequestDto.cs
      Commands/
        CreateAppointmentCommand.cs
        UpdateAppointmentStatusCommand.cs
        UpdateAppointmentCommand.cs       # Increment 3
      Queries/
        GetAppointmentsByDateQuery.cs
        GetAppointmentByIdQuery.cs
        GetAppointmentsByPatientIdQuery.cs # Increment 3
      Services/
        IAppointmentRepository.cs
      AppointmentValidation.cs
      AppointmentStatuses.cs
      AppointmentOptions.cs               # SlotMinutes config
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        AppointmentConfiguration.cs
    Repositories/
      AppointmentRepository.cs
    Migrations/
      <timestamp>_AddAppointmentsTable.cs
  PatientManagement.Api/
    Controllers/
      AppointmentsController.cs
  PatientManagement.Tests/
    Unit/Appointments/
      CreateAppointmentCommandTests.cs
      GetAppointmentsByDateQueryHandlerTests.cs
      UpdateAppointmentStatusCommandTests.cs   # Increment 2
      UpdateAppointmentCommandTests.cs         # Increment 3
      GetAppointmentsByPatientIdQueryHandlerTests.cs # Increment 3
    Integration/Appointments/
      AppointmentsEndpointsTests.cs

src/client/src/app/
  core/appointments/
    appointment.service.ts
    appointments.models.ts
  core/shell/
    app-shell.component.ts / .html        # extended: + Appointments nav tab
  features/appointments/
    list/
      appointments-list.component.ts / .html / .scss
      appointments-list.component.spec.ts
    form/
      appointment-form.component.ts / .html / .scss   # create-mode Increment 1, edit-mode Increment 3
      appointment-form.component.spec.ts
  features/patients/
    detail/
      patient-detail.component.ts / .html   # extended, Increment 3: Appointments tab wired to real data
```

## 11. Security Considerations

- Every `AppointmentsController` endpoint relies on the existing JWT bearer requirement (`RequireAuthenticatedUser` fallback policy) — no `[AllowAnonymous]` added (BRD Security NFR).
- Server-side validation on every write (Create/Update/Status-update) regardless of client-side checks, consistent with Module 2's approach — particularly important here since `PatientId` is a foreign key the server must verify actually exists, not just trust a client-supplied GUID.
- No PII beyond what's already captured on `Patient` is duplicated onto `Appointment` — the entity stores only `PatientId` (FK) plus scheduling metadata (date/time/status/notes), not a denormalized copy of patient demographics, to avoid a second place where PII could drift out of sync with Module 2's `Patients` table.
- Data in transit protected via the app's existing HTTPS enforcement; data at rest encryption remains a Module 9 (Backup & Reliability) concern.
- `GetOverlappingAsync`/date-filtered queries use parameterized EF Core LINQ (`Where`/date comparisons), never raw SQL string concatenation — same injection-avoidance posture as Module 2's search.
- No cross-patient data leakage: the patient-scoped `GET /api/appointments?patientId=` endpoint returns only that patient's rows — since this is a single-doctor, single-clinic app, there is no additional per-user row-level authorization needed beyond "authenticated at all" (matches Module 2's existing posture, no new authorization complexity introduced).

## 12. Test Strategy

**Unit tests (xUnit, Application layer)**
- `CreateAppointmentCommand`: succeeds with valid patient/date/time; fails when `PatientId` missing or refers to a non-existent patient; fails when date/time missing; overlap present → `HasOverlapWarning = true` and save still succeeds; no overlap → `HasOverlapWarning = false`; cancelled/no-show existing appointments are excluded from the overlap check (§3.4 step 4).
- `GetAppointmentsByDateQuery`: returns only the requested date's appointments, ordered by time ascending; empty day returns an empty array; appointments hydrate with patient name, not just `PatientId`.
- `UpdateAppointmentStatusCommand`: succeeds for each of the four target statuses; fails for an invalid/unsupported status string; fails with `NotFound` for a non-existent appointment id.
- `UpdateAppointmentCommand` (Increment 3): succeeds with valid date/time/notes changes; re-runs overlap check excluding itself (editing an appointment to the same time it already occupied should not warn against itself); fails validation same as create; `NotFound` for unknown id.
- `GetAppointmentsByPatientIdQuery` (Increment 3): returns only that patient's appointments; empty array for a patient with none.

**Integration tests (xUnit + `WebApplicationFactory`, mirroring `PatientsEndpointsTests`)**
- `POST /api/appointments` without a Bearer token → `401`.
- `POST /api/appointments` with valid token + valid payload → `201`, and the appointment is retrievable via `GET /api/appointments?date=` for that date immediately after.
- `POST /api/appointments` with an unknown `PatientId` → `400`.
- `POST /api/appointments` that overlaps an existing scheduled appointment on the same day → `201` (still succeeds) with `HasOverlapWarning: true` in the response body.
- `GET /api/appointments?date=` returns appointments in time order for that day and none from other days.
- `PATCH /api/appointments/{id}/status` with a valid status → `200`, subsequent `GET` reflects the new status.
- `PATCH /api/appointments/{id}/status` with an invalid status string → `400`.
- `PATCH /api/appointments/{id}/status` for an unknown id → `404`.
- `PUT /api/appointments/{id}` (Increment 3) persists date/time/notes changes and re-triggers overlap detection correctly.
- `GET /api/appointments?patientId=` (Increment 3) returns only that patient's appointments.

**E2E / component-level (Angular)**
- Doctor logs in, clicks "Appointments" in the header, sees today's list (possibly empty), clicks "Add Appointment," picks an existing patient, sets a date/time, saves, and sees the new appointment appear in the list.
- Doctor schedules a second appointment overlapping an existing one and sees the overlap warning, while the appointment still saves.
- Doctor changes an appointment's status from the Daily List with one or two clicks and sees the row update immediately without navigating away.
- Doctor navigates to a Patient's Detail page and opens the (now-active) Appointments tab, seeing that patient's appointment history/upcoming visits (Increment 3).

**Performance**
- Daily list (`GET /api/appointments?date=`) response time stays within the < 2 second target (R8/Modules\03 §11) under a representative single day's appointment volume (a clinic's realistic daily cap, e.g., 20–40 visits) — validate once the date index is in place.

## 13. Acceptance Criteria

- AC1: Doctor can create an appointment against an existing patient with a date and time, and it is immediately retrievable via the daily list for that date. (Modules\03 §10)
- AC2: The daily list correctly filters to show only that day's appointments, in time order. (Modules\03 §10)
- AC3: Changing an appointment's status updates the record and reflects immediately in the daily list, in one or two clicks. (Modules\03 §10; §11 R9)
- AC4: Creating an appointment that overlaps an existing time slot shows a warning but still allows the save to succeed — never a hard block. (Modules\03 §10)
- AC5: Every appointment is linked to a valid, existing patient — creation against an unknown `PatientId` is rejected. (Modules\03 §5)
- AC6: Status is restricted to exactly the four defined values (Scheduled/Completed/Cancelled/No-show) — no other value is accepted. (Modules\03 §5)
- AC7: All Appointment Management endpoints reject unauthenticated requests with `401`. (BRD Security NFR)
- AC8: No reminder/notification is triggered anywhere by a status change. (Modules\03 §5; BRD Out of Scope)
- AC9: The daily appointment list loads within the BRD's < 2 second target under representative volume. (BRD Performance NFR; Modules\03 §11)
- AC10 (Increment 3): The Patient Detail screen's "Appointments" tab, previously a disabled placeholder, shows that patient's real appointment data.

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| No appointment duration/end-time is specified in the BRD, forcing an assumed slot length for overlap detection | Overlap warnings could fire incorrectly (too sensitive or not sensitive enough) if the assumed 30-minute slot doesn't match real clinic behavior | Flagged explicitly (§2, §4, task 26) as an open question; make `SlotMinutes` configurable, not hardcoded; confirm with Product Owner before Increment 1 sign-off |
| Overlap detection excludes Cancelled/No-show appointments from the conflict check — an interpretation, not an explicit BRD rule | Could under- or over-warn depending on what the doctor actually expects "existing appointment" to mean | Documented as an explicit interpretation (§3.4 step 4); easy to reverse if Product Owner disagrees, since it's isolated to one repository method's `WHERE` clause |
| Status transition state machine is intentionally unenforced (any status → any status) | A doctor could accidentally revert a `Completed` appointment back to `Scheduled` with no guard rail | Documented as a deliberate scope-limiting decision (§4) since Modules\03 doesn't specify transition rules; flag to Product Owner as a possible future refinement, not built preemptively |
| Full edit/reschedule (`PUT`, date/time change) is inferred from the overlap-detection rule rather than explicitly listed as its own functionality in Modules\03 §4 | Could be seen as scope creep if Product Owner intended appointments to be cancel-and-recreate only, not edited in place | Flagged in §4; sequenced as Increment 3 (lower priority) rather than Increment 1, so it can be dropped without disrupting the core Schedule/List/Status slice if Product Owner says reschedule isn't needed |
| Patient-scoped appointment cross-navigation on Patient Detail depends on Module 2's existing disabled placeholder link, which this plan repurposes | Risk of drift if Module 2's actual current markup/structure differs from what's assumed here | Task 24 explicitly calls for reading the live `patient-detail.component` markup before wiring the real link, not assuming the Module 2 plan's description is still accurate |
| Daily list performance unverified until real data volumes exist | Could miss the < 2s NFR if a clinic's daily appointment count is higher than assumed | Add the `AppointmentDate` index proactively (§5); include a performance test in Increment 1 (§12); revisit if volume assumptions change |
| `PatientId` FK with no cascade delete, paired with Module 2 having no delete endpoint today | Currently low risk (nothing can delete a referenced patient), but if Module 2 ever gains a delete capability, orphaned/blocked-delete behavior needs revisiting | Documented as defensive-only for now (§5); flag to whichever future plan (if any) reopens Patient deletion that this FK constraint exists and must be accounted for |

---

## Open Questions — Requiring Product Owner Confirmation

1. **Assumed appointment slot/duration length for overlap detection** (BRD/Modules\03 specify neither an end-time field nor a duration): this plan proposes a configurable default of **30 minutes**. Confirm this value (or that a different default, or an explicit end-time field instead, is preferred) before Increment 1 closes.
2. **Full reschedule/edit (`PUT`, changing date/time on an existing appointment)**: Modules\03 §4 explicitly lists Schedule, Daily List, Status Update, and Overlap Detection as the four functionalities — it does not explicitly list "edit an existing appointment's date/time." This plan infers reschedule is needed (since overlap detection logically should also apply to edits) and sequences it as Increment 3. Confirm whether this is in scope for Phase 1, or whether "cancel and re-schedule" (create a new appointment, mark the old one Cancelled) is the intended flow instead — the latter would let this plan drop the `PUT` endpoint and Increment 3 task 21 entirely.
3. **Status-change UI widget** (dropdown vs. icon buttons) for the Daily List row — no BRD preference stated; developer/UX call within the "one- or two-click" constraint (R9). Not blocking, but flagged for awareness.
4. **Overlap definition detail**: should overlap detection compare against *only* `Scheduled` appointments (as this plan proposes, excluding `Cancelled`/`NoShow`), or against *all* appointments on the date regardless of status? Confirm the interpretation in §3.4 step 4 matches intent.

---

## Dependencies Recap (for sequencing awareness)

This module sits third in the fixed build order (Authentication → Patient Management → **Appointment Management** → Consultation & Clinical Records → Prescription & Medication Management → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Module 4 (Consultation & Clinical Records) should not begin its own appointment-referencing schema work until this module's `Appointments` table/migration (Increment 1, tasks 1–3) is merged, since a consultation is expected to be initiated from an existing appointment.
