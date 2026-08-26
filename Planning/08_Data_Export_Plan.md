# Module 8: Data Export — Implementation Plan

## 1. Module Overview

Data Export is a terminal, read-only module: it introduces no new clinical data of its own and is depended on by nothing downstream. Its entire job is to let the doctor take a single patient's or a single visit's already-captured data out of the application — as a CSV file or a formatted PDF — on manual, one-at-a-time request, satisfying the BRD's named success criterion "Successful export of data in CSV/PDF format" (`BRD\Doc_BRD_Final.md` — Success Criteria) while deliberately excluding bulk "export all" or scheduled export capability (Modules\08 §3 Out of Scope).

**This is a delta plan, not a from-scratch plan.** Reading the live codebase shows a working, server-generated PDF pipeline already exists for one specific case: Module 5's "Print Prescription" (`GET /api/visits/{id}/prescription/pdf`, `QuestPdfPrescriptionGenerator`, QuestPDF-based). That pipeline is narrowly scoped to *prescription* content only (vitals, diagnosis, medications — deliberately omitting complaints, per its own design) and is presented to the user as "print," not "export." Module 8 needs genuinely new, broader outputs: (1) a full **patient** export (profile, optionally summarized visit history) in both CSV and PDF, and (2) a full **visit** export (vitals, complaints, diagnosis, medications — including complaints, which the prescription PDF omits by design) in both CSV and PDF. No CSV generation exists anywhere in the codebase today; no `Data Export` controller, service, or client feature exists. This plan scopes exactly that net-new work while deliberately reusing what already fits: the same `IPatientRepository`/`IVisitRepository` read paths, the same QuestPDF library/rendering conventions (fixed clinic header/footer look-and-feel, A4 page size) established by Module 5, and the same authenticated-blob-download client pattern established by `PrescriptionService`.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Export a single selected patient's data as CSV | Modules\08 §4 #1; BRD Functional Requirements → Data Export "Export patient or visit data as: CSV, PDF" |
| R2 | Export a single selected patient's data as PDF | Modules\08 §4 #2 |
| R3 | Export a single selected visit's structured data (vitals, complaints, diagnosis, medications) as CSV | Modules\08 §4 #3 |
| R4 | Export a single selected visit's data as a formatted PDF, distinct from (though may share rendering logic with) Module 5's prescription-only print | Modules\08 §4 #4 |
| R5 | Export is always manual, user-triggered, and scoped to exactly one patient or one visit at a time — no multi-select, no "export all" | Modules\08 §5; BRD "Export is manual only, per-patient or per-visit; no bulk 'export all' and no scheduled/automatic export" |
| R6 | No export is ever triggered by a system event (e.g., end of day, on save) — always an explicit doctor action | Modules\08 §5 |
| R7 | Exported files contain only data in scope for that one patient or that one visit — no cross-patient aggregation, no data belonging to other patients/visits | Modules\08 §5 |
| R8 | Export generation must not meaningfully block the UI given Phase 1's small expected data volumes | Modules\08 §11 |
| R9 | Must be authenticated (JWT) to export any data | BRD Security NFR; module Dependencies (Auth) |

**Explicitly out of scope for this module** (do not build; flag rather than implement if requested): bulk "export all patients" functionality, scheduled/automatic/recurring export jobs, any export format other than CSV/PDF (e.g., HL7, FHIR) (Modules\08 §3 Out of Scope; BRD Out of Scope — no analytics/reporting infrastructure implied by bulk export either). If asked for any of these, name the conflict with the BRD/Modules\08 rather than building it.

**Explicit assumption carried from Module 5/6's precedent, reused here**: there is no separate `Prescription` entity in the live schema — a visit's "prescription" is its `Medications` collection on `Visit` (per Module 5's plan §4/§14, reaffirmed by Module 6's plan §2). Modules\08 §7's "reads from `Patient`, `Visit`, and `Prescription`/`Medication` data" is read the same way here: `Visit.Medications` *is* the prescription data for export purposes — no separate query or entity is introduced to satisfy that line literally.

## 3. Delta Analysis — What Already Exists vs. What Module 8 Adds

