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

### 3.5 Browse All Patients (grid) — new, Increment 3 revision
1. Doctor selects "Patients" from the app header navigation menu (available on every authenticated screen, not just the dashboard).
2. Client calls `GET /api/patients` (no `query` param) — the full, unfiltered patient list — and renders it as a grid/table.
3. Doctor clicks "Add Patient" (button on the grid page) to reach `patients/new` (existing Increment 1 flow, unchanged).
4. Doctor clicks the edit icon on a row to navigate directly to `patients/:id/edit` (existing Increment 2 form, edit-mode, unchanged) — bypassing the Patient Detail (view) screen entirely for this entry point, which is a deliberate one-click affordance the user asked for ("edit icon to edit patient record").
5. If the grid has zero rows (first-run, no patients yet), show an empty state with a shortcut to "Add Patient," matching the search screen's empty-state convention (3.4 step 5).

## 4. Architecture Approach

- **Layering**: Follow the existing Clean Architecture split proven in Module 1 — `PatientManagement.Domain` (Patient entity), `PatientManagement.Application` (Patients feature: DTOs, Commands/Queries, validation, repository interface), `PatientManagement.Infrastructure` (EF Core configuration, repository implementation, migration), `PatientManagement.Api` (PatientsController), `PatientManagement.Tests` (unit + integration). No code is written in this plan, but this is the structure implementation should target.
- **CQRS-lite via Commands, matching Module 1's pattern** (`LoginCommand` precedent): `CreatePatientCommand`, `UpdatePatientCommand`, `GetPatientByIdQuery`, `SearchPatientsQuery`. Keeps validation and persistence orchestration out of controllers, consistent with the Auth module's existing style.
- **Validation placement**: Client-side validation for UX responsiveness (required fields, format hints) plus mandatory server-side validation in the Application layer (never trust the client) — required because this is the only enforcement point that guarantees data integrity for every future module hanging off `Patient`.
- **Full-update vs partial-update for Edit**: Use `PUT` with the full patient payload (simpler, matches a single edit form that shows all fields at once) rather than `PATCH`. Rationale: the BRD doesn't call for partial-field API updates, and a single form covers all editable fields, so `PUT` avoids the extra complexity of partial-payload merge logic.
- **Soft, not hard, uniqueness constraints**: BRD does not require enforcing unique phone numbers (patients could share a household phone; no "prevent duplicate" business rule stated). No DB unique constraint on `PhoneNumber`; duplicate name/phone is allowed. This directly maps to Modules\02 §3 excluding "merging duplicate records" — the module doesn't attempt duplicate prevention at all.
- **No delete endpoint**: Per Modules\02 §5, records persist indefinitely. Do not implement `DELETE /api/patients/{id}` in this increment; if a future need arises, treat it as a scope change requiring Product Owner sign-off.
- **Search implementation**: Server-side `LIKE`/`Contains()` (EF Core translated) against `FullName` and `PhoneNumber`, backed by non-clustered indexes on both columns to meet the 2–5s NFR (R5) even as patient volume grows moderately (BRD Scalability NFR — "moderate patient volume").
- **Auth**: All endpoints require the existing `RequireAuthenticatedUser` fallback policy from `Program.cs` — no new `[AllowAnonymous]` surface is introduced by this module.
- **Rendering**: Angular standalone components, following the `features/auth/*` structure precedent — a new `features/patients/*` area with `list`, `detail`, `form` components, plus a `core/patients/patient.service.ts` and `patients.models.ts` mirroring `core/auth`'s structure.
- **Revision (Increment 3 scope change — see §9b for full rationale)**: The current Angular app has **no app-level header/shell component** — `AppComponent` is a bare `RouterOutlet` (`app.component.ts`), and the only navigation today is the `DashboardComponent`'s own inline logout control plus ad-hoc `routerLink`s scattered per-page. The user's request explicitly asks for a persistent header "Patient" menu tab, which requires introducing a new `AppShellComponent` (or equivalent) — this is new scaffolding, not an extension of an existing nav bar. This also **reopens Open Question 3** ("browse-all paginated list: not built... search only"), which this plan now revises — see the reconciliation note below.
- **Reconciling Open Question 3**: Increment 1 planning explicitly decided against a browse-all list endpoint in favor of search-only. The user's current request — "clicking on Patient Tab, Show Patient records in grid" — unambiguously asks for a browse-all grid, not a search box. This plan treats the new request as **superseding** Open Question 3's prior answer, since the literal ask has no search input at all, just a grid of records reachable from a menu click. Flagged explicitly here rather than silently reconciled; final confirmation from Product Owner requested in the new Open Questions below, but implementation should proceed on the "yes, supersedes" reading since the request is unambiguous and low-risk to build (see pagination decision below).
- **List endpoint shape — paginated, `GET /api/patients`** (Product Owner-confirmed, Open Question 7 resolved): the browse-all endpoint returns a **paged** result, not the full set, ordered by `FullName` ascending. This reverses the original "unpaginated for now" recommendation in this plan — the Product Owner explicitly chose the more scalable option over the leaner unpaginated build. See §9b.1 for the full API contract and §9b.3 for the Angular grid's paging controls.

