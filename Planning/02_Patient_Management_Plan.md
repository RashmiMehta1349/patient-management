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

| Method | Path | Purpose | Auth |
|---|---|---|---|
| `POST` | `/api/patients` | Create (register) a new patient | Bearer JWT required |
| `GET` | `/api/patients/{id}` | Retrieve a single patient's full profile | Bearer JWT required |
| `PUT` | `/api/patients/{id}` | Update an existing patient's details (full payload) | Bearer JWT required |
| `GET` | `/api/patients?query={term}` | Search patients by partial name or phone match | Bearer JWT required |

All routes sit behind the existing fallback `RequireAuthenticatedUser` policy — no controller-level `[AllowAnonymous]` needed. Validation failures return `400` with field-level error detail (matching the `Result`-pattern precedent in `PatientManagement.Application\Common\Result.cs`). Not-found lookups return `404`.

## 7. UI / Screens

- **Patients List / Search screen** (`features/patients/list`): search box (name/phone), results table (name, DOB/age, gender, phone), "Add Patient" button, empty state.
- **Patient Form screen** (`features/patients/form`), reused for both Add and Edit: Full Name, DOB (date picker) or Age, Gender (dropdown), Phone, optional Email/Address fields, Save/Cancel actions, inline validation messages.
- **Patient Profile / Detail screen** (`features/patients/detail`): read-only display of all captured fields, "Edit" action, and placeholder navigation tabs/links for Appointments / Consultations / History (wired up as those modules land — Module 2 only needs to render the anchors, not the content).
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
11. Add `GetPatientByIdQuery` + handler, `GET /api/patients/{id}` endpoint.
12. Add `UpdatePatientCommand` + handler, `PUT /api/patients/{id}` endpoint.
13. Unit/integration tests for get-by-id (found/not-found) and update (valid edit, validation failure, not-found).
14. Angular `features/patients/detail` component; extend `features/patients/form` to support edit-mode (pre-populate + PUT).
15. Component tests for detail view and edit flow.

**Increment 3 — Search**
16. Add `SearchPatientsQuery` + handler (name/phone partial match), `GET /api/patients?query=` endpoint.
17. Add DB indexes on `FullName`/`PhoneNumber` via migration.
18. Unit/integration tests for search (name match, phone match, partial match, no results).
19. Angular `features/patients/list` component with debounced search input, results table, empty state.
20. Dashboard integration — add navigation entry point to Patients list/search.

**Cross-cutting**
21. Confirm PK type/GUID convention against Module 1's `User`/`PasswordResetToken` entities before writing the migration, to keep the schema internally consistent.
22. Resolve Open Questions (Gender picklist values, Email/Address inclusion, pagination) with Product Owner before Increment 1 sign-off if they block the entity shape.

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
        UpdatePatientRequestDto.cs
      Commands/
        CreatePatientCommand.cs
        UpdatePatientCommand.cs
      Queries/
        GetPatientByIdQuery.cs
        SearchPatientsQuery.cs
      Services/
        IPatientRepository.cs
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        PatientConfiguration.cs
    Repositories/
      PatientRepository.cs
    Migrations/
      <timestamp>_AddPatientsTable.cs
  PatientManagement.Api/
    Controllers/
      PatientsController.cs
  PatientManagement.Tests/
    Unit/Patients/
      CreatePatientCommandTests.cs
      UpdatePatientCommandTests.cs
      SearchPatientsQueryTests.cs
    Integration/Patients/
      PatientsEndpointsTests.cs

src/client/src/app/
  core/patients/
    patient.service.ts
    patients.models.ts
  features/patients/
    list/
      patients-list.component.ts / .html / .scss
    form/
      patient-form.component.ts / .html / .scss
    detail/
      patient-detail.component.ts / .html / .scss
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

---

## Open Questions — Resolved by Product Owner

1. Gender field: **fixed picklist (Male/Female/Other)**.
2. Email/Address: **not captured** — strictly the four BRD fields (Name, DOB, Gender, Phone).
3. Browse-all paginated list: **not built in Increment 1** — search only; can be added later if needed.
4. DOB vs Age: **store `DateOfBirth`, compute Age on read**.

---

## Dependencies Recap (for sequencing awareness)

This module sits second in the fixed build order (Authentication → **Patient Management** → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Modules 3–8 should not begin their own patient-referencing schema work until the `Patients` table (Increment 1, task 1–3) is merged, since they all take a foreign-key dependency on it.