| Capability | Status today | Action needed |
|---|---|---|
| Read access to a single patient's full profile | **Done.** `GetPatientByIdQueryHandler` / `GET /api/patients/{id}` returns `PatientDto` (name, DOB, age, gender, phone, timestamps). | Reuse as-is as the data source for patient export; no change. |
| Read access to a single patient's full visit history | **Done.** `GetVisitsByPatientIdQueryHandler` / `GET /api/visits?patientId=` (extended by Module 6 with optional `fromDate`/`toDate`) returns ordered `VisitDto[]` with `Medications` eager-loaded. | Reuse as-is for the "patient export optionally includes summarized history" case; no change to the query itself. |
| Read access to a single visit's full detail | **Done.** `GetVisitByIdQueryHandler` / `GET /api/visits/{id}` returns the full `VisitDto` — vitals (value + `NotRecorded` flags), complaints, diagnosis, ordered `Medications`. | Reuse as-is as the data source for visit export; no change. |
| PDF generation infrastructure | **Partially reusable.** `QuestPdfPrescriptionGenerator` (QuestPDF, A4, fixed `PrescriptionDocumentConstants` header/footer) proves out the rendering approach and library choice, but it is hard-wired to a narrow `PrescriptionDocumentDto` (vitals + diagnosis + medications only — **no complaints field**, no patient DOB/age-as-of-record fields beyond what a prescription needs, no "visit export" framing). It cannot be reused unmodified for R2/R4 without either widening its DTO/template or introducing a sibling generator. | Add new, purpose-built PDF generators (`IPatientExportPdfGenerator`, `IVisitExportPdfGenerator`) following the same QuestPDF/A4/fixed-header-footer conventions as `QuestPdfPrescriptionGenerator`, rather than overloading the prescription generator with export-specific fields it was never designed to carry (see §5 rationale). |
| CSV generation infrastructure | **Missing entirely.** No CSV library referenced in any `.csproj` (`PatientManagement.Infrastructure.csproj` lists only EF Core, Identity, QuestPDF, JWT — no CsvHelper or equivalent), no CSV-writing code anywhere in the codebase. | Build from scratch: given the narrow, fixed-shape output required (one patient row / one visit + its medication rows — not arbitrary tabular data), this plan recommends hand-rolled CSV writing (`System.Text` + RFC 4180-style field quoting) over adding a new third-party dependency for a single, simple use case (see §5 Open Question 1). |
| Data Export API surface | **Missing entirely.** No `ExportController`/`DataExportController` exists; no export-related query/command handlers exist. | Build from scratch: new Application-layer queries + a new `DataExportController` (or extend `PatientsController`/`VisitsController` — see §5 Open Question 2). |
| Data Export UI entry points | **Missing entirely.** Neither `patient-detail.component` nor `visit-detail.component` (Module 6) has any "Export" action; only "Print Prescription" exists on both. | Add "Export CSV" / "Export PDF" actions to both the Patient Detail screen (patient-level export) and the Visit Detail screen (visit-level export), following the same authenticated-blob-download pattern as `PrescriptionService.getPrescriptionPdf`. |
| Auth on future export endpoints | **N/A yet, but pattern is settled.** Every existing controller in this codebase relies on the app's fallback `RequireAuthenticatedUser` policy with no `[AllowAnonymous]`. | New export endpoints inherit this automatically by following the same controller convention — no new auth work required, just consistent application of the existing pattern. |

## 4. Workflows

### 4.1 Export a Patient (CSV or PDF)
1. Doctor is on a patient's profile (`/patients/:id`, Modules 2/4/6/7, built).
2. Doctor selects "Export CSV" or "Export PDF" from a new Export action group on the page (§8).
3. Client calls `GET /api/patients/{id}/export/csv` or `GET /api/patients/{id}/export/pdf` (new, §7) as an authenticated blob request — same call shape as `PrescriptionService.getPrescriptionPdf` (Module 5 precedent).
4. Server composes the export from `PatientDto` (profile fields) plus, per Open Question 3, an optional summarized visit history (`VisitDto[]` via the existing patient-scoped visit query) — scoped strictly to that one patient (R7).
5. Server returns the file (`text/csv` or `application/pdf`) with a content-disposition filename (e.g., `patient-{id}-export.csv` / `.pdf`), mirroring the existing prescription download's `File(...)` convention.
6. Client triggers a browser download of the returned blob (same pattern as the existing "Print Prescription" flow, adapted from "open in new tab/print" to "save as file" — see §5).
7. If the patient ID is invalid/not found, the endpoint returns `404` and the UI surfaces an error banner consistent with existing error-handling conventions (Modules 4–7 precedent) rather than a silent failure.

### 4.2 Export a Visit (CSV or PDF)
1. Doctor is on a visit's read-only detail view (`/visits/:id`, Module 6, built) or the Consultations list row on Patient Detail.
2. Doctor selects "Export CSV" or "Export PDF" from a new Export action alongside the existing "Print Prescription"/"Edit"/"Back" actions.
3. Client calls `GET /api/visits/{id}/export/csv` or `GET /api/visits/{id}/export/pdf` (new, §7).
4. Server composes the export from the single `VisitDto` (vitals with `NotRecorded` states rendered exactly as captured, complaints, diagnosis, ordered `Medications`) — scoped strictly to that one visit (R7). Unlike Module 5's prescription PDF, this export explicitly includes **Complaints** (Modules\08 §4 #3 names complaints as part of visit export scope; the prescription PDF omits it by design, and this is a deliberate, documented difference, not an oversight).
5. Server returns the file with a content-disposition filename (e.g., `visit-{id}-export.csv` / `.pdf`).
6. Client triggers a browser download.
7. `404` on unknown visit ID, consistent with `GetVisitByIdQueryHandler`'s existing not-found convention.

