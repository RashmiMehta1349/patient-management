# Module 2: Patient Management — Implementation Plan

## 1. Module Overview

Patient Management is the anchor module of the application: it owns the `Patient` entity that every other clinical module (Appointments, Consultations, Prescriptions, History, Search, Export) references by foreign key. It gives the doctor a fast way to register a new patient, correct a patient's details as they change over time, view a patient's profile as the entry point into a visit, and locate an existing patient by name or phone. Because Authentication (Module 1) is already built, this module is the first to run behind the JWT-protected API surface, and its data shape (the `Patients` table and its DTOs) becomes the schema every downstream module builds against — so getting the entity and validation rules right here has outsized leverage on the rest of the build.

The driving user request for this increment is specifically "doctor will create patient details and it will be saved into a database table" — i.e., **Add Patient** is the first slice to ship. This plan covers full Module 2 scope (Add, Edit, View, Search) per `Modules\02_Patient_Management.md`, sequenced so Add Patient + the `Patients` table/migration land first.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Add, edit, and view patient details | BRD `Functional Requirements → Patient Management`; Modules\02 §4 #1–3 |
| R2 | Capture Name, Age/DOB, Gender, Contact details as the minimum required fields | BRD `Functional Requirements → Patient Management`; Modules\02 §5 |
| R3 | Search patients by name or phone number | BRD `Functional Requirements → Patient Management`; Modules\02 §4 #4 |
| R4 | A patient record, once created, persists indefinitely — no deletion workflow | Modules\02 §5 |
| R5 | Patient search/retrieval should complete within the 2–5 second Success Criteria target | BRD `Success Criteria`; Modules\02 §11 |
| R6 | Simple, minimal UI for fast entry (Usability NFR) | BRD `Non-Functional Requirements → Usability`; Modules\02 §11 |
| R7 | Must be logged in (JWT-authenticated) to access patient data | Modules\02 §8 Dependencies; BRD Security NFR |
| R8 | Patient search here is simple name/phone lookup; general "partial match" quick search across the app is Module 7's job but queries this module's data | Modules\02 §5 |

**Explicitly out of scope for this module** (do not build): merging duplicate records, patient self-service portal/login, multi-clinic patient sharing, insurance/billing identifiers, patient deletion. If asked for any of these, flag the conflict with BRD Out-of-Scope / Modules\02 §3 rather than building it.

## 3. Workflows

### 3.1 Register (Add) Patient
1. Doctor navigates to "New Patient" from the dashboard or search screen.
2. Doctor enters Full Name, Date of Birth (or Age), Gender, Phone Number; optionally Address/Email if the team elects to extend beyond BRD minimums (see Open Questions).
3. Client performs inline validation (required fields, phone format, DOB not in future).
4. On submit, client calls `POST /api/patients` with a Bearer token.
5. API validates payload server-side (defense in depth), persists a new `Patient` row with `CreatedAt`/`UpdatedAt` timestamps, returns the created patient with its generated ID.
6. Client navigates to the new Patient Profile view, confirming the record is immediately retrievable (Acceptance Criteria, Modules\02 §10).

### 3.2 Edit Patient
1. Doctor opens an existing patient's profile and selects "Edit."
2. Form pre-populates with current values from `GET /api/patients/{id}`.
3. Doctor changes one or more fields (e.g., phone number).
4. Client calls `PUT /api/patients/{id}` with the full updated payload (or `PATCH` for partial — see Architecture Approach).
5. API re-validates, updates the row, bumps `UpdatedAt`, returns the updated patient.
6. Client refreshes the profile view showing the persisted change.

### 3.3 View Patient Profile
1. Doctor arrives at a patient's profile either from search results, recent-patient list (Module 7), or directly after registration.
2. Client calls `GET /api/patients/{id}`.
3. Profile displays demographic details plus a placeholder/cross-navigation area for "Appointments," "Consultations," and "History" tabs (those modules populate their own content later; this module only needs to expose navigation anchors, not their data).

### 3.4 Search Patient
1. Doctor types into a search box (name or phone) on the Patients list / dashboard.
2. Client calls `GET /api/patients?query={term}` (debounced client-side, e.g., 300ms, to respect the 2–5s NFR without hammering the API).
3. API performs a case-insensitive partial match against `FullName` and an exact/partial match against `PhoneNumber`, returns a result list.
4. Doctor selects a result to open that Patient Profile (3.3).
5. If no results, client shows an empty state with a shortcut to "Add Patient."

## 4. Architecture Approach