## 5. Database Entities

### `Patients` table

| Field | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` (GUID), PK | Matches `User` PK convention already in use (confirm against `UserConfiguration.cs`; if Module 1 used GUIDs, stay consistent) |
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
| `GET` | `/api/patients?query={term}&page={n}&pageSize={n}` | Search patients by partial name or phone match, paginated | Bearer JWT required | `200` + paged envelope (Increment 3) | — |
| `GET` | `/api/patients?page={n}&pageSize={n}` (no `query` param) | Browse-all: return one page of the patient list, ordered by `FullName` ascending, for the grid page | Bearer JWT required | `200` + paged envelope (Increment 3 revision, §9b) | — (empty `items` array if no patients / page beyond range) |

All routes sit behind the existing fallback `RequireAuthenticatedUser` policy — no controller-level `[AllowAnonymous]` needed. Validation failures return `400` with field-level error detail (matching the `Result`-pattern precedent in `PatientManagement.Application\Common\Result.cs`, extended in Increment 2 with an `IsNotFound` flag — see §9a.2). Not-found lookups return `404`, checked before validation on `PUT` since there's no point validating a payload for a record that doesn't exist. The browse-all and search behaviors share the same `GET /api/patients` route, distinguished only by whether `query` is present/non-empty, and **both** are paginated using the same `page`/`pageSize` params and response envelope — see §9b.1 for the single-handler design and pagination contract.

## 7. UI / Screens

- **App Header / Shell** (`core/shell/app-shell.component`, new — see §9b.2): persistent top navigation bar shown on every authenticated route, containing at minimum a "Patients" menu tab (routes to `patients`, the new grid page) and the existing logout control (moved out of `DashboardComponent` into the shell so it's available everywhere, not just on the dashboard). Not shown on `login`.
- **Patients Grid screen** (`features/patients/list`, revised scope — was "List / Search" in the original Increment 3 plan, now **browse-all by default, paginated**): grid/table of one page of patient records (columns: Full Name, DOB/Age, Gender, Phone Number, per-row Edit icon), pagination controls below the grid, "Add Patient" button (top of page, routes to `patients/new`), empty state ("No patients yet — Add Patient"). Reachable via the header "Patients" tab. See §9b.3 for the search-box reconciliation (kept, optional, layered on top of the paginated grid — not removed) and pagination control design.
- **Patient Form screen** (`features/patients/form`), reused for both Add and Edit: Full Name, DOB (date picker) or Age, Gender (dropdown), Phone, optional Email/Address fields, Save/Cancel actions, inline validation messages.
- **Patient Profile / Detail screen** (`features/patients/detail`): read-only display of all captured fields, "Edit" action, and placeholder navigation tabs/links for Appointments / Consultations / History (wired up as those modules land — Module 2 only needs to render the anchors, not the content). Loading / loaded / not-found / error states (see §9a.7).
- **Patient Form screen, edit-mode**: same `features/patients/form` component as Add, extended (not duplicated) to pre-populate from `GET /api/patients/{id}` and submit via `PUT`; see §9a.6 for the reuse-vs-separate-component decision and rationale. Reached either from the Patient Detail screen's "Edit" button, or directly from the Patients Grid's per-row edit icon (new entry point, §9b.3).
- **Dashboard integration**: the original plan's dashboard entry point is superseded by the header nav as the primary access path (see §9b.4 reconciliation) — the header tab is the sole, canonical navigation route into this module; the dashboard does not retain a separate "Patients" card/link (Open Question 8, resolved).

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

**Increment 3 — Search + Browse-All Grid + Header Navigation (revised scope)**

> Revised from the original "Search" increment per the user's explicit request for a header-menu-driven, browse-all patient grid with an Add Patient button and per-row edit icon. Original tasks 16–18 (search) are retained unchanged; tasks 19–20 are superseded by 19a–23 below. See §9b for implementation-ready detail.

16. Add `SearchPatientsQuery` + handler (name/phone partial match), `GET /api/patients?query=` endpoint.
17. Add DB indexes on `FullName`/`PhoneNumber` via migration.
18. Unit/integration tests for search (name match, phone match, partial match, no results).
19a. Extend the same `GET /api/patients` endpoint to also serve the **browse-all** case (empty/absent `query` → full list, ordered by `FullName`) — see §9b.1 for the single-handler design.
19b. Unit/integration tests for browse-all (empty query returns all patients ordered by name; zero patients returns empty array; still `401` without a token).
20a. Angular: create `core/shell/app-shell.component` (new) with a header nav bar containing a "Patients" tab and the relocated logout control; wire it into `AppComponent`'s template so it wraps `RouterOutlet` for all authenticated routes (see §9b.2).
20b. Angular: build/revise `features/patients/list` (`PatientsListComponent`) as a **grid** — table of all patients (Full Name, DOB/Age, Gender, Phone, Edit icon column), "Add Patient" button, empty state; default data source is the browse-all call; optional search box layered on top calls the same service method with a `query` param (§9b.3).
21. Add `list()` and/or extend `search()` on `patient.service.ts` to call `GET /api/patients` with an optional `query` param; reconcile into a single method (§9b.1).
22. Register `patients` route (list/grid page) in `app.routes.ts`, guarded by `authGuard`, declared with the same ordering care already documented for `patients/new` vs `patients/:id` (the literal `patients` segment doesn't collide with `patients/:id`, but keep it grouped with the other `patients/*` routes for readability).
23. Angular component tests for `PatientsListComponent` (grid renders rows, Add Patient button navigates to `patients/new`, edit icon navigates to `patients/:id/edit`, empty state renders with zero patients) and for `app-shell.component` (Patients tab present and routes correctly, logout control still works after relocation, hidden on unauthenticated routes).

**Cross-cutting**
21. Confirm PK type/GUID convention against Module 1's `User` entity before writing the migration, to keep the schema internally consistent.
22. Resolve Open Questions (Gender picklist values, Email/Address inclusion, pagination) with Product Owner before Increment 1 sign-off if they block the entity shape.

## 9a. Increment 2 — Detailed Design

This section expands §9 tasks 11–15 to implementation-ready detail. It reflects the **actual current shape** of the Increment 1 code (verified by reading the files directly, not assumed): `IPatientRepository` today only has `GetByIdAsync`/`AddAsync`; `PatientsController` only has `POST`, returning a manually-built `Created()` Location header pointing at a route that doesn't exist yet; `Result<T>` has no not-found concept, only `Succeeded`/`Value`/`Error`; `CreatePatientCommandHandler` has a private `Validate(...)` and an `internal static ToDto(Patient, DateTime)` that Increment 2 should reuse rather than duplicate.

### 9a.1 Backend — Repository

- Extend `IPatientRepository` with `Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)`. `GetByIdAsync` already exists and is reused as-is for both the query handler and the update handler's "does this patient exist" check — no new repository method needed for lookup.
- `PatientRepository.UpdateAsync`: since EF Core's change tracker already has the entity attached from a prior `GetByIdAsync` call within the same handler (same `DbContext` scope, scoped per-request), `UpdateAsync` can simply call `await _dbContext.SaveChangesAsync(cancellationToken)` — no explicit `Update()`/`Attach()` call needed as long as the handler mutates the tracked instance returned by `GetByIdAsync` rather than constructing a new detached `Patient`. This is the same "load, mutate, save" pattern implicitly used by `UserRepository.UpdateAsync` for `LastLoginAt` in `LoginCommandHandler` — confirm that method's shape before writing this one, to stay consistent.

### 9a.2 Backend — `Result<T>` extension (new, non-breaking)

Increment 2 is the first place the codebase needs to distinguish "validation failed" (400) from "record doesn't exist" (404) inside a single `Result<T>`-returning handler. Rather than inventing a new return type, extend `Result<T>` with a third state:

- Add `public bool IsNotFound { get; }` (defaults `false` on the existing `Success`/`Failure` factories — fully backward compatible with `LoginCommand`, `CreatePatientCommand`, none of which need to change).
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

## 9b. Increment 3 — Header Nav + Browse-All Grid — Detailed Design

This section expands §9 tasks 19a–23 to implementation-ready detail, triggered by the user's explicit request: header "Patient" menu tab → grid of all patient records → Add Patient button → per-row edit icon → `patients/:id/edit`.

### 9b.1 Backend — single, paginated `GET /api/patients` handler for both browse-all and search

Rather than adding a second endpoint, extend `SearchPatientsQueryHandler` (or the controller action wrapping it) to branch on whether `query` is present, and to page **both** branches identically (Product Owner-confirmed, Open Question 7 resolved: pagination is added now).

**API contract**

- `[HttpGet] GetAll([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken)`:
  - If `query` is null/empty/whitespace → browse-all branch: call a `GetAllPatientsQueryHandler` (new) that returns a **paged** result over all rows ordered by `FullName`.
  - If `query` is non-empty → search branch: existing `SearchPatientsQueryHandler` (task 16), now also returning a **paged** result over the matching rows, ordered by `FullName`.
  - Both branches return the identical response envelope shape — pagination applies consistently whether browsing or searching, so the client never has to special-case one mode's response shape versus the other's. This is the simpler, more consistent option versus paginating only pure browse-all and leaving search unpaginated.
- **Defaults / bounds**: `page` defaults to `1` (1-based, not 0-based, to match typical UI page-number display); `pageSize` defaults to `25`; `pageSize` is server-clamped to a maximum of `100` (protects against a caller requesting an unbounded page size — defense in depth even though this is a single-trusted-client app). `page < 1` or `pageSize < 1` in the request is treated as the default rather than a `400`, since a malformed page param shouldn't hard-fail a grid render — flagged as an implementation convenience, not a BRD-mandated behavior.
- **Response envelope** (applies to both branches): `{ items: PatientDto[], totalCount: int, page: int, pageSize: int }` — `items` is the current page's rows (empty array if `page` is beyond the last page, not a `404`), `totalCount` is the full matching-row count (all rows for browse-all, matching rows for search) so the client can compute total pages (`Math.Ceiling(totalCount / pageSize)`) without a second round-trip, `page`/`pageSize` echo back the effective (post-clamp/post-default) values actually applied, so the client can reconcile its own state against what the server used.
- Add `Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default)` to `IPatientRepository` (replaces the previously-planned non-paged `GetAllAsync(CancellationToken)` shape — this plan had not yet been implemented, so no prior signature is broken), implemented in `PatientRepository` via `OrderBy(p => p.FullName).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct)` plus a separate `CountAsync(ct)` (EF Core will issue two queries; acceptable at this data volume, avoids a single combined query with window-function complexity for no real benefit here).
- `SearchPatientsQueryHandler`'s existing (already-implemented, task 16) signature similarly gains `page`/`pageSize` parameters and returns the same `(Items, TotalCount)` shape from a `Contains()`-filtered, `Skip`/`Take`-paged query — this is a signature change to an already-built handler from Increment 3's earlier work, called out explicitly since it's a rework, not new-from-scratch.
- Rationale for one route, not two: keeps the API surface small (one client method, one controller action), matches how the user described it ("Show Patient records in grid" — a superset of "search," not a parallel feature), and avoids the client needing to decide which of two endpoints to call.

### 9b.2 Frontend — App Shell / Header Navigation (new)

No header/shell component exists today (`app.component.ts` is a bare `RouterOutlet`; logout lives only inside `DashboardComponent`). This increment introduces one:

- New `core/shell/app-shell.component.ts` (standalone), containing: app/product name or logo placeholder, a nav tab list (currently just "Patients," extensible for future modules' nav entries — Appointments, etc., added by their own increments), and the logout button (moved from `DashboardComponent`, reusing the same `AuthService.logout(true)` call and `InactivityTimerService` wiring already proven in Module 1).
- `AppComponent`'s template changes from a bare `<router-outlet>` to conditionally wrap it with `<app-shell><router-outlet /></app-shell>` (or an `<ng-content>`-based layout) **only when authenticated** — the shell must not render on `login`, where there's no session yet and nothing to navigate to. Implementation approach: the shell component itself checks route/auth state (e.g., subscribes to `Router` events and an `isAuthRoute` allow-list, or simpler — reads `AuthService`'s current-session signal and renders nothing if absent), rather than duplicating route logic in `AppComponent`. Exact mechanism is an implementation detail; the constraint (hidden on unauthenticated routes) is the requirement.
- `DashboardComponent` is trimmed: its own logout button is removed (now redundant with the shell's), but its `ngOnInit` still starts the inactivity timer and calls `authService.me()` for its own greeting content — or, alternatively, the shell takes over the inactivity-timer start since it's now the component present on every authenticated screen, and `DashboardComponent` stops owning that responsibility. **Flagged as an implementation decision for the developer to make consistently, not a BRD-mandated detail** — either placement satisfies the underlying NFR (idle timeout), but it should only live in one place to avoid double timers.
- The "Patients" tab is a simple `routerLink="/patients"` with `routerLinkActive` styling — no dropdown/submenu needed, since there is exactly one target today (per user request, literally "Patient Menu... clicking on Patient Tab").

### 9b.3 Frontend — Patients Grid (`PatientsListComponent`, revised from original "list/search" component), now paginated

- On init, calls `patientService.list({ page: 1 })` (new/renamed method — see §9b.5) with no `query` → renders page 1 of the grid, using the server's default `pageSize`.
- Grid columns: Full Name, Date of Birth (or computed Age — reuse whichever display convention `patient-detail.component` already uses for consistency), Gender, Phone Number, and a final **Edit** column containing an icon/button per row.
- **Pagination controls**: simple **Previous / Next** buttons plus a "Page X of Y" label below the grid, rather than a numbered page-picker — kept deliberately simple since this is a single-clinic, low-volume app where jumping to an arbitrary page number isn't a real need; "Previous" disabled on page 1, "Next" disabled on the last page (derived from the response envelope's `totalCount`/`pageSize`). Page size is **fixed** at the server default (`25`) with no client-facing page-size selector in this increment — another simplicity call consistent with the low-volume framing; flagged as an implementation choice the developer/Product Owner can revisit, not a BRD-mandated control.
- "Add Patient" button placed above the grid (top-left or top-right, developer's call on exact placement — not a BRD-specified detail), `routerLink="/patients/new"`, reusing the existing Increment 1 create flow unchanged.
- Edit icon per row: `routerLink="['/patients', patient.id, 'edit']"`, navigating straight into `patient-form.component` in edit-mode (§9a.6) — **does not** route through the Patient Detail (view) screen first; this is the direct one-click behavior the user asked for and is additive to, not a replacement for, the existing Detail screen's own "Edit" button (3.3/9a.7 flow is unchanged and still reachable via search or direct profile links).
- States: loading (initial fetch, and again on every page-change fetch), loaded (grid populated for the current page), empty (zero patients total → "No patients yet" + Add Patient shortcut, matching 3.4 step 5's convention — distinct from "this page happens to be past the end," which shouldn't occur in normal use since Next is disabled once on the last page), error (fetch failure → inline message + retry).
- Search box: **kept, not removed** — layered above the grid as an optional filter; typing into it (debounced per the existing 3.4 workflow) calls the same `list()` method with `query` set. **Typing into the search box resets pagination to page 1** — standard pattern, since a new filter invalidates whatever page position applied to the previous (unfiltered or differently-filtered) result set; clearing the search box reverts to the full browse-all grid, also reset to page 1. This reconciles the original Increment 3 search-component work with the new browse-all + pagination requirement in one component rather than two competing screens.

### 9b.4 Reconciling the Dashboard entry point (original plan) vs. the new header tab

The original plan's task 20 ("Dashboard integration — add navigation entry point to Patients list/search") is superseded, not deleted: the header nav (§9b.2) is now the primary, always-available entry point per the user's explicit request, satisfying the same underlying need (a way to reach Patient Management from the post-login screen) more generally, since it works from *any* authenticated screen, not just the dashboard. **Resolved (Open Question 8): header tab only** — the dashboard does not get its own "Patients" card/link; the header tab is the sole navigation path into this module.

### 9b.5 Frontend — service/model/route changes

- `patient.service.ts`: rename or extend the existing search-oriented method (whatever Increment 3's original task 19 would have added) into a single `list(options: { query?: string; page?: number; pageSize?: number }): Observable<PagedResult<Patient>>` that calls `GET /api/patients` with `query` (omitted or empty for browse-all, populated for search) plus `page`/`pageSize`. Avoids having two near-identical service methods (`getAll()` and `search()`) calling the same route, and keeps pagination handling in one place for both modes.
- `patients.models.ts`: add a new generic `PagedResult<T>` type (`{ items: T[]; totalCount: number; page: number; pageSize: number }`) matching the backend envelope (§9b.1); `PatientsListComponent` consumes `PagedResult<Patient>` from `list()`. No change to the existing `Patient`/`PatientDto`-shaped type itself, still reused as-is by `getById`.
- `app.routes.ts`: add `{ path: 'patients', canActivate: [authGuard], loadComponent: () => import('./features/patients/list/patients-list.component').then(m => m.PatientsListComponent) }`, grouped with the other `patients/*` routes; no ordering conflict with `patients/new` or `patients/:id` since `patients` (no further segment) is distinct from both.

### 9b.6 New test cases (concrete)

**Unit — `GetAllPatientsQueryHandler` (paginated)**
- Page 1 with `pageSize=25` and 30 seeded patients → returns the first 25, ordered by `FullName` ascending, `totalCount == 30`.
- Page 2 of the same 30-patient set → returns the remaining 5, `totalCount == 30`, `page == 2`.
- A page beyond the total (e.g., page 3 of a 30-row, pageSize-25 set → only 2 pages exist) → returns an empty `items` array, `totalCount` still `30` (not a `404`, not an error).
- Returns an empty `items` array and `totalCount == 0` when no patients exist at all.
- `pageSize` above the server max (e.g., request `pageSize=500`) → clamped to `100`.
- `page=0` or negative `page` → treated as `page=1` (default fallback, not `400`).

**Unit — `SearchPatientsQueryHandler` (paginated, reworked from its existing task-16 unpaged version)**
- A query matching more rows than one page → returns only the current page's matches, `totalCount` reflects the full matching-row count, not just the page.
- Same page-size clamp and page-bounds behavior as the browse-all handler above, confirming both handlers apply pagination identically.

**Integration (`WebApplicationFactory`, extending `PatientsEndpointsTests`)**
- `GET /api/patients` (no `query`) without a token → `401`.
- `GET /api/patients?page=1&pageSize=25` with a token and several created patients → `200` with the envelope shape (`items`, `totalCount`, `page`, `pageSize`), `items.length` matching whichever is smaller of `pageSize` or the seeded count, ordered by name.
- `GET /api/patients?page=2&pageSize=10` with 15 seeded patients → `200` with the remaining 5 in `items`, `totalCount == 15`.
- `GET /api/patients?page=99` with far fewer than 99 pages of data → `200` with an empty `items` array (not `404`), `totalCount` unchanged.
- `GET /api/patients` (no `query`) with zero patients seeded → `200` with `items: []`, `totalCount: 0`.
- `GET /api/patients?query=` (empty string) behaves identically to no `query` param at all, including pagination (regression guard against an off-by-one in the "is query present" branch, §9b.1).
- `GET /api/patients?query={term}&page=2&pageSize=5` with a search term matching more than 5 patients → `200` with page 2 of the matching set, `totalCount` reflecting only matches, confirming search and browse-all share identical paging behavior.
- `GET /api/patients?pageSize=1000` → `200` with `pageSize` echoed back as the clamped `100` (or fewer rows if fewer exist), not `1000`.

**Angular component tests**
- `PatientsListComponent`: renders a grid row per patient from a mocked `list()` response's `items`; "Add Patient" button has `routerLink` to `/patients/new`; each row's edit icon has `routerLink` to `/patients/{id}/edit`; empty state renders with zero total patients and includes an Add Patient shortcut; search box re-invokes `list()` with the typed `query` (debounced) and resets to page 1, and clearing it re-invokes `list()` with no `query`, also reset to page 1.
- `PatientsListComponent` pagination: "Next" button calls `list()` with `page` incremented and renders the new page's `items`; "Previous" is disabled on page 1; "Next" is disabled when `page * pageSize >= totalCount` (last page); "Page X of Y" label reflects `page` and `Math.ceil(totalCount / pageSize)` from the mocked response.
- `app-shell.component`: renders the "Patients" nav tab with a link to `/patients`; renders the logout button and calls `AuthService.logout(true)` on click (moved from the old `dashboard.component.spec.ts` coverage); does not render when there is no authenticated session (e.g., on `/login`).
- `DashboardComponent` regression: existing tests updated to reflect the removed inline logout button (now owned by the shell), without losing coverage of whatever `DashboardComponent` still owns (its own greeting/`me()` call).

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
        GetAllPatientsQuery.cs            # new, Increment 3 revision — paginated browse-all handler (§9b.1)
      Dtos/
        PagedResultDto.cs                 # new, Increment 3 revision — generic { Items, TotalCount, Page, PageSize } envelope (§9b.1)
      Services/
        IPatientRepository.cs             # extended, Increment 2: + UpdateAsync; Increment 3: + paginated GetAllAsync(page, pageSize)
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
      PatientRepository.cs                # extended, Increment 2: + UpdateAsync; Increment 3: + paginated GetAllAsync(page, pageSize)
    Migrations/
      <timestamp>_AddPatientsTable.cs
  PatientManagement.Api/
    Controllers/
      PatientsController.cs               # extended, Increment 2: + GetById, + Update; POST switches to CreatedAtAction
                                           # extended, Increment 3: GetAll branches browse-all vs search on `query` (§9b.1)
  PatientManagement.Tests/
    Unit/Patients/
      CreatePatientCommandTests.cs
      GetPatientByIdQueryHandlerTests.cs   # new, Increment 2
      UpdatePatientCommandTests.cs         # new, Increment 2
      SearchPatientsQueryTests.cs
      GetAllPatientsQueryHandlerTests.cs   # new, Increment 3 revision (§9b.6)
    Integration/Patients/
      PatientsEndpointsTests.cs            # extended, Increment 2: GET/PUT cases added
                                           # extended, Increment 3: browse-all GET cases added (§9b.6)

src/client/src/app/
  core/shell/
    app-shell.component.ts / .html / .scss   # new, Increment 3 revision — header nav + logout (§9b.2)
    app-shell.component.spec.ts               # new, Increment 3 revision
  core/patients/
    patient.service.ts                    # extended, Increment 2: + getById, + update
                                           # extended, Increment 3: paginated list({query, page, pageSize}) GET (§9b.5)
    patients.models.ts                    # extended, Increment 2: + UpdatePatientRequest
                                           # extended, Increment 3: + PagedResult<T> (§9b.5)
  features/patients/
    list/
      patients-list.component.ts / .html / .scss   # revised, Increment 3 — browse-all grid + optional search + Add Patient button + edit icon (§9b.3)
      patients-list.component.spec.ts               # new, Increment 3 (§9b.6)
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
- **Browse-all endpoint (Increment 3 revision)**: `GET /api/patients` (no `query`) returns one bounded page (at most `pageSize`, server-capped to `100`) of patient records per call, not the full table in one response — still behind the same `RequireAuthenticatedUser` policy as every other endpoint, so this introduces no new unauthenticated surface, and pagination further bounds the per-response PII exposure versus an unpaginated full-table return. Acceptable given single-user/single-clinic scope (only the one doctor account can ever call it) and no BRD requirement to restrict bulk-read within an authenticated session; flagged here as a deliberate, scope-consistent decision, not an oversight.
- The new header/shell component must not leak the logout/nav affordance onto unauthenticated routes (`login`) — enforced via the auth-state check in §9b.2, verified by the corresponding component test.

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
- Browse-all grid page load time (`GET /api/patients?page=1&pageSize=25`, no `query`) stays within the same 2–5s target — with pagination now in place (Open Question 7 resolved), this should comfortably hold even at higher volumes than the earlier unpaginated design assumed, since each response is bounded to at most `pageSize` (max `100`) rows regardless of total patient count.

**New — Increment 3 revision (header nav + paginated browse-all grid)**
- Component test: clicking the header "Patients" tab from any authenticated screen navigates to `/patients` and renders the grid (page 1).
- Component test: `PatientsListComponent` grid renders one row per patient for the current page's `items`, in `FullName` order, matching the `GET /api/patients` response envelope.
- Component test: "Add Patient" button navigates to `/patients/new`; edit icon on a row navigates to `/patients/{id}/edit` (not `/patients/{id}` — confirms the direct-to-edit behavior the user asked for).
- Component test: pagination — page 2 renders the correct slice of patients given a mocked multi-page response; an empty page beyond the total renders the grid's empty-row state without erroring; "Next"/"Previous" disabled states match the boundary pages.
- E2E: doctor logs in, clicks "Patients" in the header, sees page 1 of the grid, clicks Add Patient, registers a new patient, returns to the grid (manually or via nav) and sees the new row (on whichever page it sorts to); clicks the edit icon on an existing row and lands directly on the pre-populated edit form; with more than one page of seeded data, clicks "Next" and sees the next page's rows, then "Previous" to return to page 1.

## 13. Acceptance Criteria

- AC1: Doctor can create a new patient with Name, DOB/Age, Gender, and Phone, and the record is immediately retrievable via `GET`. (Modules\02 §10)
- AC2: Doctor can edit any field on an existing patient and the change persists across a subsequent fetch. (Modules\02 §10)
- AC3: Searching by full name, partial name, or phone number returns the correct matching patient(s) and excludes non-matches. (Modules\02 §10)
- AC4: The Patient Profile view displays enough information (name, DOB/age, gender, phone) to positively identify the patient before starting a consultation. (Modules\02 §10)
- AC5: All Patient Management endpoints reject unauthenticated requests with `401`. (BRD Security NFR)
- AC6: No delete/merge functionality is exposed anywhere in the UI or API for this module. (Modules\02 §3, §5)
- AC7: Patient search returns results within the BRD's 2–5 second target under representative load. (BRD Success Criteria)
- AC8: A "Patients" tab is present in a persistent header navigation menu on every authenticated screen and, when clicked, opens a paginated grid showing all patient records (page 1 by default), with Previous/Next controls to reach further pages. (User request, Increment 3 revision; pagination per Open Question 7 resolution)
- AC9: The Patients grid page has an "Add Patient" button that opens the existing create-patient form (`patients/new`). (User request, Increment 3 revision)
- AC10: Each row in the Patients grid has an edit icon/control that, when clicked, opens the existing edit-patient form pre-populated for that patient (`patients/:id/edit`). (User request, Increment 3 revision)
- AC11: The header/nav menu does not render on unauthenticated screens (login). (BRD Security NFR; §9b.2)

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Gender field values/format not specified in BRD | Schema/UI rework if Product Owner wants different picklist or free text later | Flag as open question now; implement as a small constrained picklist (Male/Female/Other) that's easy to extend; confirm with Product Owner before Increment 1 closes |
| Age vs DOB storage ambiguity (BRD says "Age/DOB" without specifying which is canonical) | Downstream modules (History, Prescription header) may need age-at-visit computed differently than assumed | Store DOB as canonical, compute Age on read; confirm this is acceptable, not a silent decision that blocks a BRD-stated need for an "Age" field only |
| Optional Email/Address fields not in BRD minimum set | Scope creep beyond BRD if added without sign-off | Keep them nullable/omit entirely from v1 unless Product Owner explicitly requests; do not surface as required |
| No uniqueness constraint on phone/name could allow accidental true duplicates, but BRD also excludes "merging duplicates" as a concern | Doctor could end up with fragmented records for the same real patient without a repair path | Accept as BRD-aligned (Modules\02 §3 explicitly excludes merge); note to Product Owner as a known Phase 1 limitation, not silently fixed |
| Search performance at scale unverified until real data volumes exist | Could miss the 2–5s NFR if patient volume grows beyond "moderate" | Add indexes proactively (§5); include a performance test in Increment 3; revisit indexing/pagination strategy if volume assumptions change |
| Pagination for a full patient list/browse view not explicitly required by BRD (only "search") | Could build an unnecessary feature, or conversely fail a real doctor workflow if a browse-all view turns out to be needed | Treated as optional in this plan (§6, "Optional" list endpoint); confirm with Product Owner whether a browse-all view is needed or search-only is sufficient |
| PK type consistency with Module 1 entities not yet confirmed from this plan alone | Migration rework if `Patient.Id` type diverges from established convention | Task #21 explicitly requires confirming `User` PK type before writing the `Patient` entity/migration |
| No optimistic concurrency control on `PUT` (Increment 2) | Last-write-wins if the same patient record is somehow edited from two open tabs/sessions | Documented assumption (§9a.5): acceptable for a single-user, single-clinic app per BRD scope; revisit only if Product Owner flags real-world multi-tab conflicts |
| `Result<T>` extended with a new `IsNotFound` state (Increment 2) touches a shared type used by Module 1's Auth handlers | Regression risk if the extension isn't purely additive | §9a.2 specifies the extension as backward-compatible (`IsNotFound` defaults `false` on existing factories); Increment 2 tests should include a quick regression pass on existing Auth unit tests after the change |
| Angular route ordering (`patients/new` vs `patients/:id`) is order-sensitive | `/patients/new` could silently resolve to the detail component with `id: 'new'` if routes are declared/reordered incorrectly | §9a.8 flags this explicitly; add an integration/E2E check that navigating to `/patients/new` still renders the create form, not a "patient not found" state |
| This plan's Increment 3 revision **supersedes** the earlier Open Question 3 answer ("browse-all: not built, search only") without a fresh, explicit Product Owner sign-off cycle before implementation starts | Rework risk if Product Owner actually wanted search-only preserved and the header tab to open a search screen instead of a grid | §4/§9b.1 document the reasoning for treating the user's literal request as superseding; new Open Question 6 (below) asks for explicit confirmation before/alongside Increment 3 build-out — do not treat this plan's reasoning as a substitute for that confirmation |
| Pagination changes the `GET /api/patients` response shape (envelope with `items`/`totalCount`/`page`/`pageSize` instead of a flat array) for a route not yet released to any client | Low risk in practice since this plan's earlier unpaginated version of the route was never implemented (Increment 3 work is still upcoming per the task list), but the search handler (task 16) may already exist unpaginated — its signature/return type must be rewired to the paged shape as part of this change, not left as a second, inconsistent response format | §9b.1 explicitly calls out the `SearchPatientsQueryHandler` rework; ensure any already-written tests for the unpaged search handler are updated in the same change, not left passing against a stale contract |
| Fixed page size with no client-facing page-size selector may not suit every doctor's preference | Minor UX friction if 25 rows per page feels too small/large for a given clinic's typical patient count | §9b.3 flags this as a deliberate simplicity choice for a low-volume single-clinic app; revisit with a page-size selector only if Product Owner requests it post-launch |
| Introducing a new `AppShellComponent` touches `AppComponent` (previously untouched since Module 1) and relocates the logout control out of `DashboardComponent` | Regression risk to existing Module 1 auth/logout tests and the inactivity-timer wiring if the relocation isn't done carefully | §9b.2 flags the single-owner-of-inactivity-timer decision explicitly; existing `dashboard.component.spec.ts` and `app.component.spec.ts` must be updated in the same change, not left stale |

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

## Open Questions — Resolved by Product Owner (Increment 3 Revision)

6. **Does the new browse-all grid + header nav supersede the original Increment 1 decision that Module 2 would be "search-only, no browse-all list"?** **Confirmed: yes.** The browse-all grid supersedes the earlier search-only decision.
7. **Expected patient volume ceiling / pagination**: **Add pagination now.** This reverses this plan's earlier "unpaginated for now" recommendation — the Product Owner explicitly chose the more scalable, paginated option over the leaner unpaginated build. See §9b.1 for the API contract and §9b.3 for the Angular grid's paging controls.
8. **Does the dashboard keep its own "Patients" entry point/card alongside the new header tab, or is the header tab the sole navigation path?** **Confirmed: header tab only** — no separate dashboard card. §9b.4's "optional dashboard card" language is superseded; the dashboard does not get a Patients card in this build.
9. **Grid columns**: **Confirmed** — Full Name, DOB/Age, Gender, Phone, Edit icon, as originally proposed. No additional columns (e.g., no "last visit date," which remains out of reach until Module 6 History exists).
10. **Search box on the grid page**: **Confirmed: keep it** — layered on top of the paginated browse-all list, per the dual-mode UX this plan proposed (§9b.3).

---

## Dependencies Recap (for sequencing awareness)

This module sits second in the fixed build order (Authentication → **Patient Management** → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Modules 3–8 should not begin their own patient-referencing schema work until the `Patients` table (Increment 1, task 1–3) is merged, since they all take a foreign-key dependency on it.
