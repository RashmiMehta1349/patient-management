# Module 2: Patient Management — Technical Planning Document

> Source module spec: `Modules\02_Patient_Management.md`
> Source BRD: `BRD\Doc_BRD_Final.md`
> Upstream dependency: Module 1 (Authentication & Authorization) — complete. This plan reuses the architectural baseline established there (Angular + ASP.NET Core/.NET 8 + EF Core + SQL Server, four-project backend layering, `AuthGuard`/`AuthInterceptor`/`JwtSessionMiddleware`, `/api/v1/...` convention) rather than re-deciding it. See `Planning\01_Authentication_and_Authorization_Plan.md` §"Baseline Established for Later Modules."

---

## 0. Assumptions Log (flagged per agent instructions)

The BRD and module spec state the four required fields but leave several implementation mechanics open. These are reasonable, low-risk defaults for Product Owner sign-off, not silent decisions:

| # | Open item | BRD/Module says | Assumption made | Rationale |
|---|---|---|---|---|
| B1 | Age vs. DOB storage | "Age / DOB" listed as one bullet, doesn't say which is captured/stored | **Store `DateOfBirth` (date), compute Age on read** | DOB is the durable fact; age drifts daily. Storing DOB and deriving Age avoids stale data and matches how the printable prescription (Module 5) would show a patient's age at time of visit. Screen should let the doctor type either a DOB or a raw age which is converted to an approximate DOB, since some patients won't know their exact birthdate — this conversion approach is an assumption for PO confirmation. |
| B2 | Gender value set | "Gender" listed with no enumerated options | **Enum: Male / Female / Other**, stored as a constrained string/lookup, not free text | Keeps the field structured/searchable/reportable rather than an unbounded text field; "Other" avoids forcing a binary choice. Confirm the exact label set with Product Owner. |
| B3 | Contact details — which fields exactly | "Contact details" is a single bullet, not itemized | **Phone number (required, primary contact and search key) + optional Address + optional Email** | BRD explicitly names phone as a search key ("search by name or phone number"), so phone must be a distinct, validated field; address/email are reasonable low-risk additions for identifying/contacting a patient but are optional so they don't block the "register in under a minute" user story. |
| B4 | Patient identifier shown to the doctor | Not specified | **System-generated sequential/human-readable Patient ID (e.g., `P-00001`) in addition to the internal DB primary key** | A GUID/int PK is not something a doctor can casually reference; a short human-readable ID improves usability for verbal/paper cross-reference, consistent with the Usability NFR. |
| B5 | Duplicate patient handling | Out of Scope explicitly excludes "merging duplicate records," but doesn't say whether creation should warn on likely duplicates (same name+phone) | **Soft warning (non-blocking) if an exact name+phone match already exists, letting the doctor proceed anyway** | Prevents accidental double-entry without building the excluded merge feature; mirrors the "soft overlap warning, not a hard block" pattern the BRD explicitly uses elsewhere (Appointment Management). Flagged for PO confirmation since it's not stated in scope. |
| B6 | Patient deletion | Module spec explicitly says "no deletion workflow specified" | **No delete capability in Phase 1; edit-only correction of erroneous entries** | Directly stated in the module spec (§5, Business Rules) — not an assumption, restated here so it isn't accidentally built. |
| B7 | Phone number format/validation | Not specified | **Basic format validation (digits, optional country code prefix, min/max length) rather than strict carrier validation** | Avoids over-engineering for a single-clinic, likely-single-country deployment; exact format rules should be confirmed with Product Owner if international patients are expected. |

These items should be confirmed with the Product Owner before or during the sprint that implements this module; none change scope — they fill in mechanics the BRD/module spec left unstated.

---

## 1. Module Overview

