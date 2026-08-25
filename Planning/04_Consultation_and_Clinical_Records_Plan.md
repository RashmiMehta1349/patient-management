# Module 4: Consultation & Clinical Records (EMR Core) — Implementation Plan

## 1. Module Overview

Consultation & Clinical Records is the clinical heart of the application — the screen where the doctor actually documents what happened during a visit: vitals, the patient's complaints, and the diagnosis. It sits directly after Patient Management (Module 2, built) and Appointment Management (Module 3, built) in the dependency chain: a consultation is recorded against an existing `Patient` and, typically but not mandatorily, against an existing `Appointment`. The resulting `Visit` record is the parent entity the rest of the clinical workflow hangs off — Prescription (Module 5, not yet built) attaches medication line items to it, and Patient History (Module 6, not yet built) lists and displays it chronologically. Unlike Modules 2/3, this module introduces the BRD's one deliberately "soft" validation model in the whole app: vitals are conceptually mandatory but never a hard save-blocker, because the doctor must always be able to finish and move to the next patient. The BRD's stated success criterion for this module — complete a consultation record in 2–3 minutes — drives every UI decision here: minimal clicks, keyboard-first fields, no forced structured data entry beyond what's explicitly required.

This plan covers full Module 4 scope — Vitals Capture, Complaints Entry, Diagnosis Entry, Save Visit Record — per `Modules\04_Consultation_and_Clinical_Records.md`, sequenced into increments that mirror how Modules 2 and 3 were built (schema + core create/read first, then cross-navigation wiring, then lower-priority edit), so a developer can pick up Increment 1 immediately after this plan is approved. It also follows the same "wire the disabled placeholder" pattern Module 3 used for the Patient Detail screen's "Appointments" tab: the currently-disabled **"Consultations (coming soon)"** placeholder on `patient-detail.component.html` (see `src\client\src\app\features\patients\detail\patient-detail.component.html` line 58) becomes a real, active section listing that patient's saved visits.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Record Temperature, Blood Pressure, and Pulse for a consultation, with each field explicitly addressable as either a value or "not recorded" | BRD `Functional Requirements → Consultation Workflow → Vitals Capture (Mandatory)`; Modules\04 §4 #1, §5 |
| R2 | "Not recorded" on any vital never blocks save — vitals are conceptually mandatory but not a hard validation gate | BRD: "the consultation can still be saved (not a hard block)"; Modules\04 §5 |
| R3 | Free-text complaints entry, no required minimum length | BRD `Functional Requirements → Complaints`; Modules\04 §4 #2, §5 |
| R4 | Free-text diagnosis entry, no required minimum length, no structured/coded diagnosis | BRD `Functional Requirements → Diagnosis`; Modules\04 §4 #3, §5 |
| R5 | Save vitals + complaints + diagnosis as a single visit record tied to the patient (and appointment, if applicable) | Modules\04 §4 #4, §7 |
| R6 | A visit cannot exist without a linked patient; the appointment link is optional | Modules\04 §5, §7 |
| R7 | A saved consultation is immediately part of the patient's permanent visit history and available to Prescription | Modules\04 §5, §10 |
| R8 | Full flow (open patient → enter vitals/complaints/diagnosis → save) realistically completable in 2–3 minutes, keyboard-first, minimal-click | BRD Success Criteria; Modules\04 §11 |
| R9 | Must be logged in (JWT-authenticated) to access consultation data | BRD Security NFR; module Dependencies (Auth) |
| R10 | Consultation data encrypted at rest and in transit, included in daily backup | Modules\04 §11; BRD Security/Reliability NFRs (Module 9 owns the backup mechanics, this module owns not undermining them) |

**Explicitly out of scope for this module** (do not build): structured/coded diagnosis (e.g., ICD-10 lookup), AI-assisted diagnosis or recommendations, lab result integration, templates/macros for common complaints (Modules\04 §3 Out of Scope; BRD Out of Scope list). If asked for any of these, flag the conflict rather than building it.

**Explicit assumptions flagged for Product Owner** (BRD/Modules\04 is silent on these — see §14 Open Questions for full detail):
- No unit is specified for Temperature (°C vs °F) or a format for Blood Pressure (e.g., "120/80") — this plan treats Temperature as a free-form decimal (no unit conversion/validation) and Blood Pressure as a free-form short string, not two separate numeric fields, since the BRD calls for capture, not structured/coded clinical validation (which is explicitly out of scope).
- No BRD statement on whether a saved consultation can be edited afterward, or whether saving a visit against an appointment should auto-transition that appointment's status to `Completed`. Both are treated as reasonable, low-risk extensions and sequenced as lower-priority increments (§9) pending sign-off, not silently built into Increment 1.
- No explicit list/detail screen is named in Modules\04 §4 (its four functionalities are Vitals/Complaints/Diagnosis/Save only) — this plan still includes a minimal patient-scoped visit list (read-only) because it is the only way to fulfill R7 ("immediately part of the patient's...history") visibly in the UI before Module 6 exists, and to retire the disabled "Consultations (coming soon)" placeholder per this task's explicit instruction. This is a thin precursor to Module 6's full History view, not a substitute for it — Module 6 remains the authoritative, richer implementation.