## 5. Architecture Approach

- **No new persistent entity, no schema change.** Per Modules\08 §7 and confirmed against the live schema, this module is a pure read/render layer over `Patient`, `Visit`, and `Visit.Medications` — the same tables already read by Modules 2, 4, 5, and 6. No migration is introduced.
- **New, purpose-built PDF generators rather than widening the prescription generator.** `QuestPdfPrescriptionGenerator`/`PrescriptionDocumentDto` are Module 5's, designed narrowly around "what goes on a prescription" (explicitly no complaints field). Retrofitting export requirements (complaints, potentially summarized visit history for the patient case) onto that DTO/generator would conflate two different documents with different audiences (a prescription handed to a pharmacy vs. a full record export for the doctor's/patient's own archive) and risk regressing Module 5's already-shipped, tested behavior. This plan adds two new generators — `IPatientExportPdfGenerator` / `QuestPdfPatientExportGenerator` and `IVisitExportPdfGenerator` / `QuestPdfVisitExportGenerator` — reusing QuestPDF (already a project dependency, no new package) and the same fixed clinic header/footer visual convention (`PrescriptionDocumentConstants`, reused directly — the BRD does not distinguish a different "brand" for exports vs. prescriptions) but with their own DTOs shaped for export content. Rationale: keeps Module 5's tested prescription path completely untouched while giving export the fields it actually needs (§6).
- **Hand-rolled CSV writing, not a new third-party dependency (recommended; flagged as Open Question 1).** The CSV shape required is small and fixed — one row of patient fields, or one visit's fields plus a fixed-shape medication sub-table rendered as repeated/prefixed rows — not general-purpose arbitrary tabular export. Rationale for hand-rolling over adding e.g. CsvHelper: (a) avoids a new package/dependency-review cycle for a single, narrow use case; (b) RFC 4180 field-quoting (wrap in `"..."`, escape embedded `"` as `""`, handle commas/newlines in free-text fields like Complaints/Diagnosis/Instructions) is a small, well-understood, testable amount of code; (c) consistent with this codebase's general pattern of preferring existing/first-party libraries (EF Core, QuestPDF, ASP.NET Identity) over adding new ones without a driving need. **Flagged for Product Owner/architect confirmation** — if broader CSV needs are anticipated later (e.g., richer multi-sheet exports), a library would be the better long-term choice and this is a low-cost pivot point.
- **New `DataExportController`, not an extension of `PatientsController`/`VisitsController` (flagged as Open Question 2, mild preference stated).** Modules 5/6 established a precedent of extending an existing controller for closely related concerns (e.g., prescription PDF added to `VisitsController` rather than a new controller). Data Export spans *two* anchor resources (patient AND visit) and is conceptually one cohesive capability ("give me this record as a file") rather than a visit-specific or patient-specific concern — this plan recommends a single `DataExportController` with routes nested under each resource (`/api/patients/{id}/export/{format}`, `/api/visits/{id}/export/{format}`) so the export capability reads as one coherent module in the codebase (mirroring how Modules\08 itself is one module spanning both resources) rather than being split invisibly across two unrelated controllers. This is a low-stakes structural choice — either resolution satisfies the BRD equally; flagged for confirmation before implementation.
- **Patient export's visit-history inclusion — summarized, not full detail, and off by default via a query flag (flagged as Open Question 3).** Modules\08 §4 #1 says "export a selected patient's profile (and *optionally* summarized history)" — the BRD/Modules text itself treats this as optional, not mandatory. This plan defaults the patient export to profile-only, with an explicit `includeHistory=true` query parameter to append a summarized visit list (date, vitals, diagnosis, medication count — not full complaint/medication-line detail, which is what visit-level export already covers per-visit). Rationale: keeps the default patient export small/fast (R8) and avoids duplicating the full visit-export shape inside every patient export; a doctor wanting full per-visit detail for many visits can export each visit individually (R5 already scopes this to one-at-a-time by design) or opt in via the flag for a lightweight summary. **Flagged for Product Owner confirmation** — the alternative (always include full or always include summarized history) is easy to implement either way once decided.
- **Download mechanism — direct file response + browser-triggered save, not "open in new tab" (Module 5's PDF pattern was to enable printing, not necessarily saving).** Both endpoints set `Content-Disposition` such that the client's blob-handling triggers a save-as-file browser action (via a client-side anchor-download trick, the standard Angular pattern for blob downloads) rather than opening inline — appropriate for "export a file to keep," distinct from Module 5's "print" framing. This is a UI-layer decision, not a new architectural pattern (same authenticated blob-fetch approach as `PrescriptionService`).
- **Validation/scoping enforcement stays server-side.** R7's "no cross-patient aggregation" guarantee is structural — each export query is parameterized by exactly one `patientId` or `visitId`, mirroring every existing single-record read handler in this codebase (`GetPatientByIdQueryHandler`, `GetVisitByIdQueryHandler`) — no new server-side logic is introduced that could accidentally widen scope.
- **No async/background job processing.** Given Phase 1's single-clinic, single-patient/single-visit-at-a-time export scope and small expected payload sizes (one patient's profile + a bounded visit history, or one visit + its medications), synchronous request/response generation is judged sufficient for R8 — consistent with Module 5's precedent of synchronous PDF generation for prescriptions, and Modules\08 §11's own framing that "volumes are expected to be small."