Patient Management is the anchor entity of the entire application: every appointment, consultation, prescription, and history record hangs off a `Patient` row created here. This module gives the doctor a fast way to register a new patient (four required fields, sub-minute entry per the BRD's user story), correct their details over time, view their profile as the entry point into clinical workflows, and locate an existing patient by name or phone. It sits immediately behind Authentication in the dependency graph — every screen and endpoint here requires a valid session — and it is the first data-holding module built, so its entity shape and API conventions are what Appointment Management, Consultation & Clinical Records, Prescription & Medication Management, Patient History, Search & Navigation, and Data Export will all reference via foreign key.

---

## 2. Business Requirements

Traced to `Modules\02_Patient_Management.md` and `BRD\Doc_BRD_Final.md`:

- Add, edit, and view patient details (Module spec §4 Functionalities #1–3; BRD Functional Requirements → Patient Management: "Add, edit, and view patient details").
- Capture Name, Age/DOB, Gender, and Contact details as the minimum required fields (Module spec §4 #1, §5 Business Rules; BRD "Capture: Name, Age/DOB, Gender, Contact details").
- Search patients by name or phone number (Module spec §4 #4; BRD "Search patients by name or phone number").
- Patient records persist indefinitely; no deletion workflow (Module spec §5 Business Rules).
- Requires an authenticated session to access (Module spec §8 Dependencies: "Depends on: Authentication & Authorization").
- Patient search/retrieval within the 2–5 second target (Module spec §11; BRD Success Criteria "Patient search and history retrieval within 2–5 seconds").
- Simple, minimal UI for fast entry (Module spec §11; BRD NFR-Usability: "Simple, minimal UI optimized for fast data entry").
- Page load < 2 seconds (BRD NFR-Performance).

**Explicitly out of scope** (must not be planned in): merging duplicate patient records, patient self-service portal/patient-facing login, multi-clinic patient sharing, insurance/billing identifiers (Module spec §3 "Out of Scope"; billing is out of scope entirely per BRD). If a future request asks for any of these, it is a scope change — redirect to brainstorming-agent rather than planning it here.

---

## 3. Workflows

### 3.1 Add (Register) Patient
1. Authenticated doctor navigates to "New Patient" (from dashboard, search results empty-state, or nav shell).
2. Form captures: Name (required), Date of Birth or Age (required, converted per B1), Gender (required, B2 enum), Phone (required, B3/B7), optional Address, optional Email.
3. Client-side validation (required fields present, phone format, DOB not in the future) runs before submit; API tier re-validates the same rules server-side (never trust client-only validation).
4. On submit, business-logic tier checks for a likely duplicate (B5: exact name + phone match); if found, returns a non-blocking warning the UI surfaces as a confirm-to-proceed dialog rather than rejecting the save.
5. On save, a system-generated human-readable Patient ID (B4) is assigned, `CreatedAt`/`UpdatedAt` timestamps set, and the new patient's profile view is shown immediately (Acceptance Criteria: "record is immediately retrievable").

### 3.2 Edit Patient
1. From the patient profile view, doctor selects "Edit."
2. Form pre-populated with current values; same field set and validation rules as Add apply.
3. On save, business-logic tier updates the record and `UpdatedAt`; no re-run of the duplicate check (editing an existing patient isn't a new-duplicate scenario).
4. Profile view reflects updated values immediately.

### 3.3 View Patient
1. Doctor arrives at a patient's profile either from search results, "recent patients" (Search & Navigation module), or directly after registration.
2. Profile view displays all captured fields plus the system-generated Patient ID, and serves as the launch point into "Schedule Appointment," "Start Consultation," and "View History" (cross-navigation to Modules 3, 4, 6) — those actions render as links/buttons here but are implemented by their respective modules.

### 3.4 Search Patient
1. Doctor enters a search term (name fragment or phone fragment) in the patient search field.
2. API performs a case-insensitive partial match against Name and Phone columns, returns matching patients ranked by relevance/recency.
3. Doctor selects a result to open that patient's profile (→ 3.3).
4. No matches: UI offers a direct "Register new patient" action pre-filling the searched term as the Name field, reducing re-typing.

Note: this module implements the underlying search query and endpoint; the broader "quick search with partial match across the app, recent patients, cross-navigation" experience is owned by Module 7 (Search & Navigation), which calls into this module's data per the module spec's Business Rules.

---

## 4. Architecture Approach

Follows the multi-tier baseline established by Module 1 without introducing new tiers or patterns:

- **Presentation tier (Angular):** a `PatientModule` feature area with list/search, add, edit, and profile-view components, sitting inside the shared authenticated shell and behind the existing `AuthGuard`.
- **API tier (ASP.NET Core Web API):** a thin `PatientsController` under `/api/v1/patients`, doing model binding and input-shape validation only; delegates all rules to the business-logic tier, consistent with Module 1's pattern.
- **Business-logic tier:** a `PatientService` (in `PatientMgmt.BusinessLogic`) owning field validation rules, the DOB/age reconciliation (B1), the duplicate-warning check (B5), and Patient ID generation (B4); framework-agnostic and unit-testable in isolation, matching `AuthService`'s design.
- **Data-access tier:** an `IPatientRepository` / `PatientRepository` over EF Core, providing CRUD plus a dedicated `SearchAsync(term)` method that queries Name/Phone with an indexed, case-insensitive partial match (`LIKE` / EF `Contains` translated to a SQL `LIKE '%term%'`, or a full-text/trigram index if search volume later warrants it — flagged as a future optimization, not needed at "moderate patient volume" scale per BRD Scalability NFR).
- **Database tier:** a new `Patients` table in the same SQL Server database as `Users`/`Sessions`, encrypted at rest via the same TDE/disk-encryption mechanism already in place from Module 1.

**Key decisions and rationale:**
- **Validation placement:** input-shape validation (required, format) at the API tier for fast rejection; business rules (duplicate-warning, DOB/age reconciliation, Patient ID assignment) in the business-logic tier so they're unit-testable without HTTP — same split as Module 1.
- **Soft-block duplicate check, not a hard unique constraint:** the module spec explicitly excludes duplicate-merging but doesn't forbid two same-named patients (father/son, common names); a hard DB unique constraint on name+phone would incorrectly block legitimate registrations, so this is enforced as an advisory warning in the service layer, not a database constraint.
- **DOB stored, age derived (B1):** keeps a single source of truth and avoids a nightly job to "age up" stored age values; age is computed at read time in the business-logic/presentation layer.
- **Synchronous CRUD, no async/queue processing:** patient registration/edit/search are simple, low-volume, single-user operations — consistent with Module 1's "no async warranted at this scale" decision.
- **Search indexed at the database tier, not client-side filtering:** with patient volume expected to grow over the life of the deployment, filtering must happen server-side against an indexed column set to keep the 2–5 second Success Criteria met as data grows, rather than shipping the full patient list to the client.
- **Patient ID generation (B4):** generated server-side (business-logic tier) at insert time using a simple sequence/counter approach, not client-supplied, to guarantee uniqueness and monotonic order without a coordination problem (single-writer app, so no distributed-ID complexity needed).

---

## 5. Database Entities

| Table | Field | Type | Notes |
|---|---|---|---|
| **Patients** | `Id` | UNIQUEIDENTIFIER / int, PK | Internal surrogate key, referenced by FK from all other modules' tables. |
| | `PatientCode` | nvarchar(20), unique, not null | Human-readable ID (B4), e.g. `P-00001`; generated server-side, shown in UI. |
| | `FullName` | nvarchar(200), not null | Required field per BRD. |
| | `DateOfBirth` | date, nullable | Nullable to allow "age-only" entry per B1; at least one of `DateOfBirth`/`ApproxAgeAtEntry` must be present — enforced in business-logic tier. |
| | `ApproxAgeAtEntry` | int, nullable | Captured when doctor enters age instead of DOB (B1); paired with `EntryDate` to allow age computation later without pretending to know an exact DOB. |
| | `Gender` | nvarchar(20) / lookup enum, not null | Constrained value set per B2. |
| | `PhoneNumber` | nvarchar(20), not null | Required; primary search key alongside Name (B3/B7). |
| | `Email` | nvarchar(256), nullable | Optional (B3). |
| | `Address` | nvarchar(500), nullable | Optional (B3). |
| | `CreatedAt` | datetime2, not null | Set at registration. |
| | `UpdatedAt` | datetime2, not null | Set at registration, updated on every edit. |

**Indexes:** `Patients.PatientCode` (unique); `Patients.FullName` (non-unique, supports partial-match search — consider a case-insensitive collation); `Patients.PhoneNumber` (non-unique, supports partial-match search); composite/covering index on `(FullName, PhoneNumber)` if query profiling shows the two-column duplicate-check lookup needs it.

**FK relationships:** none inbound (Patients is the root entity for clinical data); outbound references from `Appointments.PatientId`, `Consultations.PatientId`, `Prescriptions.PatientId` (or via Consultation), and any `Patients`-referencing tables in later modules all point to `Patients.Id`, restrict-on-delete (moot in Phase 1 since deletion is out of scope, but set now for referential safety).

---

## 6. APIs

All endpoints under `/api/v1/patients`, reusing Module 1's versioned base-path convention. All endpoints require a valid bearer token (Module 1's `JwtSessionMiddleware`) — there are no public patient endpoints.

| Method | Path | Purpose | Auth required |
|---|---|---|---|
| POST | `/api/v1/patients` | Register a new patient; returns the created record incl. generated `PatientCode` | Yes |
| GET | `/api/v1/patients/{id}` | Retrieve a single patient's full profile | Yes |
| PUT | `/api/v1/patients/{id}` | Update an existing patient's editable fields | Yes |
| GET | `/api/v1/patients?search={term}` | Partial-match search across Name/Phone; returns a ranked result list | Yes |
| GET | `/api/v1/patients/check-duplicate?name={n}&phone={p}` | Advisory pre-save check used by the Add-Patient form to surface the soft duplicate warning (B5) before final submit | Yes |

No request/response schemas are specified here in code form per the planning-agent's constraints — each payload is described in prose in the workflows above (§3). There is intentionally no `DELETE` endpoint (B6/Module spec §5).

---

## 7. UI / Screens

- **Patient Search / List screen** (`/patients`) — search input (name or phone), results list showing PatientCode, Name, Age/Gender, Phone; "Register New Patient" call-to-action, and a "recent patients" section (data supplied by this module, orchestrated by Module 7's navigation logic).
- **Add Patient screen** (`/patients/new`) — form with Name, DOB-or-Age toggle, Gender (dropdown), Phone, optional Email/Address; inline validation; duplicate-warning modal (non-blocking) if `check-duplicate` returns a match; Save/Cancel.
- **Edit Patient screen** (`/patients/{id}/edit`) — same field set as Add, pre-populated; Save/Cancel; no duplicate check re-run.
- **Patient Profile / View screen** (`/patients/{id}`) — read-only display of all fields plus PatientCode, Created/Updated timestamps; action buttons for "Edit," "Schedule Appointment," "Start Consultation," "View History" (cross-navigation entry points into Modules 3/4/6, implemented by those modules).

---

## 8. Dependencies

- **Upstream:** Authentication & Authorization (Module 1) — every screen sits behind `AuthGuard`, every API call behind `JwtSessionMiddleware`; no other upstream dependency (per the dependency graph, Patient Management is the first data-holding module built after Auth).
- **Downstream:** Appointment Management (3), Consultation & Clinical Records (4), Prescription & Medication Management (5), Patient History (6), Search & Navigation (7), Data Export (8) — all reference `Patients.Id` as a foreign key and/or call this module's search endpoint; none of them can be meaningfully built/tested without this module's schema and API being stable first, per the Recommended Development Order.

---

## 9. Implementation Tasks

1. Add `Patient` entity to `PatientMgmt.Domain\Entities` and corresponding request/response contracts to `PatientMgmt.Domain\Contracts`.
2. Create the `Patients` table via an EF Core migration in `PatientMgmt.DataAccess\Migrations`, including the indexes in §5.
3. Implement `IPatientRepository`/`PatientRepository` in `PatientMgmt.DataAccess\Repositories`, including a `SearchAsync(term)` method and a `FindPossibleDuplicateAsync(name, phone)` method.
4. Implement `PatientService` in `PatientMgmt.BusinessLogic` covering: field validation, DOB/age reconciliation (B1), Gender enum validation (B2), Patient ID generation (B4), duplicate-warning logic (B5); unit test against a fake/in-memory repository.
5. Implement `PatientsController` in `PatientMgmt.Api\Controllers` exposing the five endpoints in §6, secured by the existing auth middleware/attribute used by `AuthController`'s protected endpoints.
6. Add DTO validation attributes/FluentValidation rules at the API tier for required fields and formats (phone, email if provided).
7. Build Angular `PatientService` (API client) plus the four screens/components in §7 inside a `features/patients` module, reusing the shared authenticated shell.
8. Implement client-side validation mirroring server-side rules, and the duplicate-warning confirm dialog wired to `check-duplicate`.
9. Add "recent patients" data hook exposed for Module 7's navigation consumption (e.g., a `GET /api/v1/patients?sort=recent&limit=N` or dedicated recent-patients query — coordinate final shape with Module 7 planning).
10. Add integration tests covering create → immediate retrieve, edit → persisted change, search by partial name, search by partial phone, and duplicate-warning trigger.
11. Verify search/retrieval latency against the 2–5 second Success Criteria with a basic timing check as patient volume is seeded (see `PatientMgmt.Seed`).
12. Extend `PatientMgmt.Seed` with sample patient records for downstream module development/testing (Appointment, Consultation, etc. will need seeded patients to build against).

---

## 10. File Structure

Extends the baseline established in Module 1's plan; no new top-level folders introduced.

```
/frontend
│   └── /src/app
│       ├── /core                        (unchanged — shared AuthGuard/Interceptor/shell)
│       └── /features
│           └── /patients
│               ├── patient.service.ts        (API client)
│               ├── patient.models.ts
│               ├── patient-search/            (search/list screen)
│               ├── patient-add/                (add form)
│               ├── patient-edit/               (edit form)
│               └── patient-profile/            (view/profile screen)
│
/backend
├── /PatientMgmt.Api
│   └── /Controllers
│       └── PatientsController.cs
│
├── /PatientMgmt.BusinessLogic
│   ├── /Patients
│   │   └── PatientService.cs
│   └── /Interfaces
│       └── IPatientService.cs
│
├── /PatientMgmt.DataAccess
│   ├── /Repositories
│   │   └── PatientRepository.cs
│   └── /Migrations
│       └── <timestamp>_AddPatients.cs
│
├── /PatientMgmt.Domain
│   ├── /Entities
│   │   └── Patient.cs
│   └── /Contracts
│       ├── CreatePatientRequest.cs
│       ├── UpdatePatientRequest.cs
│       └── PatientResponse.cs
│
├── /PatientMgmt.BusinessLogic.Tests
│   └── PatientServiceTests.cs
│
├── /PatientMgmt.Api.IntegrationTests
│   └── PatientsControllerTests.cs
│
└── /PatientMgmt.Seed
    └── PatientSeedData.cs
```

---

## 11. Security Considerations

- Every patient endpoint requires a valid bearer token/session (BRD Security NFR: "Secure login... single user authentication") — no anonymous access, reusing Module 1's `JwtSessionMiddleware` unmodified.
- Patient data (name, DOB, phone, address) is PHI-adjacent personal data; it inherits the encryption-at-rest coverage already configured at the database level (TDE/disk encryption) and travels only over HTTPS/TLS, same as Module 1's traffic.
- No new attack surface for authentication/authorization is introduced — this module trusts the existing session validation and does not implement its own auth logic.
- Server-side re-validation of all input (never trust client-only checks) prevents malformed or oversized data reaching the database, and mitigates basic injection risk in the search endpoint (parameterized queries via EF Core, no raw SQL concatenation for the `LIKE` search).
- No sensitive-field logging (avoid writing full patient PII to application logs; log Patient IDs, not raw name/phone/DOB, in operational logs).

---

## 12. Test Strategy

**Unit tests (business-logic tier, isolated from EF Core/HTTP):**
- `PatientService`: valid input creates a patient with a generated `PatientCode`; missing required field rejected; DOB-in-future rejected; age-only entry correctly stores `ApproxAgeAtEntry` + `EntryDate` (B1); invalid Gender value rejected (B2); duplicate name+phone triggers a warning result (not an exception/hard failure) (B5); edit updates only permitted fields and refreshes `UpdatedAt`.

**Integration tests (API tier + real test database):**
- POST `/patients` with valid payload → 201 and immediately retrievable via GET `/patients/{id}` (Acceptance Criteria: "record is immediately retrievable").
- POST `/patients` with missing required field → 400 with field-level error.
- PUT `/patients/{id}` with updated phone → subsequent GET reflects the change.
- GET `/patients?search=` with a partial name fragment → returns expected matching patient(s), excludes non-matches.
- GET `/patients?search=` with a partial phone fragment → same, scoped to phone matching.
- GET `/patients/check-duplicate` with an existing name+phone pair → returns warning flag; with a unique pair → no warning.
- All patient endpoints called without a bearer token → 401 (confirms Module 1 auth middleware is correctly applied).

**End-to-end (UI-driven):**
- Register a new patient with all four required fields in under a minute (manual timing check against the user story), land on the new profile.
- Attempt to register with a required field missing → inline validation blocks submit with a clear message.
- Register a likely-duplicate (same name+phone as an existing patient) → warning dialog appears, doctor can confirm-and-proceed or cancel.
- Edit an existing patient's phone number → profile view reflects the new number.
- Search by partial name → correct patient(s) appear in results; select one → lands on that patient's profile.
- Search by partial phone → same behavior scoped to phone.
- Search with no matches → "Register new patient" prompt appears with the search term pre-filled.

**Performance:**
- Patient search and single-patient retrieval complete within the 2–5 second Success Criteria under a representative seeded patient volume (BRD Success Criteria), measured via a basic timing check consistent with Module 1's approach (not full load testing, given single-clinic scale).

---

## 13. Acceptance Criteria

(Restated/extended from Module spec §10, each independently testable)

- Doctor can create a new patient with the four required fields (Name, Age/DOB, Gender, Contact) and the record is immediately retrievable.
- Doctor can edit any field on an existing patient and the changes persist.
- Searching by full or partial name, or by phone number, returns the correct matching patient(s) within the 2–5 second target.
- Patient profile view surfaces enough information (Name, Age/DOB, Gender, Contact, PatientCode) to identify the correct person before starting a consultation.
- Attempting to register a patient with a missing required field is blocked with a clear, field-level message.
- Registering a patient whose name+phone closely matches an existing patient shows a non-blocking warning, and the doctor can still proceed to save (per B5, not a hard duplicate-prevention block).
- No delete action is exposed anywhere in the UI or API for a patient record (Module spec §5).
- All patient screens and endpoints are unreachable without a valid authenticated session.

---

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Age vs. DOB storage approach (B1) not confirmed by Product Owner | Downstream modules (Prescription header, History) may display age inconsistently if the underlying data model changes later | Surface B1 for explicit PO sign-off before this module's schema migration is finalized, since Consultation/Prescription/History all read patient age from this table. |
| Gender value set (B2) and Contact sub-fields (B3) not itemized in BRD | Field set may need to change after downstream modules or a UI review surface a gap (e.g., a needed "Non-binary" option or a second phone number) | Keep `Gender` as a small lookup/enum (not hardcoded UI radio buttons) and `Patients` table additive-friendly (new optional columns) so field-set changes don't require restructuring; confirm with PO before Sprint close. |
| Soft duplicate-warning (B5) is not explicitly requested by BRD | Could be seen as scope creep beyond the stated four functionalities | Explicitly flagged here as an assumption, not a silent addition; implemented as advisory-only (never blocks save) so it adds safety without adding a new blocking workflow; confirm acceptability with PO. |
| Partial-match search performance degrading as patient volume grows | Risk to the 2–5 second Success Criteria and to Search & Navigation's "fast" search requirement | Index `FullName`/`PhoneNumber` from day one; monitor query timing during seeded-data testing; escalate to full-text/trigram indexing only if profiling shows the simple `LIKE` index is insufficient at real volumes — avoid over-building for a scale that may never be reached (BRD Scalability NFR: "moderate patient volume"). |
| No delete/merge capability means data-entry errors (wrong patient created) can only be corrected via edit, not removed | A clearly mis-created patient record persists forever, cluttering search results | Explicitly restated as intentional per Module spec §5/§3 Out of Scope; if this becomes a real operational pain point, it's a scope change for the brainstorming-agent to evaluate, not something to quietly add here. |
| PatientCode generation (B4) approach (simple counter) could collide under concurrent writes in a future multi-instance deployment | Duplicate/failed PatientCode assignment | Given the BRD's explicit single-user, single-clinic, single-instance scope, a simple sequence is sufficient; flag as a design note if the deployment model ever changes to multi-instance/scaled hosting. |

---

## Module Dependency Flow (scoped to Modules 1–2)

```
Authentication & Authorization (1)
        │
        ▼
 Patient Management (2)
        │
        ▼
 [Appointment Management (3), Consultation & Clinical Records (4),
  Prescription & Medication Management (5), Patient History (6),
  Search & Navigation (7), Data Export (8)]  — all depend on Module 2's
  schema/API being stable before their own planning/build proceeds.
```

## Recommended Development Order (scoped to Modules 1–2)

1. **Authentication & Authorization (1)** — complete; hard prerequisite, already delivering `AuthGuard`/`AuthInterceptor`/`JwtSessionMiddleware` this module reuses.
2. **Patient Management (2)** — build next; its entity shape (`Patients` table, `PatientCode`, search endpoint) must be stable before Appointment Management, Consultation & Clinical Records, and Search & Navigation can be meaningfully planned in detail, since all three reference or query patient data directly.