## 3. Workflows

### 3.1 Start / Record a Consultation
1. Doctor reaches the "New Consultation" action from one of two entry points, mirroring Module 3's dual-entry pattern:
   - From the **Appointment Daily List** (`features/appointments/list`), a "Start Consultation" action on a `Scheduled` appointment row pre-fills both `PatientId` and `AppointmentId` from that row.
   - From the **Patient Detail** screen's (now-active) "Consultations" section, a "New Consultation" action pre-fills `PatientId` only (walk-in / no linked appointment — R6 allows this).
2. The Consultation form loads with the patient's name shown read-only for context (no patient picker here — patient is always pre-selected via route/context, unlike Module 3's Schedule form which needed a picker for cold-start entry).
3. Doctor addresses each vital: enters a value, or toggles "Not recorded" (see §4 for the exact value/flag interaction). Enters free-text Complaints and Diagnosis (either or both may be left blank — R3/R4 have no minimum length).
4. Client performs light inline validation only (patient/appointment context present — always true since it's route-derived; no vital, complaint, or diagnosis field is a hard client-side requirement, per R2).
5. On submit, client calls `POST /api/visits`. Server validates the payload (patient exists; each vital is "addressed" — either a value or `NotRecorded = true`, never silently defaulted) and persists.
6. API returns the created `VisitDto`. Client shows an inline success confirmation and navigates back to the Patient Detail screen (or, if entered from the Appointment Daily List, back to that list) — no forced multi-step wizard, consistent with R8's 2–3 minute target.

### 3.2 View a Patient's Consultations (minimal, precursor to Module 6)
1. Doctor opens Patient Detail; the "Consultations" section (replacing the disabled placeholder) calls `GET /api/visits?patientId={id}`.
2. API returns that patient's visits, most-recent-first, each row showing date, a one-line vitals summary, and a truncated diagnosis preview.
3. Empty state ("No consultations recorded for this patient yet") with a "New Consultation" shortcut.
4. This list is intentionally minimal (no date filtering, no full visit detail expansion) — Module 6 (Patient History) owns the richer, filterable, full-detail chronological view; this section exists only to close the loop on R7 and retire the placeholder.

### 3.3 Vitals "Not Recorded" Handling (embedded in 3.1)
1. Each of the three vitals (Temperature, BP, Pulse) is rendered as a value input paired with a "Not recorded" toggle/checkbox.
2. If the doctor enters a value, the "Not recorded" toggle is implicitly off (value takes precedence; the client keeps them mutually exclusive in the UI so a value can't coexist with `NotRecorded = true`).
3. If the doctor toggles "Not recorded" on, the value input is cleared/disabled — the server persists `NotRecorded = true`, value column `null`.
4. If a doctor leaves a vital field completely untouched (neither a value entered nor the toggle explicitly touched), the client defaults it to `NotRecorded = true` on submit — this satisfies R1's "explicitly addressed" business rule without forcing an extra click per field for the common case where a vital genuinely wasn't measured. Flagged as an interpretation choice, not an explicit BRD instruction (see §14 Open Question 3).

## 4. Architecture Approach

- **Layering**: same Clean Architecture split established by Modules 2 and 3 — `PatientManagement.Domain\Entities\Visit.cs`, `PatientManagement.Application\Visits\` (DTOs, Commands, Queries, `IVisitRepository`, validation), `PatientManagement.Infrastructure\Persistence\Configurations\VisitConfiguration.cs` + repository + migration, `PatientManagement.Api\Controllers\VisitsController.cs`, tests under `PatientManagement.Tests\Unit\Visits` / `Integration\Visits`. Same single-solution/assembly structure as Patients and Appointments — no new project split introduced.
- **CQRS-lite via Commands/Queries**: `CreateVisitCommand`, `GetVisitByIdQuery`, `GetVisitsByPatientIdQuery`. `UpdateVisitCommand` (edit an already-saved consultation) is a distinct, lower-priority Increment 3 item (see §9), since Modules\04 §4's four functionalities are Capture/Entry/Entry/Save only — editing after the fact is not explicitly named, matching how Module 3 treated reschedule as an inferred, sequenced-later capability rather than baseline scope.
- **`Result<T>` reuse**: extend the existing `PatientManagement.Application.Common.Result<T>` for `CreateVisitCommand`/`UpdateVisitCommand` outcomes — no new result type. Not-found means either "patient not found" (bad `PatientId`) or "appointment not found" (bad, non-null `AppointmentId`), both mapped to `Result<T>.NotFound(...)`; a missing/invalid `PatientId` on create is treated the same way Module 3 treats it (a `400`-worthy data-integrity guard, not a normal user-facing 404, since the client always derives `PatientId` from route context, never free typing).
- **Vitals modeling — value + explicit flag, not nullable-only**: each vital is stored as a nullable value column *plus* a required boolean `NotRecorded` flag (`TemperatureNotRecorded`, `BloodPressureNotRecorded`, `PulseNotRecorded`), rather than inferring "not recorded" from a null value column alone. Rationale: R1 requires the field be "explicitly addressed" — a bare null is ambiguous (doctor forgot vs. doctor deliberately skipped it), whereas a boolean flag makes the "explicitly not recorded" intent durable and queryable, and keeps future Module 6 history views from having to guess. This is the module's one genuinely novel data-modeling decision relative to Modules 2/3's straightforward required/optional fields.
- **Vitals validation placement**: enforced in `VisitValidation.cs` (Application layer, mirroring `PatientValidation.cs`/`AppointmentValidation.cs` precedent) with one rule per vital: exactly one of {value present, `NotRecorded = true`} must hold — a payload with both a value and `NotRecorded = true` is normalized server-side (`NotRecorded` wins, value discarded) rather than rejected, keeping this the one field-set in the whole app where the server is deliberately permissive rather than strict, consistent with R2's "not a hard block" intent. This normalization (not a 400) is a deliberate choice to avoid ever blocking a save over a vitals-shape ambiguity — flagged in §14 Open Question 2 for confirmation.
- **Blood Pressure representation**: stored as a short free-form string (e.g., `"120/80"`), not two separate systolic/diastolic integer columns — Modules\04 explicitly excludes structured/coded clinical data validation, and a single free-text-ish field keeps entry to one keystroke sequence (2–3 minute target) without inventing a validation format the BRD never specified. Flagged as an assumption (§14 Open Question 1).
- **Temperature/Pulse representation**: Temperature stored as `decimal(4,1)` (no unit conversion or enforcement — BRD doesn't specify °C/°F), Pulse stored as `int` (beats per minute, no BRD-specified range validation beyond basic sanity, e.g., reject negative values, since a hard clinical range check would be inventing structured validation the BRD doesn't call for).
- **Appointment linkage**: `AppointmentId` is a nullable FK. When present, `CreateVisitCommandHandler` verifies the appointment exists (`IAppointmentRepository.GetByIdAsync`, already built) but does **not** enforce that the appointment's `PatientId` matches the visit's `PatientId` beyond a consistency check returning a validation failure if they mismatch (defensive data-integrity guard, not a new business rule) — and does **not** auto-transition the appointment's `Status` to `Completed` in Increment 1 (flagged as a deferred, lower-priority Increment 3 enhancement, §14 Open Question 4, since Modules\04/BRD never states this coupling).
- **Validation placement (general)**: client-side for responsiveness (patient/appointment context present); server-side in the Application layer as the actual enforcement point, matching Modules 2/3's precedent — `VisitValidation.cs` static helper reused by create and (Increment 3) edit handlers.
- **Rendering**: Angular standalone components under `features/consultations/*` (mirroring `features/appointments/*`): `form` (record consultation, single-mode create in Increment 1, edit-mode added Increment 3), reusing route-derived patient/appointment context rather than a picker (unlike the Appointment form, which needs one for cold-start scheduling).
- **Auth**: all endpoints behind the existing `RequireAuthenticatedUser` fallback policy — no new anonymous surface, consistent with Modules 2/3.
- **Navigation integration**: retires the disabled "Consultations (coming soon)" placeholder span on `patient-detail.component.html` (line 58), replacing it with an active section following the same pattern as the "Appointments" section immediately above it in that same file (loading/error/empty states, list of rows, a "+ New Consultation" action) — not a new top-level nav tab, since a consultation is always entered in the context of a specific patient (there is no cross-patient "all consultations today" screen analogous to the Appointment Daily List; Modules\04 defines no such functionality).
- **"Start Consultation" entry point on the Appointment Daily List**: adds a row-level action to `features/appointments/list` (alongside the existing status-change control) that routes to `consultations/new?patientId={id}&appointmentId={id}` — a small, additive change to an already-built Module 3 component, not a rebuild of it.

## 5. Database Entities

### `Visits` table

| Field | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` (GUID), PK | Matches `Patient`/`Appointment`/`User` PK convention |
| `PatientId` | `uniqueidentifier`, required, FK → `Patients.Id` | R6 — every visit must reference a patient; no cascade delete, matching Module 3's `Appointments.PatientId` precedent (no delete endpoint exists on `Patients`) |
| `AppointmentId` | `uniqueidentifier`, nullable, FK → `Appointments.Id` | R6 — optional link; `ON DELETE NO ACTION`/restrict, same defensive rationale as the Patient FK |
| `VisitDate` | `datetime2`, required | Defaults to server time-of-save (doctor doesn't manually pick a date/time for "now" consultations — no BRD statement requiring a manual date picker here, unlike Appointment scheduling which is inherently future-dated) |
| `TemperatureValue` | `decimal(4,1)`, nullable | Null when `TemperatureNotRecorded = true` |
| `TemperatureNotRecorded` | `bit`, required | R1/R2 — explicit "not recorded" flag |
| `BloodPressureValue` | `nvarchar(20)`, nullable | Free-form string (e.g., `"120/80"`); null when `BloodPressureNotRecorded = true` |
| `BloodPressureNotRecorded` | `bit`, required | R1/R2 |
| `PulseValue` | `int`, nullable | Beats per minute; null when `PulseNotRecorded = true` |
| `PulseNotRecorded` | `bit`, required | R1/R2 |
| `Complaints` | `nvarchar(2000)`, nullable | R3 — free text, no minimum length |
| `Diagnosis` | `nvarchar(2000)`, nullable | R4 — free text, no minimum length, no structured coding |
| `CreatedAt` | `datetime2`, required | Audit/history ordering |
| `UpdatedAt` | `datetime2`, required | Bumped on any Increment 3 edit |

**Indexes**: non-clustered index on `PatientId` (patient-scoped visit list — the module's one read query, and the future Module 6/Module 5 join path); non-clustered index on `AppointmentId` (supports the "did this appointment produce a visit" lookup, useful for the deferred auto-`Completed` enhancement). **FK relationships**: `Visits.PatientId → Patients.Id`, required, `ON DELETE NO ACTION`; `Visits.AppointmentId → Appointments.Id`, nullable, `ON DELETE NO ACTION`.

## 6. APIs

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `POST` | `/api/visits` | Record a new consultation (vitals + complaints + diagnosis) against a patient, optionally an appointment | Bearer JWT required | `201` + `VisitDto` | `400` invalid payload / unknown `PatientId` or `AppointmentId` / patient-appointment mismatch |
| `GET` | `/api/visits/{id}` | Retrieve a single visit (supports future edit-form pre-population and Module 5's "generate prescription from this visit" flow) | Bearer JWT required | `200` + `VisitDto` | `404` unknown id |
| `GET` | `/api/visits?patientId={id}` | Patient-scoped visit list, most-recent-first (Patient Detail "Consultations" section) | Bearer JWT required | `200` + `VisitDto[]` | — (empty array if none) |
| `PUT` | `/api/visits/{id}` | Full edit (vitals/complaints/diagnosis) — Increment 3, deferred pending sign-off | Bearer JWT required | `200` + updated `VisitDto` | `400` invalid payload / `404` unknown id |

All routes sit behind the existing fallback `RequireAuthenticatedUser` policy. `404`/`400` bodies follow the established `{ message: "..." }` convention from `PatientsController`/`AppointmentsController`. No `DELETE` endpoint — matching Modules 2/3's precedent of no delete capability anywhere in the app to date, and consistent with clinical records being a permanent history (R7).

## 7. UI / Screens

- **Patient Detail screen update** (`features/patients/detail`, existing component from Modules 2/3): the currently-disabled "Consultations (coming soon)" placeholder span (`patient-detail.component.html` line 58) becomes an active "Consultations" section, structurally parallel to the existing "Appointments" section directly above it — loading/error/empty states, a list of that patient's visits (date, one-line vitals summary, diagnosis preview), and a "+ New Consultation" action routing to `consultations/new?patientId={id}`.
- **Record Consultation form** (`features/consultations/form`), Increment 1 create-only, Increment 3 adds edit-mode (mirroring the `patient-form`/`appointment-form` single-component-dual-mode pattern already established): read-only patient name header (context, not editable — no patient picker, unlike the Appointment form), three vitals rows (Temperature / Blood Pressure / Pulse), each with a value input + "Not recorded" toggle rendered as mutually exclusive, Complaints textarea, Diagnosis textarea, Save/Cancel actions. Keyboard-first: logical tab order through value→toggle→next field, no required-field blocking dialogs on any vital (R2/R8).
- **Appointment Daily List update** (`features/appointments/list`, existing component from Module 3): adds a "Start Consultation" row action (alongside the existing status-change control) on rows with `Status = Scheduled`, routing to `consultations/new?patientId={id}&appointmentId={id}`.
- No new App Shell nav tab — consultations are only ever reached in patient context (from Patient Detail or from an appointment row), consistent with Modules\04 defining no cross-patient "all consultations" screen.

## 8. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — JWT/`auth.guard`/`auth.interceptor` protect the pattern this module reuses. Patient Management (Module 2, built) — every visit requires an existing `Patient`; `IPatientRepository.GetByIdAsync` reused server-side for the existence check, same pattern as Module 3. Appointment Management (Module 3, built) — the optional `AppointmentId` FK requires `Appointments` table/migration already merged (it is), and `IAppointmentRepository.GetByIdAsync` is reused for the optional existence/consistency check.
- **Downstream**: Prescription & Medication Management (Module 5) — a prescription is generated from a visit's data; Module 5 should not begin its own visit-referencing schema work until this module's `Visits` table/migration (Increment 1) is merged, since it takes a FK dependency on `Visits.Id`. Patient History (Module 6) — visits are the core history entries; Module 6's richer chronological/filterable view supersedes this module's minimal patient-scoped list (§3.2) once built, but does not require changes to the `Visits` schema this plan establishes.

## 9. Implementation Tasks

**Increment 1 — Record Consultation (create) + schema + minimal read**
1. Add `Visit` entity to `PatientManagement.Domain\Entities` (fields per §5).
2. Add `VisitConfiguration` (EF Core Fluent API) to `PatientManagement.Infrastructure\Persistence\Configurations`, including both FKs (`PatientId` required, `AppointmentId` nullable) and both indexes (§5); register in `PatientManagementDbContext`.
3. Generate and apply EF Core Code-First migration (`AddVisitsTable`), following the `AddAppointmentsTable` migration's pattern.
4. Add `IVisitRepository` (Application layer) + `VisitRepository` implementation (Infrastructure), with `AddAsync`, `GetByIdAsync`, `GetByPatientIdAsync(Guid patientId)` (most-recent-first).
5. Add `VisitValidation.cs` shared validator: patient context present; for each of the three vitals, normalize the value/`NotRecorded` pair per §4's rule (value wins over a conflicting `NotRecorded = true`, both-empty defaults to `NotRecorded = true` per §3.3 step 4); reject only genuinely malformed input (e.g., negative Pulse) — never reject on a vital simply being unaddressed in a way that maps to "not recorded."
6. Add `CreateVisitCommand` + handler: validates payload, checks `IPatientRepository.GetByIdAsync` for patient existence (`NotFound` if missing), if `AppointmentId` present checks `IAppointmentRepository.GetByIdAsync` (`NotFound` if missing; `Failure` if the appointment's `PatientId` doesn't match), persists, returns `VisitDto`.
7. Add `VisitDto`, `CreateVisitRequestDto`, `VisitMapper` (mirroring `AppointmentMapper`'s precedent).
8. Add `GetVisitByIdQuery` + handler (mirrors `GetAppointmentByIdQuery`'s null-return convention — not-found is a normal GET-by-id outcome, no `Result<T>` wrapper).
9. Add `GetVisitsByPatientIdQuery` + handler (returns `VisitDto[]`, most-recent-first).
10. Add `VisitsController` with `POST /api/visits`, `GET /api/visits/{id}`, `GET /api/visits?patientId=`, wired to `RequireAuthenticatedUser`.
11. xUnit unit tests: `CreateVisitCommand` (valid input with all vitals recorded; valid input with all vitals "not recorded"; mixed vitals; missing patient → `NotFound`; unknown `AppointmentId` → `NotFound`; mismatched appointment/patient → `Failure`; complaints/diagnosis blank succeeds; a submitted value alongside `NotRecorded = true` normalizes to not-recorded rather than failing). `GetVisitsByPatientIdQuery` (returns correct patient's visits, most-recent-first; empty patient returns empty array). `GetVisitByIdQuery` (found/not-found).
12. xUnit integration tests for `POST`/`GET` end-to-end (auth required 401; create succeeds and is retrievable via both `GET /api/visits/{id}` and `GET /api/visits?patientId=` immediately after; unknown `PatientId` → 400; unauthenticated requests rejected).
13. Angular: `visit.service.ts` (`create()`, `getById(id)`, `listByPatientId(id)`), `visits.models.ts` (`Visit`, `CreateVisitRequest`), `features/consultations/form` (create-mode: patient/appointment context from route, vitals rows with value+toggle, complaints/diagnosis textareas, Save/Cancel), route registration (`consultations/new`) in `app.routes.ts`.
14. Angular unit/component tests: form (vitals toggle mutual-exclusivity behavior, submit with all-not-recorded succeeds, submit with mixed vitals succeeds, submit error handling surfaces inline without losing entered data).

**Increment 2 — Patient Detail wiring + Appointment Daily List entry point**
15. Retire the disabled "Consultations (coming soon)" placeholder on `patient-detail.component.html` (line 58): add a "Consultations" section calling `visit.service.ts`'s `listByPatientId`, structurally parallel to the existing Appointments section (loading/error/empty states, row list, "+ New Consultation" action).
16. Angular component tests: Patient Detail's Consultations section renders that patient's visits and is no longer the disabled placeholder; empty state renders correctly for a patient with none.
17. Add a "Start Consultation" row action to `features/appointments/list` for `Scheduled`-status rows, routing to `consultations/new` with `patientId`/`appointmentId` query params pre-filled.
18. Angular component tests: clicking "Start Consultation" on a Daily List row navigates with the correct query params; the Consultation form correctly reads and pre-fills both `patientId` and `appointmentId` from route/query context.

**Increment 3 — Edit consultation + auto-`Completed` linkage (deferred, pending Product Owner sign-off — see §14)**
19. Add `UpdateVisitCommand` + handler (full edit: vitals/complaints/diagnosis), reusing `VisitValidation`; wire `PUT /api/visits/{id}`.
20. (Contingent on Open Question 4 sign-off) When a visit is saved against a non-null `AppointmentId`, transition that appointment's `Status` to `Completed` via `IAppointmentRepository`'s existing status-update path — implemented as an explicit, documented side effect inside `CreateVisitCommandHandler`, not a hidden background job.
21. Unit/integration tests for edit (valid edit persists, 404 on unknown id, 400 on invalid payload) and, if task 20 is approved, for the appointment auto-completion side effect (status flips to `Completed` on visit save; unaffected when no `AppointmentId` is present).
22. Angular: extend `features/consultations/form` to support edit-mode (route param, pre-population via `getById`, submit calls `update` instead of `create`), mirroring the Module 2/3 dual-mode form pattern.
23. Angular component tests: edit-mode form pre-population and submit-to-update.

**Cross-cutting**
24. Confirm all four §14 open assumptions (BP/Temperature representation, vitals normalization-not-rejection rule, "untouched defaults to not-recorded" interpretation, auto-`Completed` linkage) with Product Owner before Increment 1 sign-off for the first three, and before Increment 3 starts for the fourth.
25. Verify keyboard-first tab order and click-count on the Record Consultation form against R8's 2–3 minute target during Increment 1's UX review (informal timing pass, not a formal usability study — no BRD-mandated measurement method).

## 10. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Domain/
    Entities/
      Visit.cs
  PatientManagement.Application/
    Visits/
      Dtos/
        VisitDto.cs
        CreateVisitRequestDto.cs
        UpdateVisitRequestDto.cs          # Increment 3
      Commands/
        CreateVisitCommand.cs
        UpdateVisitCommand.cs             # Increment 3
      Queries/
        GetVisitByIdQuery.cs
        GetVisitsByPatientIdQuery.cs
      Services/
        IVisitRepository.cs
      VisitValidation.cs
      VisitMapper.cs
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        VisitConfiguration.cs
    Repositories/
      VisitRepository.cs
    Migrations/
      <timestamp>_AddVisitsTable.cs
  PatientManagement.Api/
    Controllers/
      VisitsController.cs
  PatientManagement.Tests/
    Unit/Visits/
      CreateVisitCommandTests.cs
      GetVisitByIdQueryHandlerTests.cs
      GetVisitsByPatientIdQueryHandlerTests.cs
      UpdateVisitCommandTests.cs          # Increment 3
    Integration/Visits/
      VisitsEndpointsTests.cs

src/client/src/app/
  core/visits/
    visit.service.ts
    visits.models.ts
  features/consultations/
    form/
      consultation-form.component.ts / .html / .scss   # create-mode Increment 1, edit-mode Increment 3
      consultation-form.component.spec.ts
  features/patients/
    detail/
      patient-detail.component.ts / .html   # extended, Increment 2: Consultations section wired to real data
  features/appointments/
    list/
      appointments-list.component.ts / .html   # extended, Increment 2: "Start Consultation" row action
```

## 11. Security Considerations

- Every `VisitsController` endpoint relies on the existing JWT bearer requirement (`RequireAuthenticatedUser` fallback policy) — no `[AllowAnonymous]` added (BRD Security NFR), consistent with Modules 2/3.
- Server-side validation on every write regardless of client-side checks — particularly the `PatientId`/`AppointmentId` FK existence and consistency checks, which the server must verify rather than trust a client-supplied GUID (same posture as Module 3's `PatientId` guard).
- `Visit` rows carry clinical PII (vitals, complaints, diagnosis) — the single most sensitive data this application stores. No denormalized copy of patient demographics is added to `Visit` beyond `PatientId`, matching Module 3's Appointment precedent of not duplicating PII across tables.
- Data in transit protected via the app's existing HTTPS enforcement; data at rest encryption and inclusion in the daily backup remain Module 9 (Backup & Reliability) concerns — this module's obligation (R10) is simply to not introduce a separate, unencrypted storage path (e.g., no local file exports, no logging of vitals/complaints/diagnosis field values in application logs).
- No cross-patient data leakage: `GET /api/visits?patientId=` returns only that patient's rows; since this is a single-doctor, single-clinic app, there is no additional per-user row-level authorization needed beyond "authenticated at all" (matches Modules 2/3's existing posture).
- All EF Core queries use parameterized LINQ, never raw SQL string concatenation — same injection-avoidance posture as Modules 2/3.

## 12. Test Strategy

**Unit tests (xUnit, Application layer)**
- `CreateVisitCommand`: succeeds with all three vitals recorded as values; succeeds with all three marked "not recorded"; succeeds with a mixed combination; fails with `NotFound` for an unknown `PatientId`; fails with `NotFound` for an unknown, non-null `AppointmentId`; fails with `Failure` when the supplied appointment's `PatientId` doesn't match the visit's `PatientId`; succeeds with blank Complaints and/or blank Diagnosis (R3/R4 — no minimum length); a payload with both a vital value present and `NotRecorded = true` for that field normalizes to not-recorded rather than being rejected (R2's "never a hard block" guarantee, exercised directly).
- `GetVisitsByPatientIdQuery`: returns only the requested patient's visits, most-recent-first; empty patient returns an empty array.
- `GetVisitByIdQuery`: returns the visit when found; returns null/not-found for an unknown id.
- `UpdateVisitCommand` (Increment 3): succeeds with valid vitals/complaints/diagnosis changes; `NotFound` for unknown id; same normalization rule as create.

**Integration tests (xUnit + `WebApplicationFactory`, mirroring `AppointmentsEndpointsTests`)**
- `POST /api/visits` without a Bearer token → `401`.
- `POST /api/visits` with valid token + all vitals "not recorded" + blank complaints/diagnosis → `201` (the minimal-valid-payload case, proving R2 in practice).
- `POST /api/visits` with valid token + full payload (all vitals, complaints, diagnosis, linked appointment) → `201`, retrievable immediately via both `GET /api/visits/{id}` and `GET /api/visits?patientId=`.
- `POST /api/visits` with an unknown `PatientId` → `400`.
- `POST /api/visits` with an unknown `AppointmentId` → `400`.
- `GET /api/visits/{id}` for an unknown id → `404`.
- `GET /api/visits?patientId=` for a patient with no visits → `200` + empty array.
- `PUT /api/visits/{id}` (Increment 3) persists vitals/complaints/diagnosis changes.

**E2E / component-level (Angular)**
- Doctor opens a patient's detail page, clicks "New Consultation," addresses all three vitals as "not recorded," leaves complaints/diagnosis blank, saves, and the consultation appears in the patient's Consultations section immediately.
- Doctor opens a patient's detail page, clicks "New Consultation," enters values for all three vitals plus complaints and diagnosis text, saves, and sees the saved values reflected.
- Doctor, from the Appointment Daily List, clicks "Start Consultation" on a scheduled appointment row and lands on a pre-filled Consultation form for the correct patient and appointment.
- Doctor times a full "open patient → complete consultation → save" pass and it completes within roughly 2–3 minutes with no forced/blocking dialogs (informal UX validation of R8, not an automated test).

**Performance**
- `GET /api/visits?patientId=` response time stays comfortably within the BRD's general < 2 second Performance NFR under a representative single patient's visit history volume (a few dozen visits) — validate once the `PatientId` index is in place; no dedicated load test beyond this, since visit volume per patient is inherently small in a single-clinic app.

## 13. Acceptance Criteria

- AC1: Doctor can enter Temperature, Blood Pressure, and Pulse, or mark any/all of them "not recorded," and successfully save — no combination of vitals input blocks the save. (Modules\04 §10)
- AC2: Doctor can enter free-text Complaints and Diagnosis, including leaving either or both blank, and have whatever was entered persist with the visit record. (Modules\04 §10)
- AC3: A saved consultation is immediately visible on that patient's Patient Detail "Consultations" section and retrievable via `GET /api/visits/{id}` for future Prescription (Module 5) use. (Modules\04 §10)
- AC4: A visit cannot be created without a valid, existing `PatientId`; creation against an unknown patient is rejected. (Modules\04 §5)
- AC5: A visit's `AppointmentId` is optional; omitting it succeeds (walk-in case), and supplying an unknown or mismatched one is rejected. (Modules\04 §5, §7)
- AC6: All Consultation endpoints reject unauthenticated requests with `401`. (BRD Security NFR)
- AC7: The full flow — open patient, address vitals/complaints/diagnosis, save — is achievable with no more than a handful of clicks and no forced multi-step wizard, consistent with the 2–3 minute target. (BRD Success Criteria; Modules\04 §11)
- AC8: The previously-disabled "Consultations (coming soon)" placeholder on Patient Detail is replaced with a real, data-backed section. (This task's explicit instruction; parallels Module 3's AC10 for Appointments.)

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| No unit is specified for Temperature or a format for Blood Pressure (BRD/Modules\04 silent) | Stored values could be misinterpreted (°C vs °F) or inconsistent in shape (free-text BP) across consultations, undermining clinical usefulness | Flagged explicitly (§4, §14 Open Question 1) as an assumption; free-form BP string and unit-agnostic Temperature decimal chosen deliberately over inventing structured validation the BRD doesn't call for (structured/coded data is explicitly out of scope); easy to revisit with Product Owner input before Increment 1 closes |
| Vitals "value + NotRecorded conflict" is resolved by silent server-side normalization rather than rejection | A doctor could unknowingly have a typed value discarded if a stale "Not recorded" toggle state slips through | Client keeps the two mutually exclusive in the UI so this should rarely occur in practice; documented explicitly as a deliberate choice (§4, §14 Open Question 2) favoring "never block" over "always trust the raw payload" — reversible by making it a rejection instead if Product Owner prefers strictness |
| "Untouched vital defaults to Not Recorded on submit" (§3.3 step 4) is an interpretation, not an explicit BRD rule | Could produce more "not recorded" vitals than intended if doctors expect an explicit prompt per field instead | Documented as an explicit interpretation (§14 Open Question 3); isolated to client-side submit logic, easy to change to a mandatory per-field touch requirement if Product Owner disagrees |
| Auto-transitioning a linked appointment's status to `Completed` on visit save is deferred, not built in Increment 1 | Doctor may expect appointment status to update automatically after a consultation, and be surprised it doesn't (until Increment 3, pending sign-off) | Explicitly scoped out of Increment 1 and flagged (§4, §9 task 20, §14 Open Question 4) rather than silently built in or silently omitted; doctor can still manually update appointment status via Module 3's existing status control in the meantime |
| Minimal patient-scoped visit list (§3.2) is a thin precursor to Module 6, not that module's real implementation | Risk of scope confusion or duplicated UI work if Module 6 rebuilds this section from scratch instead of extending it | Documented explicitly (§2 assumptions, §3.2) as intentionally minimal and superseded by Module 6; Module 6's planning should treat this section as a starting point to extend, not a parallel competing view |
| `Visits` carries the most clinically sensitive data in the app so far, with encryption-at-rest/backup responsibility living in a not-yet-built module (Module 9) | Until Module 9 is implemented, "encrypted at rest" (R10) is not actually enforced at the infrastructure level | Documented as an explicit dependency gap (§11); this module's own obligation is limited to not introducing a parallel unencrypted storage/logging path; flag to whoever plans/builds Module 9 that `Visits` is now a table requiring coverage |

---

## Open Questions — Requiring Product Owner Confirmation

1. **Temperature unit and Blood Pressure format** (BRD/Modules\04 specify neither): this plan proposes a unit-agnostic `decimal(4,1)` for Temperature and a free-form string for Blood Pressure (e.g., `"120/80"`), deliberately avoiding structured/coded validation (explicitly out of scope). Confirm this is acceptable, or whether a fixed unit/format should be enforced in the UI (still without becoming "structured/coded diagnosis," which remains out of scope).
2. **Vitals value/"Not recorded" conflict resolution**: this plan normalizes a conflicting payload (value present + `NotRecorded = true`) by discarding the value and keeping `NotRecorded = true`, rather than rejecting the request. Confirm this "never block" interpretation of R2 is correct, versus a stricter alternative that would reject such a payload as malformed.
3. **"Untouched vital defaults to Not Recorded on submit"**: confirm whether an untouched vital field should silently save as "not recorded" (this plan's default) or whether the UI should require an explicit per-field acknowledgment (value or toggle) before allowing submit — the latter adds a click per field, working against the 2–3 minute target, but the former is an inferred convenience, not a stated rule.
4. **Auto-transitioning appointment status to `Completed` on visit save**: Modules\04/BRD never states this coupling. This plan defers it to Increment 3, pending explicit sign-off, rather than building it into Increment 1 or omitting it permanently. Confirm whether this coupling is wanted, and if so, whether it should be automatic (as sequenced) or a separate manual doctor action.
5. **Consultation edit-after-save** (`PUT /api/visits/{id}`, Increment 3): Modules\04 §4 lists only Vitals Capture, Complaints Entry, Diagnosis Entry, and Save — it does not explicitly name "edit a saved consultation." This plan infers editing is a reasonable need (clinical notes sometimes need correction) and sequences it as Increment 3. Confirm whether this is in scope for Phase 1, or whether a saved consultation should be treated as permanent/append-only (the latter would let this plan drop the `PUT` endpoint and Increment 3 task 19 entirely).

---

## Dependencies Recap (for sequencing awareness)

This module sits fourth in the fixed build order (Authentication → Patient Management → Appointment Management → **Consultation & Clinical Records** → Prescription & Medication Management → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Module 5 (Prescription & Medication Management) should not begin its own visit-referencing schema work until this module's `Visits` table/migration (Increment 1, tasks 1–3) is merged, since a prescription is generated from an existing visit's data. Module 6 (Patient History) should treat this module's minimal patient-scoped visit list (§3.2, Increment 2) as a starting point to extend into its full chronological, date-filterable view, not a parallel implementation to replace outright.