- **Layering**: Follow the existing Clean Architecture split proven in Module 1 — `PatientManagement.Domain` (Patient entity), `PatientManagement.Application` (Patients feature: DTOs, Commands/Queries, validation, repository interface), `PatientManagement.Infrastructure` (EF Core configuration, repository implementation, migration), `PatientManagement.Api` (PatientsController), `PatientManagement.Tests` (unit + integration). No code is written in this plan, but this is the structure implementation should target.
- **CQRS-lite via Commands, matching Module 1's pattern** (`LoginCommand`, `ForgotPasswordCommand` precedent): `CreatePatientCommand`, `UpdatePatientCommand`, `GetPatientByIdQuery`, `SearchPatientsQuery`. Keeps validation and persistence orchestration out of controllers, consistent with the Auth module's existing style.
- **Validation placement**: Client-side validation for UX responsiveness (required fields, format hints) plus mandatory server-side validation in the Application layer (never trust the client) — required because this is the only enforcement point that guarantees data integrity for every future module hanging off `Patient`.
- **Full-update vs partial-update for Edit**: Use `PUT` with the full patient payload (simpler, matches a single edit form that shows all fields at once) rather than `PATCH`. Rationale: the BRD doesn't call for partial-field API updates, and a single form covers all editable fields, so `PUT` avoids the extra complexity of partial-payload merge logic.
- **Soft, not hard, uniqueness constraints**: BRD does not require enforcing unique phone numbers (patients could share a household phone; no "prevent duplicate" business rule stated). No DB unique constraint on `PhoneNumber`; duplicate name/phone is allowed. This directly maps to Modules\02 §3 excluding "merging duplicate records" — the module doesn't attempt duplicate prevention at all.
- **No delete endpoint**: Per Modules\02 §5, records persist indefinitely. Do not implement `DELETE /api/patients/{id}` in this increment; if a future need arises, treat it as a scope change requiring Product Owner sign-off.
- **Search implementation**: Server-side `LIKE`/`Contains()` (EF Core translated) against `FullName` and `PhoneNumber`, backed by non-clustered indexes on both columns to meet the 2–5s NFR (R5) even as patient volume grows moderately (BRD Scalability NFR — "moderate patient volume").
- **Auth**: All endpoints require the existing `RequireAuthenticatedUser` fallback policy from `Program.cs` — no new `[AllowAnonymous]` surface is introduced by this module.
- **Rendering**: Angular standalone components, following the `features/auth/*` structure precedent — a new `features/patients/*` area with `list`, `detail`, `form` components, plus a `core/patients/patient.service.ts` and `patients.models.ts` mirroring `core/auth`'s structure.

## 5. Database Entities

### `Patients` table