## 6. Database Entities

**No new table or entity.** This module is a pure read layer over `Patients`, `Visits`, and `Medications` (Modules 2/4/5), reusing existing indexes and query patterns (including Module 6's `(PatientId, VisitDate)` composite index, if `includeHistory`/full-history export is invoked). No migration is required.

New DTOs (Application layer, not persisted — listed for completeness since they shape both CSV and PDF rendering):

| DTO | Purpose | Key fields |
|---|---|---|
| `PatientExportDto` | Shapes both CSV and PDF patient export | `PatientId`, `FullName`, `DateOfBirth`, `Age`, `Gender`, `PhoneNumber`, `RegisteredAt` (`CreatedAt`), optional `VisitSummaries: List<VisitSummaryDto>` (populated only when `includeHistory=true`) |
| `VisitSummaryDto` | One row per visit inside a patient export's optional history section | `VisitDate`, `TemperatureDisplay`, `BloodPressureDisplay`, `PulseDisplay` (each pre-resolved to value-or-"Not recorded"), `Diagnosis`, `MedicationCount` |
| `VisitExportDto` | Shapes both CSV and PDF visit export | `VisitId`, `PatientName`, `VisitDate`, vitals (value + `NotRecorded` per field, matching `VisitDto`), `Complaints`, `Diagnosis`, `Medications: List<MedicationExportDto>` |
| `MedicationExportDto` | One row per medication inside a visit export | `Name`, `Dosage`, `Frequency`, `Duration`, `Instructions` |

## 7. APIs

New `DataExportController` (per §5's Open Question 2 recommendation) — routes nested under each anchor resource for discoverability:

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `GET` | `/api/patients/{id}/export/csv?includeHistory={bool}` | Export one patient's profile (optionally + summarized visit history) as CSV | Bearer JWT required | `200`, `text/csv`, `Content-Disposition: attachment; filename="patient-{id}-export.csv"` | `404` unknown `id` |
| `GET` | `/api/patients/{id}/export/pdf?includeHistory={bool}` | Export one patient's profile (optionally + summarized visit history) as PDF | Bearer JWT required | `200`, `application/pdf`, `Content-Disposition: attachment; filename="patient-{id}-export.pdf"` | `404` unknown `id` |
| `GET` | `/api/visits/{id}/export/csv` | Export one visit's vitals, complaints, diagnosis, and medications as CSV | Bearer JWT required | `200`, `text/csv`, `Content-Disposition: attachment; filename="visit-{id}-export.csv"` | `404` unknown `id` |
| `GET` | `/api/visits/{id}/export/pdf` | Export one visit's vitals, complaints, diagnosis, and medications as a formatted PDF | Bearer JWT required | `200`, `application/pdf`, `Content-Disposition: attachment; filename="visit-{id}-export.pdf"` | `404` unknown `id` |

All routes sit behind the app's existing fallback `RequireAuthenticatedUser` policy — no `[AllowAnonymous]` added, consistent with every other controller in this codebase. `404` bodies follow the established `{ message: "..." }` convention used by `PatientsController`/`VisitsController`.

**If Open Question 2 resolves toward extending existing controllers instead of a new `DataExportController`**, the same four routes would move to `PatientsController` (the two `/patients/{id}/export/*` routes) and `VisitsController` (the two `/visits/{id}/export/*` routes) with no change to behavior, request/response shape, or auth — purely a code-organization choice, not a contract change.

## 8. UI / Screens

- **Patient Detail (`features/patients/detail/patient-detail.component`) — new Export action group**: adds "Export CSV" and "Export PDF" buttons (plus, if Open Question 3 resolves to exposing the toggle in the UI, an "Include visit history" checkbox controlling `includeHistory`) near the existing patient-profile header actions. Triggers an authenticated blob download via a new `DataExportService` (client), mirroring `PrescriptionService`'s call pattern. Shows an inline "Preparing export…" state and a `banner-error` on failure, consistent with the existing print-error pattern already on this page and on `visit-detail.component`.
- **Visit Detail (`features/patient-history/visit-detail/visit-detail.component`) — new Export action**: adds "Export CSV" and "Export PDF" buttons alongside the existing "Print Prescription" / "Edit" / "Back" actions (same `.actions` button row, §4.2 workflow). Same preparing/error-state pattern.
- **Consultations list rows (Patient Detail, existing)**: no change — export is reached via the Visit Detail view (one click deeper), not duplicated as a per-row action on the list, keeping the list's action surface consistent with Module 6/7's existing "View"/"Print Prescription" precedent rather than growing it further.
- No new top-level route or nav destination — export remains reachable only in the context of a specific patient or visit, consistent with R5's single-record scoping and the navigation precedent set by Modules 6/7 (nothing in this app surfaces a "Data Export" hub screen, matching the BRD's explicit "no bulk" framing).

## 9. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — JWT/`authGuard`/the existing fallback auth policy protect every new endpoint/route this module adds, no changes needed. Patient Management (Module 2, built) — patient export's data source is the existing `GetPatientByIdQueryHandler`/`PatientDto`, reused as-is. Patient History (Module 6, built) — visit export's data source is the existing `GetVisitByIdQueryHandler`/`VisitDto`; patient export's optional history section reuses the existing patient-scoped visit query (including its `fromDate`/`toDate` capability, though this plan does not expose date-filtering on the export endpoints themselves — see §15 risk). Prescription & Medication Management (Module 5, built) — establishes the QuestPDF/A4/fixed-header-footer rendering convention and the authenticated-blob-download client pattern this plan reuses; its own `QuestPdfPrescriptionGenerator`/`PrescriptionDocumentDto` are read as a reference pattern, not modified.
- **Downstream**: None. Per Modules\08 §8, Data Export is a terminal/output-only module — no other module depends on its output.

## 10. Implementation Tasks

**Increment 1 — Visit Export (CSV + PDF)**
1. Confirm Open Questions 1 and 2 (hand-rolled CSV vs. library; new controller vs. extending existing) with Product Owner/architect before starting — low-risk to proceed with the Application-layer DTOs/handlers regardless of that outcome.
2. Define `VisitExportDto`/`MedicationExportDto` (Application layer) and a `GetVisitExportQueryHandler` that maps an existing `VisitDto` (via `GetVisitByIdQueryHandler`, reused) into the export shape — no new repository method needed.
3. Build a small `ICsvWriter`/`CsvExportBuilder` utility (Infrastructure or Application layer, developer's call) implementing RFC 4180-style field quoting/escaping for the visit export shape (header row + vitals/complaints/diagnosis row + a medications sub-table section).
4. Build `IVisitExportPdfGenerator`/`QuestPdfVisitExportGenerator`, reusing `PrescriptionDocumentConstants` for the fixed header/footer and following `QuestPdfPrescriptionGenerator`'s structural pattern, but rendering **all** `VisitExportDto` fields including Complaints (the deliberate difference from the prescription PDF, §4.2).
5. Add `DataExportController` (or extend `VisitsController`, per Open Question 2's resolution) with `GET /api/visits/{id}/export/csv` and `GET /api/visits/{id}/export/pdf`, returning `404` on unknown visit ID via the same convention as `GetPrescriptionPdf`.
6. xUnit unit tests: CSV writer correctly escapes commas/quotes/newlines in free-text fields (Complaints/Diagnosis/Instructions); visit export DTO mapping correctly renders "Not recorded" vitals states; empty-medications visit produces a CSV/PDF with an explicit "no medications" state, not a broken/empty table (consistent with Module 5's existing precedent).
7. xUnit integration tests: `GET /api/visits/{id}/export/csv` and `/pdf` against a seeded visit → `200` with correct content-type/`Content-Disposition`; unknown visit ID → `404`; unauthenticated request → `401`.

**Increment 2 — Patient Export (CSV + PDF)**
8. Define `PatientExportDto`/`VisitSummaryDto` (Application layer) and a `GetPatientExportQueryHandler` composing `GetPatientByIdQueryHandler`'s `PatientDto` plus, when `includeHistory=true`, a summarized mapping over the existing patient-scoped visit query's `VisitDto[]` (date, vitals-display, diagnosis, medication count only — not full per-medication detail).
9. Extend the CSV writer utility (built in Increment 1) to support the patient export shape (profile row + optional summarized-history rows).
10. Build `IPatientExportPdfGenerator`/`QuestPdfPatientExportGenerator`, reusing the same fixed header/footer convention; renders profile fields always, and the optional summarized history table only when requested.
11. Add `GET /api/patients/{id}/export/csv?includeHistory=` and `/pdf?includeHistory=` to `DataExportController` (or `PatientsController`, per Open Question 2), `404` on unknown patient ID.
12. xUnit unit/integration tests mirroring task 6/7's coverage for the patient case, plus explicit coverage of `includeHistory=true` vs. `false`/omitted (history section present vs. absent) and of a patient with zero visits (`includeHistory=true` → empty history section, not an error).

**Increment 3 — Client UI**
13. Build `DataExportService` (`core/data-export/data-export.service.ts`) with `exportPatientCsv(patientId, includeHistory?)`, `exportPatientPdf(patientId, includeHistory?)`, `exportVisitCsv(visitId)`, `exportVisitPdf(visitId)` — each an authenticated `HttpClient.get(..., { responseType: 'blob' })` call, mirroring `PrescriptionService`'s pattern exactly.
14. Add a small shared client-side "trigger blob download" helper (anchor-element `download` attribute trick) — reusable by both patient and visit export buttons rather than duplicated per component.
15. Extend `patient-detail.component` with the Export action group (§8) — CSV/PDF buttons, optional history-inclusion control (per Open Question 3's resolution), preparing/error states matching the existing print-error pattern.
16. Extend `visit-detail.component` with the Export action group (§8) — CSV/PDF buttons alongside existing actions, same preparing/error-state pattern.
17. Angular component tests: clicking each export button calls the correct `DataExportService` method with correct parameters; a preparing state renders while the request is in flight; a failed request renders the error banner; a successful request triggers the download helper (spy-verified, not an actual filesystem write in tests).

**Cross-cutting**
18. Confirm Open Questions 1–3 are resolved and documented before this module is considered complete.
19. Informally time a full "open patient/visit → click Export → file downloads" pass at realistic Phase 1 data volume, confirming R8 ("should not meaningfully block the UI") — consistent with Modules 6/7's precedent of an informal (not load-tested) performance check given small expected volumes.

## 11. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Application/
    DataExport/
      Dtos/
        PatientExportDto.cs
        VisitSummaryDto.cs
        VisitExportDto.cs
        MedicationExportDto.cs
      Queries/
        GetPatientExportQuery.cs        # composes PatientDto + optional summarized VisitDto[]
        GetVisitExportQuery.cs          # composes VisitDto -> VisitExportDto
      Services/
        ICsvWriter.cs
        IPatientExportPdfGenerator.cs
        IVisitExportPdfGenerator.cs
  PatientManagement.Infrastructure/
    Services/
      CsvExportWriter.cs                # RFC 4180-style hand-rolled CSV writer
      QuestPdfPatientExportGenerator.cs
      QuestPdfVisitExportGenerator.cs
  PatientManagement.Api/
    Controllers/
      DataExportController.cs           # new (or: extend PatientsController.cs / VisitsController.cs, per Open Q2)
  PatientManagement.Tests/
    Unit/DataExport/
      CsvExportWriterTests.cs
      GetPatientExportQueryHandlerTests.cs
      GetVisitExportQueryHandlerTests.cs
      QuestPdfPatientExportGeneratorTests.cs
      QuestPdfVisitExportGeneratorTests.cs
    Integration/DataExport/
      DataExportEndpointsTests.cs

src/client/src/app/
  core/
    data-export/
      data-export.service.ts
      data-export.service.spec.ts
    shared/
      download/
        trigger-download.ts             # small blob-download helper, reused by both export flows
  features/
    patients/
      detail/
        patient-detail.component.html   # extended: Export action group
        patient-detail.component.ts     # extended: export handlers, preparing/error state
        patient-detail.component.spec.ts
    patient-history/
      visit-detail/
        visit-detail.component.html     # extended: Export action group
        visit-detail.component.ts       # extended: export handlers, preparing/error state
        visit-detail.component.spec.ts
```

## 12. Security Considerations

- All four new export endpoints remain behind the app's existing fallback `RequireAuthenticatedUser` policy — no `[AllowAnonymous]` added, consistent with every prior module (BRD Security NFR).
- Each export query is parameterized by exactly one `patientId` or `visitId` and performs no join or aggregation beyond that single record's own data (R7) — no server-side logic path exists that could return another patient's/visit's data in an export response.
- Exported files carry the same PII/clinical sensitivity classification as the source data (Modules 2/4/5's existing posture) — no new logging of exported field values in the new query handlers/generators, consistent with the codebase's established no-PII-logging convention.
- Data in transit is protected by the app's existing HTTPS enforcement; exported files are generated in-memory and streamed directly in the response (matching `QuestPdfPrescriptionGenerator`'s existing `byte[]` return pattern) — no temporary file is written to server-side disk, avoiding an unnecessary at-rest exposure window.
- Client-side, the downloaded file is handed directly to the browser's native download mechanism (no intermediate client-side storage/caching of export contents beyond the transient blob) — consistent with how `PrescriptionService`'s existing PDF blob is already handled.
- Encryption at rest for any server-side persistence (there is none introduced by this module) and backup inclusion remain Module 9 concerns, unaffected by this module.
- New CSV-writing code (whether hand-rolled per Open Question 1, or a library) must not permit CSV injection via free-text fields (Complaints/Diagnosis/Instructions) that begin with `=`, `+`, `-`, or `@` when opened in spreadsheet software — the CSV writer should neutralize (e.g., prefix with a single quote or otherwise escape) such leading characters as a defensive measure, an explicit test case (§13) rather than an assumption.

## 13. Test Strategy

**Unit tests (xUnit, Application/Infrastructure layers)**
- `CsvExportWriter`: correctly quotes/escapes fields containing commas, double quotes, and newlines; neutralizes leading `=`/`+`/`-`/`@` characters in free-text fields (CSV-injection defense, §12); produces a well-formed header row matching field order.
- `GetVisitExportQueryHandler`: maps a `VisitDto` to `VisitExportDto` correctly, including all three "Not recorded" vitals states and an empty-medications case; unknown visit ID returns a not-found result consistent with `GetVisitByIdQueryHandler`'s convention.
- `GetPatientExportQueryHandler`: maps `PatientDto` to `PatientExportDto` correctly with `includeHistory=false`/omitted (no history section) and `includeHistory=true` (history section present, correctly summarized — vitals-display, diagnosis, medication count only, not full per-medication rows); a patient with zero visits and `includeHistory=true` produces an empty (not error) history section; unknown patient ID returns not-found.
- `QuestPdfVisitExportGenerator`/`QuestPdfPatientExportGenerator`: generate non-empty, valid PDF byte arrays for a fully-populated record, a record with all vitals "Not recorded," and (visit) a record with zero medications / (patient) zero-visit history — mirroring `QuestPdfPrescriptionGeneratorTests`' existing test shape.

**Integration tests (xUnit + `WebApplicationFactory`)**
- `GET /api/visits/{id}/export/csv` and `/pdf` against a seeded visit → `200`, correct `Content-Type`/`Content-Disposition`, non-empty body; unknown visit ID → `404`; unauthenticated → `401`.
- `GET /api/patients/{id}/export/csv` and `/pdf` (with and without `includeHistory=true`) against a seeded patient with multiple visits → `200`, correct content-type/disposition, history section present/absent as requested; unknown patient ID → `404`; unauthenticated → `401`.
- Exported CSV for a visit with special-character free text (comma, quote, embedded newline in Complaints) round-trips correctly when parsed back (structural correctness check, not just "non-empty file").

**E2E / component-level (Angular)**
- Doctor opens a visit's detail view, clicks "Export CSV" — a file download is triggered (spy-verified) with the correct filename/service call; same for "Export PDF."
- Doctor opens a patient's profile, clicks "Export PDF" with the history-inclusion control off/on — correct `includeHistory` parameter is passed to `DataExportService`.
- A failed export request (simulated 404/500) renders the existing error-banner pattern rather than a silent failure or unhandled exception.
- No UI path exists anywhere in the app for selecting multiple patients/visits for export, or for scheduling a recurring export (explicit negative-case check against R5/Modules\08 AC4).

**Performance**
- No dedicated load test — Phase 1's single-record-at-a-time export scope and small expected payload sizes (per Modules\08 §11) do not warrant one, consistent with Modules 5/6/7's precedent of skipping load testing for similarly small-volume operations; an informal timing pass (§10 task 19) confirms R8 is met at realistic Phase 1 volume.

## 14. Acceptance Criteria

- AC1: Doctor can export a selected patient's data as a valid, correctly formatted CSV file. (Modules\08 §10)
- AC2: Doctor can export a selected patient's data as a valid, correctly formatted PDF file. (Modules\08 §10)
- AC3: Doctor can export a single visit's data in both CSV and PDF formats, including Complaints (a field the existing Module 5 prescription PDF deliberately omits). (Modules\08 §10, §4 #3)
- AC4: No UI path exists for exporting multiple patients/visits at once or scheduling a recurring export. (Modules\08 §10)
- AC5: Every export endpoint rejects unauthenticated access. (BRD Security NFR, R9)
- AC6: An exported file never contains data belonging to a patient/visit other than the one explicitly requested. (Modules\08 §5, R7)
- AC7: Export generation does not visibly block the UI for realistic Phase 1 data volumes. (Modules\08 §11, R8)

## 15. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Modules\08 §7 names "Prescription" as a data source distinct from "Medication," but no `Prescription` table exists in the live schema (Module 5 built medications directly on `Visit`, reaffirmed by Module 6) | Could cause confusion for a developer expecting a `Prescription` query/entity to exist for export | Explicitly documented (§2) that this module follows Modules 5/6's established interpretation — `Visit.Medications` *is* "the prescription" for export purposes — no separate query/entity introduced |
| Whether CSV generation should hand-roll a small writer vs. add a third-party library (e.g., CsvHelper) is not dictated by the BRD/Modules\08 | Hand-rolling risks subtle RFC 4180 escaping bugs if under-tested; adding a library is unnecessary weight for a narrow, fixed-shape use case if hand-rolling would have sufficed | Flagged as Open Question 1 (§5) with a stated default (hand-rolled) and explicit test coverage (§13) targeting exactly the escaping edge cases that would otherwise be the risk; low-cost to swap in a library later since the `ICsvWriter` interface isolates the implementation |
| Whether export endpoints belong in a new `DataExportController` or should extend `PatientsController`/`VisitsController` (per Modules 5/6's "extend, don't fork" precedent) is a judgment call not settled by the BRD | Low — either resolution is functionally identical; picking the "wrong" one only costs a refactor, not a behavior change | Flagged as Open Question 2 (§5, §7) with a stated recommendation and rationale (export spans two resources and reads as one cohesive capability), pending confirmation before Increment 1 |
| Modules\08 §4 #1 leaves "optionally summarized history" in patient export ambiguous on default-on vs. default-off vs. always-on | Building the wrong default could under- or over-deliver against an unstated expectation, or produce unexpectedly large default exports | Flagged as Open Question 3 (§5) with a stated default (off by default, opt-in via `includeHistory=true`) and rationale (keeps default export small/fast per R8); low-cost to flip the default once confirmed |
| The new visit/patient export PDFs reuse `PrescriptionDocumentConstants` (fixed clinic header/footer) verbatim — if that content is ever changed for prescription-specific reasons only, it would silently also affect export documents' branding | Low — but a real coupling introduced by this plan's reuse choice | Documented explicitly here (§5) as an intentional, low-risk reuse (the BRD does not call for different branding per document type); if this ever needs to diverge, the constant can be split without touching either generator's logic |
| Patient export's optional history section reuses the existing patient-scoped visit query but this plan does not expose `fromDate`/`toDate` filtering on the export endpoints themselves (unlike Module 6's UI) | A doctor wanting a date-bounded patient export (e.g., "last 6 months only") has no way to request that without exporting all history or exporting visits individually | Not scoped by Modules\08's stated functionality list (§4), which describes only "patient profile + optionally summarized history," not date-filtered export; flagged here as a reasonable follow-up enhancement rather than a gap against current requirements — the underlying query already supports `fromDate`/`toDate` if this is later requested, so the extension cost would be small |

---

## Open Questions — Requiring Product Owner / Architect Confirmation

1. **CSV generation approach**: this plan recommends a small, hand-rolled RFC 4180-style CSV writer over adding a third-party library (e.g., CsvHelper), given the narrow and fixed shape of the two export cases. Confirm this approach, or state a preference for a library instead.
2. **Controller placement**: this plan recommends a new, dedicated `DataExportController` (routes nested under `/api/patients/{id}/export/*` and `/api/visits/{id}/export/*`) rather than extending `PatientsController`/`VisitsController` directly, on the grounds that export is one cohesive cross-resource capability. Confirm this approach, or state a preference for extending the existing controllers per Modules 5/6's "extend, don't fork" precedent.
3. **Patient export's history inclusion default**: this plan defaults patient export to profile-only, with visit history included only when an explicit `includeHistory=true` parameter is passed. Confirm this default, or state a preference for always including a summarized history, or never including it (visit-level export only, patient export is profile-only with no flag at all).

---

## Dependencies Recap (for sequencing awareness)

This module sits eighth in the fixed build order (Authentication → Patient Management → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → Patient History → Search & Navigation → **Data Export** → Data Backup & Reliability → Administration). Modules 1, 2, 4, 5, 6, and 7 are already built and merged; this module takes no new upstream dependency beyond what those modules already expose (existing `PatientDto`/`VisitDto` read paths), and — per Modules\08 §8 — nothing downstream depends on this module's output. It is the first module in the build order whose net-new server-side surface (CSV writing, two new PDF generators, a new controller) is not simply an extension of an existing query/handler, since no prior module needed either CSV output or a second PDF template shape.