| Field | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` (GUID), PK | Matches `User`/`PasswordResetToken` PK convention already in use (confirm against `UserConfiguration.cs`; if Module 1 used GUIDs, stay consistent) |
| `FullName` | `nvarchar(200)`, required | R2; indexed for search |
| `DateOfBirth` | `date`, nullable | R2 — "Age/DOB"; store DOB as the canonical field, compute Age in the UI/DTO rather than persisting a derived Age (avoids drift as time passes) — flagged as an architecture decision, not a BRD-mandated one |
| `Gender` | `nvarchar(20)`, required | R2 — picklist (Male/Female/Other), enforced client-side and server-side; confirmed with Product Owner |
| `PhoneNumber` | `nvarchar(20)`, required | R2; indexed for search; no uniqueness constraint (see Architecture Approach) |
| `CreatedAt` | `datetime2`, required | Audit/history ordering |
| `UpdatedAt` | `datetime2`, required | Bumped on every edit |

**Indexes**: non-clustered index on `FullName` (search), non-clustered index on `PhoneNumber` (search). No FK relationships originate from this table; it is the referenced side for `Appointments.PatientId`, `Consultations.PatientId`, etc. in later modules.

## 6. APIs

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `POST` | `/api/patients` | Create (register) a new patient | Bearer JWT required | `201` + `PatientDto`, `Location` header → `GET /api/patients/{id}` (via `CreatedAtAction`, Increment 2) | `400` invalid payload |
| `GET` | `/api/patients/{id}` | Retrieve a single patient's full profile | Bearer JWT required | `200` + `PatientDto` | `404` unknown id |
| `PUT` | `/api/patients/{id}` | Update an existing patient's details (full payload) | Bearer JWT required | `200` + updated `PatientDto` | `404` unknown id (checked first) / `400` invalid payload |
| `GET` | `/api/patients?query={term}` | Search patients by partial name or phone match | Bearer JWT required | `200` + list (Increment 3) | — |

All routes sit behind the existing fallback `RequireAuthenticatedUser` policy — no controller-level `[AllowAnonymous]` needed. Validation failures return `400` with field-level error detail (matching the `Result`-pattern precedent in `PatientManagement.Application\Common\Result.cs`, extended in Increment 2 with an `IsNotFound` flag — see §9a.2). Not-found lookups return `404`, checked before validation on `PUT` since there's no point validating a payload for a record that doesn't exist.

## 7. UI / Screens

- **Patients List / Search screen** (`features/patients/list`): search box (name/phone), results table (name, DOB/age, gender, phone), "Add Patient" button, empty state.
- **Patient Form screen** (`features/patients/form`), reused for both Add and Edit: Full Name, DOB (date picker) or Age, Gender (dropdown), Phone, optional Email/Address fields, Save/Cancel actions, inline validation messages.
- **Patient Profile / Detail screen** (`features/patients/detail`): read-only display of all captured fields, "Edit" action, and placeholder navigation tabs/links for Appointments / Consultations / History (wired up as those modules land — Module 2 only needs to render the anchors, not the content). Loading / loaded / not-found / error states (see §9a.7).
- **Patient Form screen, edit-mode**: same `features/patients/form` component as Add, extended (not duplicated) to pre-populate from `GET /api/patients/{id}` and submit via `PUT`; see §9a.6 for the reuse-vs-separate-component decision and rationale.
- **Dashboard integration**: extend the existing `dashboard.component` with a "Recent Patients" or "Search Patients" entry point so the doctor's post-login flow reaches this module directly.

## 8. Dependencies

- **Upstream**: Authentication & Authorization (Module 1) — already built; every endpoint here requires a valid JWT and the existing `auth.guard`/`auth.interceptor` protect the client routes.
- **Downstream**: Appointment Management (3), Consultation & Clinical Records (4), Prescription & Medication Management (5), Patient History (6), Search & Navigation (7), Data Export (8) — all store a FK to `Patients.Id` and/or query this module's search. None of those modules should start their own patient-referencing schema work until this module's `Patients` table and migration are merged, since the shape frozen here is what they build against.

## 9. Implementation Tasks

**Increment 1 — Add Patient + schema (priority slice per the driving request)**
1. Add `Patient` entity to `PatientManagement.Domain\Entities`.
2. Add `PatientConfiguration` (EF Core Fluent API) to `PatientManagement.Infrastructure\Persistence\Configurations`, register in `PatientManagementDbContext`.
3. Generate and apply EF Core Code-First migration (`AddPatientsTable`), following the pattern of the existing `InitialCreate` migration.
4. Add `IPatientRepository` interface (Application layer) + `PatientRepository` implementation (Infrastructure), mirroring `IUserRepository`/`UserRepository`.
5. Add `CreatePatientCommand` + handler + validation (Application layer), `CreatePatientRequestDto`/`PatientDto`.
6. Add `PatientsController` with `POST /api/patients`, wired to `RequireAuthenticatedUser`.
7. xUnit unit tests for `CreatePatientCommand` (valid input, missing required fields, invalid DOB/gender).
8. xUnit integration test for `POST /api/patients` end-to-end (auth required, 201 + retrievable via direct DB check or immediate GET).
9. Angular: `patient.service.ts`, `patients.models.ts`, `features/patients/form` component wired to create-mode, route + guard registration in `app.routes.ts`.
10. Angular unit/component tests for the form (required-field validation, submit success/error handling).

**Increment 2 — View + Edit**

> Superseded by §9a "Increment 2 — Detailed Design" below, which expands tasks 11–15 to implementation-ready detail (exact repository/DTO/handler/controller shapes, Angular component decisions, concurrency/not-found handling, and test cases). This condensed list is kept for at-a-glance sequencing only.

11. Extend `IPatientRepository` (`UpdateAsync`), add `GetPatientByIdQueryHandler` returning `PatientDto?` directly (no `Result<T>` wrapper — mirrors the `AuthController.Me` null-check precedent), wire `GET /api/patients/{id}` on `PatientsController` (200 / 404).
12. Add `UpdatePatientRequestDto`, `UpdatePatientCommand` + handler (reuses `CreatePatientCommandHandler`'s validation logic — extract to a shared static validator), extend `Result<T>` with a non-breaking `NotFound` variant, wire `PUT /api/patients/{id}` on `PatientsController` (200 / 400 / 404). Also switch `POST`'s `Created()` call to `CreatedAtAction(nameof(GetById), ...)` now that the GET route exists.
13. Unit tests: `GetPatientByIdQueryHandler` (found → correct DTO incl. recomputed `Age`; not found → null). `UpdatePatientCommandHandler` (valid edit persists + bumps `UpdatedAt`; same validation failures as create; unknown `Id` → `NotFound` result). Integration tests: `GET`/`PUT` auth-required (401), `GET` 200/404, `PUT` 200/400/404, edited field visible on a subsequent `GET`.
14. Angular: add `getById()`/`update()` to `patient.service.ts`; add `features/patients/detail/patient-detail.component.ts` (new); extend `features/patients/form/patient-form.component.ts` in place to support edit-mode via route param (no new form component — see §9a rationale); register `patients/:id` and `patients/:id/edit` routes in `app.routes.ts`, taking care that the literal `patients/new` route is declared before the `patients/:id` wildcard so it keeps matching correctly.
15. Component tests: detail component (loading/loaded/not-found/error states, Edit button navigation), form component in edit-mode (pre-population from resolved patient, PUT submit success/error, validation reuse, Cancel/navigation back to detail instead of the create-mode "register another" flow).

**Increment 3 — Search**
16. Add `SearchPatientsQuery` + handler (name/phone partial match), `GET /api/patients?query=` endpoint.
17. Add DB indexes on `FullName`/`PhoneNumber` via migration.
18. Unit/integration tests for search (name match, phone match, partial match, no results).
19. Angular `features/patients/list` component with debounced search input, results table, empty state.
20. Dashboard integration — add navigation entry point to Patients list/search.

**Cross-cutting**
21. Confirm PK type/GUID convention against Module 1's `User`/`PasswordResetToken` entities before writing the migration, to keep the schema internally consistent.
22. Resolve Open Questions (Gender picklist values, Email/Address inclusion, pagination) with Product Owner before Increment 1 sign-off if they block the entity shape.

## 9a. Increment 2 — Detailed Design

This section expands §9 tasks 11–15 to implementation-ready detail. It reflects the **actual current shape** of the Increment 1 code (verified by reading the files directly, not assumed): `IPatientRepository` today only has `GetByIdAsync`/`AddAsync`; `PatientsController` only has `POST`, returning a manually-built `Created()` Location header pointing at a route that doesn't exist yet; `Result<T>` has no not-found concept, only `Succeeded`/`Value`/`Error`; `CreatePatientCommandHandler` has a private `Validate(...)` and an `internal static ToDto(Patient, DateTime)` that Increment 2 should reuse rather than duplicate.

### 9a.1 Backend — Repository

- Extend `IPatientRepository` with `Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)`. `GetByIdAsync` already exists and is reused as-is for both the query handler and the update handler's "does this patient exist" check — no new repository method needed for lookup.
- `PatientRepository.UpdateAsync`: since EF Core's change tracker already has the entity attached from a prior `GetByIdAsync` call within the same handler (same `DbContext` scope, scoped per-request), `UpdateAsync` can simply call `await _dbContext.SaveChangesAsync(cancellationToken)` — no explicit `Update()`/`Attach()` call needed as long as the handler mutates the tracked instance returned by `GetByIdAsync` rather than constructing a new detached `Patient`. This is the same "load, mutate, save" pattern implicitly used by `UserRepository.UpdateAsync` for `LastLoginAt` in `LoginCommandHandler` — confirm that method's shape before writing this one, to stay consistent.

### 9a.2 Backend — `Result<T>` extension (new, non-breaking)

Increment 2 is the first place the codebase needs to distinguish "validation failed" (400) from "record doesn't exist" (404) inside a single `Result<T>`-returning handler. Rather than inventing a new return type, extend `Result<T>` with a third state:

- Add `public bool IsNotFound { get; }` (defaults `false` on the existing `Success`/`Failure` factories — fully backward compatible with `LoginCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `CreatePatientCommand`, none of which need to change).
- Add a new factory: `public static Result<T> NotFound(string error) => new(false, default, error, isNotFound: true);`
- `UpdatePatientCommandHandler` returns `Result<PatientDto>.NotFound("Patient not found.")` when `GetByIdAsync` returns null, before running field validation (no point validating a payload for a patient that doesn't exist).
- `PatientsController.Update` checks `result.IsNotFound` first, then `!result.Succeeded` (validation), matching the 404-before-400 precedence a caller would expect.

### 9a.3 Backend — Query: `GetPatientByIdQuery`

No `Result<T>` wrapper — not-found is a normal, expected outcome for a GET-by-id, not an application error, matching the existing `AuthController.Me` null-check precedent (return `PatientDto?`, controller maps `null` → `NotFound()`).

- `GetPatientByIdQueryHandler.HandleAsync(Guid id, CancellationToken)` → `Task<PatientDto?>`: calls `_patientRepository.GetByIdAsync(id, ct)`; if null, return null; else return `CreatePatientCommandHandler.ToDto(patient, _dateTimeProvider.UtcNow)` (reuse the existing `internal static` method — requires `InternalsVisibleTo` already in place for `PatientManagement.Tests`, confirm it also covers cross-handler use within the same assembly, which it does since both live in `PatientManagement.Application`).
- Controller: `[HttpGet("{id:guid}")] GetById(Guid id, CancellationToken)` → `200 OK` with `PatientDto`, or `404 NotFound()` (no body needed, or `{ message = "Patient not found." }` for consistency with the existing error-shape convention used elsewhere in this controller/`AuthController`).

### 9a.4 Backend — Command: `UpdatePatientCommand`

- New DTO `UpdatePatientRequestDto`: identical shape to `CreatePatientRequestDto` (`FullName`, `DateOfBirth` as `yyyy-MM-dd` string, `Gender`, `PhoneNumber`) — no `Id` field in the body; `Id` comes from the route. Full-payload `PUT` per the already-settled Architecture Approach (§4) — the client always sends all four fields, not a partial diff.
- Extract `CreatePatientCommandHandler.Validate(...)`'s field-validation body into a shared static helper (e.g., `PatientValidation.Validate(string fullName, string dateOfBirth, string gender, string phoneNumber, DateTime utcNow, out DateOnly dob) : List<string>`) that both `CreatePatientCommandHandler` and `UpdatePatientCommandHandler` call, so the create-path behavior (task 5) isn't duplicated or allowed to drift from the edit-path behavior. Place it in `PatientManagement.Application\Patients\PatientValidation.cs` alongside `PatientGenders.cs`.
- `UpdatePatientCommandHandler.HandleAsync(Guid id, UpdatePatientRequestDto request, CancellationToken)`:
  1. `var patient = await _patientRepository.GetByIdAsync(id, ct);` — if null, `return Result<PatientDto>.NotFound("Patient not found.")`.
  2. Run the shared validator against `request`; if errors, `return Result<PatientDto>.Failure(string.Join(" ", errors))` (same shape as create).
  3. Mutate the tracked `patient` in place: `FullName`, `DateOfBirth`, `Gender`, `PhoneNumber` from the validated request, `.Trim()`'d consistently with create; set `UpdatedAt = _dateTimeProvider.UtcNow` — leave `CreatedAt`/`Id` untouched.
  4. `await _patientRepository.UpdateAsync(patient, ct);`
  5. `return Result<PatientDto>.Success(CreatePatientCommandHandler.ToDto(patient, now));`
- Controller: `[HttpPut("{id:guid}")] Update(Guid id, [FromBody] UpdatePatientRequestDto request, CancellationToken)` → `200 OK` with updated `PatientDto` / `404` if `result.IsNotFound` / `400` with `{ message = result.Error }` otherwise.
- While in the controller, also change `POST`'s `Created($"/api/patients/{result.Value!.Id}", ...)` to `CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)` now that `GetById` exists — closes the gap the Increment 1 code comment (`PatientsController.cs` line 33–35) explicitly flagged as pending.

### 9a.5 Concurrency handling — assumption, not a silent decision

The BRD does not mention concurrent-edit conflict handling, and this is a **single-user, single-clinic** application (BRD scope statement) — there is exactly one doctor account, so two simultaneous edits to the same patient by different actors is not a realistic scenario (unlike the multi-tab/multi-device case, which is still technically possible but out of scope to defend against per the BRD's Phase 1 boundaries). **Assumption**: no optimistic concurrency control (no `RowVersion`/`xmin` check) — last-write-wins on `PUT`. Flagged here as an explicit assumption for Product Owner awareness, not built silently as a "just in case" feature, consistent with this plan's stated boundary against inventing unrequested requirements.

### 9a.6 Frontend — one form component vs. a separate edit component

**Decision: extend `patient-form.component.ts` in place to support both create- and edit-mode, rather than adding a second `patient-edit-form` component.**

Rationale:
- The two modes differ only in: (a) whether the form is pre-populated from a `GET`, (b) whether submit calls `create()` or `update(id, ...)`, and (c) the post-submit destination (create → inline "registered successfully" confirmation with a "register another" action; edit → navigate back to the Patient Detail view). The field set, validators, and markup are otherwise identical — duplicating the template/validators into a second component would immediately violate DRY and risk the two modes' validation drifting apart, mirroring the exact reason `CreatePatientCommandHandler`'s validation is being extracted into a shared helper server-side (§9a.4).
- Angular idiom for this (single reactive form, mode driven by route data) is already close to how `patients/new` is wired today (component reads nothing from the route currently; edit-mode adds an `ActivatedRoute` paramMap read).
- Implementation: add an `id: string | null` field resolved from `ActivatedRoute.snapshot.paramMap.get('id')` in `ngOnInit`; when present, call `patientService.getById(id)`, `patchValue()` the form, and set an `isEditMode` flag; `submit()` branches on `isEditMode` to call `update(id, ...)` vs `create(...)`; on edit success, `router.navigate(['/patients', id])` instead of setting `createdPatient`; the existing `createdPatient`/`registerAnother()` inline-confirmation flow stays create-mode-only, gated behind `!isEditMode`.
- Loading state needed for edit-mode's initial `getById()` fetch (the create-mode form currently has no equivalent "loading" state to await, since it starts blank) — add a `loading` flag distinct from `submitting`, and a not-found/error state if `getById()` 404s (e.g., bad/stale URL), rendering an inline message with a link back to `/dashboard` rather than crashing the form.
- Page heading changes from the current hard-coded "Add Patient" (`patient-form.component.html` line 3) to `{{ isEditMode ? 'Edit Patient' : 'Add Patient' }}`, and the Cancel/back link in edit-mode should return to `/patients/{{id}}` rather than `/dashboard`.

### 9a.7 Frontend — Patient Detail component (new)

`features/patients/detail/patient-detail.component.ts`, standalone, mirroring the `patient-form` component's structure:

- On init, reads `id` from `ActivatedRoute.snapshot.paramMap`, calls `patientService.getById(id)`.
- States to render: `loading` (spinner/placeholder), `loaded` (patient fields), `notFound` (404 → inline message + link to `/dashboard`), `error` (unexpected failure → inline message + retry).
- Loaded state displays: Full Name, Date of Birth + computed Age, Gender, Phone Number, and (read-only, not editable here) `CreatedAt`/registration date if useful for context — no Email/Address fields per the resolved Open Question #2.
- "Edit" button/link navigates to `/patients/{{id}}/edit`.
- Placeholder navigation anchors for "Appointments," "Consultations," "History" per §7/Modules\02 — render as visibly inactive/disabled links or a short "coming soon" note (not functional buttons) so it's unambiguous to a doctor testing today's build that those tabs aren't wired to real data yet; avoid implying functionality that doesn't exist.
- No Delete action anywhere on this screen (AC6/§4 "No delete endpoint").

### 9a.8 Frontend — service, models, routes

- `patient.service.ts`: add `getById(id: string): Observable<Patient>` (`GET /patients/{id}`) and `update(id: string, request: UpdatePatientRequest): Observable<Patient>` (`PUT /patients/{id}`). Update the class-level comment (currently "Increment 1 scope: create only...") since it's now stale.
- `patients.models.ts`: add `UpdatePatientRequest` type — identical field shape to `CreatePatientRequest` (can be a type alias, e.g., `export type UpdatePatientRequest = CreatePatientRequest;`, since the payload is the same four fields for both).
- `app.routes.ts`: add two routes — `patients/:id` (lazy-loads `PatientDetailComponent`) and `patients/:id/edit` (lazy-loads `PatientFormComponent`). **Ordering matters**: the existing literal route `patients/new` (line 30) must remain declared before the `patients/:id` parameterized route, or Angular's route matcher will interpret `/patients/new` as `id: 'new'` and route it to the detail component instead of the form. Both new routes need the same `auth.guard` protection already applied to `patients/new`.

### 9a.9 Test cases (concrete, supersedes §12's Increment-2-relevant bullets with specifics)

**Unit — `GetPatientByIdQueryHandler`**
- Existing `Id` → returns a `PatientDto` with `Age` correctly recomputed from `DateOfBirth` against the injected `IDateTimeProvider`'s `UtcNow` (not the real clock — reuse the fake `IDateTimeProvider` test double already used in `CreatePatientCommandTests`).
- Non-existent `Id` → returns `null`.

**Unit — `UpdatePatientCommandHandler`**
- Valid full payload on an existing patient → `Result.Succeeded == true`, returned DTO reflects new values, `UpdatedAt` advances past the original `CreatedAt`/`UpdatedAt`, `CreatedAt`/`Id` unchanged.
- Same four validation failure cases as `CreatePatientCommandTests` (missing name/DOB/gender/phone, future DOB, invalid gender) — confirms the shared validator behaves identically for edit as for create.
- Non-existent `Id` with an otherwise-valid payload → `Result.IsNotFound == true`, `Result.Succeeded == false`.

**Integration (`WebApplicationFactory`, extending `PatientsEndpointsTests`)**
- `GET /api/patients/{id}` without a token → `401`.
- `GET /api/patients/{id}` for a patient created via the existing `POST` flow → `200` with matching fields.
- `GET /api/patients/{id}` for a random unused GUID → `404`.
- `PUT /api/patients/{id}` without a token → `401`.
- `PUT /api/patients/{id}` with a valid payload on an existing patient → `200`, and a subsequent `GET` on the same id reflects the change (this is AC2, and is the first test able to use a real `GET` instead of a direct-DbContext check, unlike the Increment 1 comment at `PatientsEndpointsTests.cs` line 20–21 which had to work around the missing endpoint).
- `PUT /api/patients/{id}` with an invalid payload (e.g., blank name) on an existing patient → `400`.
- `PUT /api/patients/{id}` for a random unused GUID with an otherwise-valid payload → `404`.
- `POST /api/patients` still returns `201` and now a `Location` header that resolves via the new `GetById` route (regression check on the `CreatedAtAction` change in §9a.4).

**Angular component tests**
- `patient-detail.component`: renders loading → loaded transition given a mocked `PatientService.getById` observable; renders not-found state on a `404` error response; Edit button link points at `/patients/{id}/edit`.
- `patient-form.component` in edit-mode: pre-populates all four fields from a mocked `getById` response; submit calls `PatientService.update` with the route id and form values, not `create`; on success navigates to `/patients/{id}` (spy on `Router.navigate`); on server-side validation error, displays the same inline error banner used by create-mode; Cancel link points at `/patients/{id}`, not `/dashboard`.
- `patient-form.component` in create-mode: existing Increment 1 tests continue to pass unmodified (regression check that the edit-mode branch doesn't change create-mode behavior).

## 10. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Domain/
    Entities/
      Patient.cs
  PatientManagement.Application/
    Patients/
      Dtos/
        PatientDto.cs
        CreatePatientRequestDto.cs
        UpdatePatientRequestDto.cs        # new, Increment 2
      Commands/
        CreatePatientCommand.cs
        UpdatePatientCommand.cs           # new, Increment 2 (handler + NotFound-aware Result usage)
      Queries/
        GetPatientByIdQuery.cs            # new, Increment 2 (handler only — returns PatientDto?, no Result<T>)
        SearchPatientsQuery.cs
      Services/
        IPatientRepository.cs             # extended, Increment 2: + UpdateAsync
      PatientValidation.cs                # new, Increment 2 — shared field validator extracted from CreatePatientCommandHandler
      PatientGenders.cs
  PatientManagement.Application/
    Common/
      Result.cs                           # extended, Increment 2: + IsNotFound flag + NotFound(string) factory
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        PatientConfiguration.cs
    Repositories/
      PatientRepository.cs                # extended, Increment 2: + UpdateAsync
    Migrations/
      <timestamp>_AddPatientsTable.cs
  PatientManagement.Api/
    Controllers/
      PatientsController.cs               # extended, Increment 2: + GetById, + Update; POST switches to CreatedAtAction
  PatientManagement.Tests/
    Unit/Patients/
      CreatePatientCommandTests.cs
      GetPatientByIdQueryHandlerTests.cs   # new, Increment 2
      UpdatePatientCommandTests.cs         # new, Increment 2
      SearchPatientsQueryTests.cs
    Integration/Patients/
      PatientsEndpointsTests.cs            # extended, Increment 2: GET/PUT cases added

src/client/src/app/
  core/patients/
    patient.service.ts                    # extended, Increment 2: + getById, + update
    patients.models.ts                    # extended, Increment 2: + UpdatePatientRequest
  features/patients/
    list/
      patients-list.component.ts / .html / .scss
    form/
      patient-form.component.ts / .html / .scss   # extended, Increment 2: create- and edit-mode
      patient-form.component.spec.ts
    detail/
      patient-detail.component.ts / .html / .scss  # new, Increment 2
      patient-detail.component.spec.ts              # new, Increment 2
```

## 11. Security Considerations

- Every `PatientsController` endpoint relies on the existing JWT bearer requirement (`RequireAuthenticatedUser` fallback policy) — no `[AllowAnonymous]` should ever be added here (BRD Security NFR: "Secure login (single user authentication)").
- Server-side validation on every write (Create/Update) regardless of client-side checks — prevents malformed or malicious payloads from corrupting the anchor entity that all other modules depend on.
- No PII beyond what's clinically necessary (Name, DOB, Gender, Phone, optional Email/Address) — consistent with BRD's minimal-scope stance; do not add SSN/insurance ID fields (explicitly out of scope).
- Data in transit protected via the app's existing HTTPS enforcement (BRD Security NFR: encryption at rest and in transit); data at rest encryption is a Module 9 (Backup & Reliability) concern for the DB itself, not something this module implements directly, but this module must not weaken it (e.g., no plaintext export outside the sanctioned Module 8 flow).
- Input sanitization on `FullName`/search `query` parameters to prevent injection — mitigated structurally by using parameterized EF Core LINQ queries (`Contains()`), never raw SQL string concatenation.

## 12. Test Strategy

**Unit tests (xUnit, Application layer)**
- `CreatePatientCommand`: succeeds with all required fields; fails when Name/DOB/Gender/Phone missing; fails on DOB in the future; fails on malformed phone number.
- `UpdatePatientCommand`: succeeds with valid changes; fails validation same as create; fails with `NotFound` result for a non-existent `Id`.
- `SearchPatientsQuery`: returns exact and partial name matches (case-insensitive); returns phone matches; returns empty list for no matches; returns multiple patients when several match.

**Integration tests (xUnit + `WebApplicationFactory`, mirroring `AuthWebApplicationFactory`)**
- `POST /api/patients` without a Bearer token returns `401`.
- `POST /api/patients` with valid token + valid payload returns `201` and the patient is retrievable via `GET /api/patients/{id}` immediately after (Acceptance Criteria #1).
- `POST /api/patients` with missing required field returns `400` with field-level errors.
- `PUT /api/patients/{id}` persists an edited field and a subsequent `GET` reflects it (Acceptance Criteria #2).
- `GET /api/patients?query=` returns correct matches for full name, partial name, and phone number (Acceptance Criteria #3).
- `GET /api/patients/{id}` for a non-existent ID returns `404`.

**E2E (Angular, e.g., via Playwright/Cypress if adopted, or component-level Angular Testing Library)**
- Doctor logs in, registers a new patient via the form, is redirected to the profile, and sees the entered data (full "add patient in under a minute" user story).
- Doctor edits an existing patient's phone number and confirms the change is visible after navigating away and back.
- Doctor searches by partial name and by phone, selects a result, and lands on the correct profile.

**Performance**
- Search endpoint response time under representative "moderate" patient volume (e.g., a few thousand rows) stays within the 2–5s Success Criteria target (R5) — validate via a simple load/timing test once the index is in place; flag if volume assumptions need Product Owner input.

## 13. Acceptance Criteria

- AC1: Doctor can create a new patient with Name, DOB/Age, Gender, and Phone, and the record is immediately retrievable via `GET`. (Modules\02 §10)
- AC2: Doctor can edit any field on an existing patient and the change persists across a subsequent fetch. (Modules\02 §10)
- AC3: Searching by full name, partial name, or phone number returns the correct matching patient(s) and excludes non-matches. (Modules\02 §10)
- AC4: The Patient Profile view displays enough information (name, DOB/age, gender, phone) to positively identify the patient before starting a consultation. (Modules\02 §10)
- AC5: All Patient Management endpoints reject unauthenticated requests with `401`. (BRD Security NFR)
- AC6: No delete/merge functionality is exposed anywhere in the UI or API for this module. (Modules\02 §3, §5)
- AC7: Patient search returns results within the BRD's 2–5 second target under representative load. (BRD Success Criteria)

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Gender field values/format not specified in BRD | Schema/UI rework if Product Owner wants different picklist or free text later | Flag as open question now; implement as a small constrained picklist (Male/Female/Other) that's easy to extend; confirm with Product Owner before Increment 1 closes |
| Age vs DOB storage ambiguity (BRD says "Age/DOB" without specifying which is canonical) | Downstream modules (History, Prescription header) may need age-at-visit computed differently than assumed | Store DOB as canonical, compute Age on read; confirm this is acceptable, not a silent decision that blocks a BRD-stated need for an "Age" field only |
| Optional Email/Address fields not in BRD minimum set | Scope creep beyond BRD if added without sign-off | Keep them nullable/omit entirely from v1 unless Product Owner explicitly requests; do not surface as required |
| No uniqueness constraint on phone/name could allow accidental true duplicates, but BRD also excludes "merging duplicates" as a concern | Doctor could end up with fragmented records for the same real patient without a repair path | Accept as BRD-aligned (Modules\02 §3 explicitly excludes merge); note to Product Owner as a known Phase 1 limitation, not silently fixed |
| Search performance at scale unverified until real data volumes exist | Could miss the 2–5s NFR if patient volume grows beyond "moderate" | Add indexes proactively (§5); include a performance test in Increment 3; revisit indexing/pagination strategy if volume assumptions change |
| Pagination for a full patient list/browse view not explicitly required by BRD (only "search") | Could build an unnecessary feature, or conversely fail a real doctor workflow if a browse-all view turns out to be needed | Treated as optional in this plan (§6, "Optional" list endpoint); confirm with Product Owner whether a browse-all view is needed or search-only is sufficient |
| PK type consistency with Module 1 entities not yet confirmed from this plan alone | Migration rework if `Patient.Id` type diverges from established convention | Task #21 explicitly requires confirming `User`/`PasswordResetToken` PK type before writing the `Patient` entity/migration |
| No optimistic concurrency control on `PUT` (Increment 2) | Last-write-wins if the same patient record is somehow edited from two open tabs/sessions | Documented assumption (§9a.5): acceptable for a single-user, single-clinic app per BRD scope; revisit only if Product Owner flags real-world multi-tab conflicts |
| `Result<T>` extended with a new `IsNotFound` state (Increment 2) touches a shared type used by Module 1's Auth handlers | Regression risk if the extension isn't purely additive | §9a.2 specifies the extension as backward-compatible (`IsNotFound` defaults `false` on existing factories); Increment 2 tests should include a quick regression pass on existing Auth unit tests after the change |
| Angular route ordering (`patients/new` vs `patients/:id`) is order-sensitive | `/patients/new` could silently resolve to the detail component with `id: 'new'` if routes are declared/reordered incorrectly | §9a.8 flags this explicitly; add an integration/E2E check that navigating to `/patients/new` still renders the create form, not a "patient not found" state |

---

## Open Questions — Resolved by Product Owner

1. Gender field: **fixed picklist (Male/Female/Other)**.
2. Email/Address: **not captured** — strictly the four BRD fields (Name, DOB, Gender, Phone).
3. Browse-all paginated list: **not built in Increment 1** — search only; can be added later if needed.
4. DOB vs Age: **store `DateOfBirth`, compute Age on read**.

## Open Questions — Resolved by Product Owner (Increment 2 Planning)

5. Concurrency handling on edit: **last-write-wins, no RowVersion/optimistic concurrency check** — matches single-user/single-clinic Phase 1 scope.
6. 404 response body shape: **`404` with a `{ message: "Patient not found." }` body**, matching the existing error-shape convention elsewhere in the API.

---

## Dependencies Recap (for sequencing awareness)

This module sits second in the fixed build order (Authentication → **Patient Management** → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Modules 3–8 should not begin their own patient-referencing schema work until the `Patients` table (Increment 1, task 1–3) is merged, since they all take a foreign-key dependency on it.
