# Module 5: Prescription & Medication Management — Implementation Plan

## 1. Module Overview

Prescription & Medication Management turns the diagnosis captured in Module 4 (Consultation & Clinical Records, built) into the concrete artifact a patient walks away with: a list of prescribed medicines and a printable prescription document. It is the module the BRD's Problem Statement points at directly — replacing the doctor's handwritten prescription pad — and it closes two explicit BRD Success Criteria items ("smooth generation and printing of prescriptions"). Architecturally it is the thinnest module built so far: it adds one new child entity (`Medication`, many-per-`Visit`) with no independent lifecycle of its own — a medication line item only ever exists as part of a specific visit's prescription, is edited in place while that visit is being recorded/edited, and is never created, listed, or deleted outside that context. There is no separate `Prescription` entity in this plan (see §4) — the `Visit` record built in Module 4 already is the prescription's context; medications simply extend it.

This plan integrates prescription entry directly into the existing Consultation form (`features/consultations/form`) rather than building a new screen, per Modules\05's business rule that a prescription is "always generated in the context of a specific visit/consultation" and Acceptance Criterion 1's "before saving" language (§10). It also adds a new printable prescription view, reachable from a saved visit, that composes patient details, vitals, diagnosis, and medications with a fixed header/footer into a print-ready document.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Add one or more medicines to a visit, each with Name, Dosage, Frequency, Duration, Instructions | BRD `Functional Requirements → Medication / Prescription`; Modules\05 §4 #1, §5 |
| R2 | Doctor can add, edit, and remove medicine entries within a single visit's prescription before saving | Modules\05 §10 AC1 |
| R3 | No BRD-specified cap on the number of medicines — UI must support an arbitrary add/remove list | Modules\05 §5 |
| R4 | Generate a printable prescription combining: clinic/doctor header, patient details, vitals, diagnosis, medications, footer | BRD `Functional Requirements → Medication / Prescription`; Modules\05 §4 #3, §10 AC2 |
| R5 | Header and footer content (clinic name, doctor name/credentials, signature area, basic notes) are fixed/hardcoded for this deployment, with no UI path to edit them | BRD: "Clinic/doctor header and footer details are fixed/hardcoded for this deployment, not editable through the UI"; Modules\05 §5, §10 AC4 |
| R6 | Print output is legible and properly formatted on a standard page size | Modules\05 §10 AC3 |
| R7 | Print rendering must not noticeably delay the 2–3 minute consultation workflow | Modules\05 §11 |
| R8 | A prescription is always generated in the context of a specific visit/consultation, pulling vitals and diagnosis from that same visit (Module 4) | Modules\05 §5 |
| R9 | Prescription/medication data is part of the encrypted-at-rest, daily-backed-up data set | Modules\05 §11; BRD Security/Reliability NFRs (Module 9 owns the mechanics) |
| R10 | Must be logged in (JWT-authenticated) to add, edit, view, or print prescription data | BRD Security NFR; module Dependencies (Auth) |

**Explicitly out of scope for this module** (do not build): drug interaction or dosage validation against a drug database, pharmacy integration / e-prescription transmission, editable header/footer templates or branding customization UI, medication inventory/stock tracking (Modules\05 §3 Out of Scope; BRD Out of Scope list). If asked for any of these, flag the conflict rather than building it.

**Explicit assumptions flagged for Product Owner** (BRD/Modules\05 is silent on these — full detail in §14 Open Questions):
- The BRD names no separate `Prescription` entity — only "medicines" as a per-visit list plus a document composed at print time. This plan therefore does **not** introduce a `Prescription` table; `Medication` rows are FK'd directly to `Visit`, and "the prescription" is a virtual/derived concept (a visit's medication list + its vitals/diagnosis + static header/footer), not a persisted row of its own. Flagged as a data-modeling interpretation, not an explicit instruction.
- No BRD statement on whether medications are saved as part of the same `POST`/`PUT /api/visits` action used by Module 4, or via a separate save step/endpoint. This plan treats medications as fields on the same visit save (single "Save Consultation" action persists vitals + complaints + diagnosis + medications together), matching AC1's "before saving" language and the 2–3 minute workflow target — treated as an assumption pending confirmation.
- No BRD statement on the literal fixed header/footer content (clinic name, doctor name/credentials, signature line, footer notes). This plan uses clearly-labeled placeholder text (e.g., "[Clinic Name]", "Dr. [Doctor Name], [Credentials]") that a developer substitutes with real values supplied by the Product Owner before go-live — never left as a silent guess in the codebase.
- No BRD statement on print mechanism (browser `window.print()` of an HTML view vs. server-generated PDF). This plan defaults to a browser-native print (`window.print()` against a dedicated print-styled HTML view) as the lowest-latency, no-new-dependency option consistent with R7 — flagged as a decision needing Product Owner/architect confirmation, since Module 8 (Data Export) will separately need PDF generation and the two could potentially share a rendering approach if decided together.

## 3. Workflows

### 3.1 Add/Edit/Remove Medicines During a Consultation (embedded in Module 4's Consultation form)
1. Doctor is on the Consultation form (`features/consultations/form`), either creating a new visit or editing a saved one — the same screen and route Module 4 built, extended with a new "Medications" section beneath Diagnosis.
2. The Medications section renders as a dynamic row list, each row with five inputs: Name, Dosage, Frequency, Duration, Instructions, plus a "Remove" action per row, and an "+ Add Medicine" action below the list — no cap on row count (R3).
3. Doctor can add as many rows as needed, remove any row before saving, and edit any field inline — all client-side state, nothing persisted until the visit is saved (R2).
4. On submit, the client includes the current medication list (name/dosage/frequency/duration/instructions per row, empty/incomplete rows dropped or flagged per §4 validation rule) in the same `POST /api/visits` (create) or `PUT /api/visits/{id}` (edit) payload Module 4 already sends.
5. Server validates each medication row (Name is the one required field per row — see §4), replaces the visit's medication set (on edit: diff/replace, not append-only — see §4), and persists atomically with the visit's other fields in a single transaction.
6. API returns the updated `VisitDto` including the persisted `Medications` array; client shows the same inline success confirmation Module 4 already uses.

### 3.2 Generate and Print a Prescription
1. From a saved visit — reachable from the Patient Detail "Consultations" section (Module 4, built) or directly after saving a consultation — the doctor selects a "Print Prescription" action.
2. Client navigates to a dedicated Prescription Print view (`features/prescriptions/print`), passing the visit id.
3. The view calls `GET /api/visits/{id}` (existing Module 4 endpoint, already returns vitals/diagnosis; extended to include `Medications`) and `GET /api/patients/{id}` (existing Module 2 endpoint) to assemble: fixed header, patient details (name, age/DOB, gender, phone), visit date, vitals, diagnosis, full medication list, fixed footer.
4. The view renders as a print-optimized HTML layout (dedicated print stylesheet — see §4) and immediately (or on a one-click "Print" button) invokes the browser's native print dialog (`window.print()`).
5. Doctor prints or saves as PDF via the OS/browser print dialog (browser-native "Save as PDF" satisfies ad-hoc PDF needs without this module building its own PDF generator — Module 8, Data Export, owns any structured export requirement separately).
6. No new data is written by this workflow — printing is a read-and-render action only, keeping it fast (R7) and free of write-path risk.

### 3.3 View Medications Within Consultation History (read path)
1. Patient Detail's "Consultations" section (Module 4) and the visit detail/edit view already show vitals/diagnosis; this module extends that same list-row and edit-form read path to also show a compact medication count/summary (e.g., "3 medicines") so the doctor can see at a glance whether a past visit has a prescription attached, without a separate screen.
2. Full detail remains in the Consultation edit form (§3.1) and the Print view (§3.2); no new standalone "view prescription" screen is introduced beyond print, since Modules\05 names no such functionality distinct from add/edit and printing.

## 4. Architecture Approach

- **Layering**: same Clean Architecture split as Modules 2–4 — no new schema/entity project, extends the existing `Visits` vertical slice rather than adding a sibling one. `Medication` lives in `PatientManagement.Domain\Entities`, mapped via `PatientManagement.Infrastructure\Persistence\Configurations\MedicationConfiguration.cs`, surfaced through `PatientManagement.Application\Visits\` (extended DTOs/commands/handlers, not a new `Prescriptions\` folder), and a small new `PatientManagement.Application\Prescriptions\` slice only for the print-composition query (see below) — kept separate from `Visits\` because "assemble a printable document" is a distinct read concern from "persist a visit," even though both operate on the same table.
- **No separate `Prescription` entity (data-modeling decision)**: per Modules\05 §7, "no separate 'document' entity is required" — the printable document is a render-time composition of `Patient` + `Visit` + `Medication` + static content, not a new table. `Medication` rows carry a direct `VisitId` FK, mirroring how `Medication` sits under `Visit` the same way `Visit` sits under `Patient`/`Appointment` — one more level of the same pattern, not a new pattern.
- **Medications as part of the Visit aggregate, not an independent CRUD resource**: `Medication` has no standalone controller/endpoints (no `POST /api/medications`). It is always written as part of `CreateVisitCommand`/`UpdateVisitCommand` (both extended, not superseded) and always read as part of `VisitDto.Medications`. This matches R2's "within a single visit's prescription before saving" framing — medications are visit-scoped child state, the same relationship Module 4 already established between `Visit` and its parent `Patient`/`Appointment`, one level deeper.
- **Edit semantics — replace-on-save, not append/diff**: `UpdateVisitCommandHandler` replaces the visit's entire `Medications` collection with whatever the client submits on each save (delete-existing-rows-then-insert-submitted-rows within the same transaction as the visit field update), rather than attempting a field-level diff/patch of individual medication rows. Rationale: the client already holds the full authoritative in-memory list (per §3.1, nothing is persisted until save), so replace-on-save is simpler, avoids orphaned-row bugs, and matches the "before saving" framing in AC1 — there is no scenario in this module's scope where a medication row needs to be modified independently of a full visit save.
- **Validation placement**: extends `VisitValidation.cs` (Application layer, same file Module 4 already introduced) with a `ValidateMedications` rule: each submitted row requires a non-blank `Name` (the one field the BRD frames as the identifying element of a "medicine"); Dosage/Frequency/Duration/Instructions are treated as free text with no minimum length, mirroring Module 4's Complaints/Diagnosis precedent (R1 lists all five fields as the defined set, but only Name functions as a meaningful required identifier — a "medicine" with no name isn't a medicine). A row with a blank Name and all other fields blank is silently dropped (treated as an accidental empty row from the add/remove UI, not a validation error) rather than rejecting the whole save — kept consistent with Module 4's "never hard-block a save" posture. Flagged as an interpretation in §14 Open Question 2.
- **No drug database / interaction validation (explicit non-goal)**: `ValidateMedications` performs no lookups, no dosage-range checks, no interaction checks — purely presence/shape validation, per Modules\05's explicit Out of Scope list.
- **Print rendering approach — browser-native, not server-generated PDF**: the Prescription Print view is a dedicated Angular route with a print-only CSS stylesheet (`@media print` rules: hide app-shell chrome/nav, force standard page margins/font sizing, page-break control around the medication table) rendered from data already fetched via existing `GET` endpoints, triggering `window.print()`. Rationale: avoids introducing a server-side PDF rendering dependency (e.g., a headless-browser or PDF library) for a Phase 1 feature the BRD frames simply as "printable" (not explicitly "downloadable PDF"), keeps the print action near-instant (R7), and reuses data already round-tripped for the Consultation/Patient views rather than adding a new heavy endpoint. Flagged as a decision for confirmation (§14 Open Question 4) since Module 8 (Data Export) will independently need PDF export and could inform a shared approach if decided up front.
- **Fixed header/footer as static, developer-configured content**: implemented as a small static Angular constant/config value (not a database row, not an admin-editable setting — R5 explicitly forbids a UI path to edit it) containing placeholder clinic/doctor/footer text, imported by the Prescription Print component. Kept in one clearly-named file (e.g., `prescription-header-footer.config.ts`) so a developer can find and replace the placeholder values in one place before go-live, without scattering hardcoded strings through the component template.
- **Rendering (list/summary integration)**: `VisitDto` (Module 4) gains a `Medications: MedicationDto[]` collection; the existing Patient Detail Consultations list and Consultation form's read/edit paths are extended to show a medication count summary — additive changes to already-built Module 4 components, not a rebuild.
- **Auth**: no new endpoints beyond the already-JWT-protected `Visits`/`Patients` GETs this module reuses; the print view sits behind the existing `authGuard` like every other authenticated route.
- **Transactionality**: `CreateVisitCommandHandler`/`UpdateVisitCommandHandler` persist the visit row and its medication rows in a single EF Core `SaveChangesAsync` call (one DbContext unit of work, same pattern already used for the visit's own scalar fields) — no partial-save risk where vitals persist but medications don't, or vice versa.

## 5. Database Entities

### `Medications` table (new)

| Field | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` (GUID), PK | Matches existing PK convention |
| `VisitId` | `uniqueidentifier`, required, FK → `Visits.Id` | R8 — every medication belongs to exactly one visit; `ON DELETE CASCADE` (unlike `Visits.PatientId`/`AppointmentId`'s restrict pattern) — a medication has no independent existence once its parent visit's medication set is replaced/removed, consistent with §4's "child of the Visit aggregate" decision |
| `Name` | `nvarchar(200)`, required | R1 — the one required, identifying field per row |
| `Dosage` | `nvarchar(100)`, nullable | R1 — free text (e.g., "500mg"), no structured/coded validation (out of scope) |
| `Frequency` | `nvarchar(100)`, nullable | R1 — free text (e.g., "twice daily") |
| `Duration` | `nvarchar(100)`, nullable | R1 — free text (e.g., "5 days") |
| `Instructions` | `nvarchar(500)`, nullable | R1 — free text (e.g., "after food") |
| `SortOrder` | `int`, required | Preserves the doctor's entry order for display/print — rows are meaningfully ordered (medication list on a printed prescription should read in the order entered, not an arbitrary DB order) |
| `CreatedAt` | `datetime2`, required | Audit/history ordering |
| `UpdatedAt` | `datetime2`, required | Bumped on replace-on-save (§4) |

**Indexes**: non-clustered index on `VisitId` (the module's one read/write access pattern — fetch/replace all medications for a visit). **FK relationship**: `Medications.VisitId → Visits.Id`, required, `ON DELETE CASCADE` (medications have no meaning without their parent visit, unlike `Visit`'s own relationship to `Patient`/`Appointment`, which are independently meaningful parents and therefore restrict-on-delete).

No changes to the existing `Visits` table schema (Module 4) beyond the new inverse navigation property (`Visit.Medications`) — no new scalar columns needed since this plan introduces no separate `Prescription` row.

## 6. APIs

No new controller. This module extends the existing `VisitsController` (Module 4) rather than adding a `PrescriptionsController`, consistent with §4's "no independent CRUD resource" decision.

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `POST` | `/api/visits` | *(extended, Module 4 endpoint)* Now additionally accepts a `Medications` array in the request body; persists visit + medications together | Bearer JWT required | `201` + `VisitDto` (now including `Medications`) | `400` invalid payload (e.g., a medication row with no Name and other fields populated — malformed, not silently dropped) |
| `PUT` | `/api/visits/{id}` | *(extended, Module 4 endpoint)* Now additionally accepts a `Medications` array; replaces the visit's medication set per §4's replace-on-save rule | Bearer JWT required | `200` + updated `VisitDto` (including `Medications`) | `400` invalid payload / `404` unknown id |
| `GET` | `/api/visits/{id}` | *(extended, Module 4 endpoint)* `VisitDto` response now includes the `Medications` array, ordered by `SortOrder` — used by both the Consultation edit form and the Prescription Print view | Bearer JWT required | `200` + `VisitDto` | `404` unknown id |
| `GET` | `/api/visits?patientId={id}` | *(extended, Module 4 endpoint)* Patient-scoped visit list now includes a medication count per visit for the Consultations section summary (full `Medications` array not required for this list view — kept lightweight) | Bearer JWT required | `200` + `VisitDto[]` | — (empty array if none) |
| `GET` | `/api/patients/{id}` | *(existing, Module 2 endpoint, unchanged)* Reused by the Prescription Print view to render patient details in the header | Bearer JWT required | `200` + `PatientDto` | `404` unknown id |

All routes remain behind the existing fallback `RequireAuthenticatedUser` policy. `404`/`400` bodies follow the established `{ message: "..." }` convention. No `DELETE`/standalone medication endpoints — matching the "no independent CRUD resource" decision (§4) and the app's existing no-delete-endpoint precedent.

## 7. UI / Screens

- **Consultation form update** (`features/consultations/form`, existing component from Module 4): adds a "Medications" section beneath the existing Diagnosis textarea — dynamic row list (Name/Dosage/Frequency/Duration/Instructions inputs per row), "+ Add Medicine" action, per-row "Remove" action, no row cap (R3). Included in the same form/submit as vitals/complaints/diagnosis — one Save action for the whole consultation including its prescription, consistent with §2's assumption on save semantics. Keyboard-first: tab order flows row-by-row, "+ Add Medicine" reachable without leaving the keyboard, consistent with Module 4's 2–3 minute UX standard.
- **Patient Detail "Consultations" section update** (`features/patients/detail`, existing component from Module 4): each visit row's vitals/diagnosis summary line gains a medication count badge (e.g., "3 medicines") when the visit has any.
- **New: Prescription Print view** (`features/prescriptions/print`): a dedicated, print-styled route (`prescriptions/:visitId/print`) rendering fixed header, patient details, visit date, vitals, diagnosis, full medication list (Name/Dosage/Frequency/Duration/Instructions per row), fixed footer with signature area. A "Print" button triggers `window.print()`; a "Back" link returns to the originating Patient Detail/Consultation screen. No app-shell chrome (nav header) shown in the print stylesheet.
- **Consultation edit form / Patient Detail "Consultations" section**: gains a "Print Prescription" action/link per saved visit, routing to `prescriptions/:visitId/print` — the primary entry point into §3.2's workflow.
- No new App Shell top-level nav tab — prescriptions are only ever reached in the context of a specific visit (from the Consultation form or Patient Detail), consistent with Modules\05 defining no cross-patient "all prescriptions" screen.

## 8. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — JWT/`auth.guard`/`auth.interceptor` protect every route and endpoint this module touches. Patient Management (Module 2, built) — the Print view's patient-details section reuses `GET /api/patients/{id}` and `PatientDto` as-is. Consultation & Clinical Records (Module 4, built) — this module's entire schema/API/UI surface extends Module 4's `Visit` entity, `VisitDto`, `VisitsController`, and Consultation form rather than introducing parallel infrastructure; Module 4's `Visits` table/migration must remain the FK target (already merged).
- **Downstream**: Patient History (Module 6) — past prescriptions/medications are shown as part of a patient's visit history; Module 6 should read `VisitDto.Medications` rather than building a separate medication query. Data Export (Module 8) — a prescription/visit can be exported as PDF/CSV; Module 8 should reuse the same `VisitDto`/`Medications` shape this module establishes rather than re-deriving it, and may revisit the print-mechanism decision (§4, §14 Open Question 4) if a shared PDF-generation approach is adopted.

## 9. Implementation Tasks

**Increment 1 — Medication schema + Consultation form integration (add/edit/remove before save)**
1. Add `Medication` entity to `PatientManagement.Domain\Entities` (fields per §5), plus `Visit.Medications` inverse navigation collection.
2. Add `MedicationConfiguration` (EF Core Fluent API) to `PatientManagement.Infrastructure\Persistence\Configurations`, including the `VisitId` FK (`ON DELETE CASCADE`) and the `VisitId` index; register in `PatientManagementDbContext`.
3. Generate and apply EF Core Code-First migration (`AddMedicationsTable`), following the `AddVisitsTable` migration's pattern.
4. Extend `IVisitRepository`/`VisitRepository` so `GetByIdAsync`/`GetByPatientIdAsync`/`AddAsync`/`UpdateAsync` eager-load (`Include`) `Medications` ordered by `SortOrder`.
5. Add `ValidateMedications` rule to the existing `VisitValidation.cs`: drop fully-blank rows silently; reject a row with a blank Name but other fields populated (`400`, malformed); no length/format/drug-database checks beyond the field-length caps in §5.
6. Extend `CreateVisitCommand`/`CreateVisitCommandHandler` and `UpdateVisitCommand`/`UpdateVisitCommandHandler` to accept a `Medications` list and persist it transactionally with the visit (create: insert; update: delete-existing-then-insert-submitted per §4's replace-on-save rule), assigning `SortOrder` from submission order.
7. Extend `VisitDto`/`VisitMapper` to include a `Medications: MedicationDto[]` collection (`MedicationDto`: Name, Dosage, Frequency, Duration, Instructions), ordered by `SortOrder`; extend `CreateVisitRequestDto`/`UpdateVisitRequestDto` with the same input shape.
8. Extend `GetVisitsByPatientIdQuery`/handler to include a lightweight `MedicationCount` on each list-row DTO (or reuse the full `Medications` array length client-side if simpler — developer's call at implementation time, no new endpoint either way).
9. xUnit unit tests: `CreateVisitCommand`/`UpdateVisitCommand` with zero medications (succeeds, R2 "one or more" is not a hard minimum since a consultation can still have no prescription), one medication, multiple medications; a row with blank Name + populated other fields → `400`/`Failure`; a fully-blank row is silently dropped and does not appear in the persisted result; edit replaces the full medication set (previously-saved rows not resubmitted are removed; newly-submitted rows are added); `SortOrder` persists the submitted row order.
10. xUnit integration tests: `POST`/`PUT /api/visits` end-to-end with a medication list — persisted and immediately retrievable via `GET /api/visits/{id}` with the same field values and order; unauthenticated requests rejected (`401`).
11. Angular: extend `visits.models.ts` (`Medication`, and `medications` on `Visit`/`CreateVisitRequest`/`UpdateVisitRequest`), extend `visit.service.ts` calls to pass the medication array through unchanged (service already forwards the full payload — likely no service-layer code change needed beyond model typing).
12. Angular: extend `consultation-form.component.ts`/`.html` with a `FormArray`-backed Medications section (add row, remove row, five text inputs per row), included in the existing submit payload.
13. Angular unit/component tests: add/remove row updates form state correctly; submit includes all non-blank rows; submit with zero medication rows succeeds (no forced minimum); a fully-blank trailing row is excluded from the submitted payload client-side (defense-in-depth alongside server-side drop).

**Increment 2 — Prescription Print view + entry points**
14. Add a static `prescription-header-footer.config.ts` (or equivalent) under `features/prescriptions/` holding the fixed clinic/doctor header and footer placeholder content (§4) — clearly commented as "replace before go-live."
15. Build `features/prescriptions/print` component: fetches `GET /api/visits/{id}` and `GET /api/patients/{id}`, renders header/patient details/vitals/diagnosis/medication list/footer, "Print" button (`window.print()`), "Back" link.
16. Add a print-only stylesheet (`@media print` rules: hide app-shell nav/header, standard page margins, legible font sizing for the medication table, page-break avoidance around each medication row) satisfying R6.
17. Register `prescriptions/:visitId/print` route (behind `authGuard`) in `app.routes.ts`.
18. Add a "Print Prescription" action to the Consultation edit form and to each visit row in Patient Detail's "Consultations" section, routing to the new print route.
19. Angular component tests: print view renders header/patient/vitals/diagnosis/medications correctly for a visit with multiple medications and for a visit with zero medications (prescription section shows an appropriate empty state, e.g., "No medications prescribed" rather than an empty table); "Print Prescription" links navigate with the correct visit id; header/footer content is not editable anywhere in the UI (no input bound to it).
20. Angular component test: Patient Detail's Consultations section shows the medication count summary per visit row.

**Cross-cutting**
21. Confirm all four §14 open assumptions (no separate `Prescription` entity, single combined save action, literal header/footer content, browser-native print vs. PDF) with Product Owner — items 1–2 before Increment 1 sign-off, items 3–4 before Increment 2 starts (header/footer text is a hard blocker for a real go-live print, even though development can proceed with placeholders).
22. Time a full "consultation + prescription entry + print" pass informally against R7 (no noticeable added delay to the 2–3 minute target) during Increment 2's UX review.

## 10. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Domain/
    Entities/
      Visit.cs                          # extended: Medications navigation collection
      Medication.cs                     # new
  PatientManagement.Application/
    Visits/
      Dtos/
        VisitDto.cs                     # extended: Medications
        CreateVisitRequestDto.cs        # extended: Medications
        UpdateVisitRequestDto.cs        # extended: Medications
        MedicationDto.cs                # new
      Commands/
        CreateVisitCommand.cs           # extended
        UpdateVisitCommand.cs           # extended
      VisitValidation.cs                # extended: ValidateMedications
      VisitMapper.cs                    # extended
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        MedicationConfiguration.cs      # new
    Repositories/
      VisitRepository.cs                # extended: Include(Medications)
    Migrations/
      <timestamp>_AddMedicationsTable.cs
  PatientManagement.Api/
    Controllers/
      VisitsController.cs               # unchanged surface, extended payload
  PatientManagement.Tests/
    Unit/Visits/
      CreateVisitCommandTests.cs        # extended cases
      UpdateVisitCommandTests.cs        # extended cases
    Integration/Visits/
      VisitsEndpointsTests.cs           # extended cases

src/client/src/app/
  core/visits/
    visits.models.ts                    # extended: Medication
    visit.service.ts                    # unchanged/minor typing update
  features/consultations/
    form/
      consultation-form.component.ts / .html / .scss   # extended: Medications FormArray section
      consultation-form.component.spec.ts               # extended
  features/patients/
    detail/
      patient-detail.component.html     # extended: medication count badge, Print Prescription links
  features/prescriptions/
    print/
      prescription-print.component.ts / .html / .scss
      prescription-print.component.spec.ts
      prescription-header-footer.config.ts
```

## 11. Security Considerations

- All extended endpoints (`POST`/`PUT`/`GET /api/visits*`) and the new print route remain behind the existing JWT bearer requirement (`RequireAuthenticatedUser` fallback policy / `authGuard`) — no `[AllowAnonymous]` added (BRD Security NFR), consistent with Modules 2–4.
- Server-side validation on every medication write regardless of client-side checks (`ValidateMedications`) — the server never trusts a client-submitted medication list without shape/presence validation, same posture as every prior module's write path.
- `Medications` rows carry clinically sensitive data (what a patient was prescribed) — same PII sensitivity class as `Visit`'s vitals/complaints/diagnosis (Module 4's §11 precedent applies directly: no denormalized copies, no logging of medication field values).
- The Print view fetches data via the same authenticated `GET` endpoints as every other screen — no separate unauthenticated "shareable print link" is introduced (not requested by BRD/Modules\05; would be a scope addition, flagged rather than built).
- Fixed header/footer content (R5) has no UI input path by design — eliminates any injection surface for that content, since it's a compiled static config value rather than user- or database-supplied.
- Data in transit protected via the app's existing HTTPS enforcement; data at rest encryption and inclusion in the daily backup remain Module 9 concerns — this module's obligation (R9) is to not introduce a separate, unencrypted storage path (e.g., no client-side caching of medication data beyond normal in-memory Angular state, no local export files written outside the browser's own print/save-as-PDF flow).
- All EF Core queries use parameterized LINQ, never raw SQL string concatenation — same injection-avoidance posture as every prior module.

## 12. Test Strategy

**Unit tests (xUnit, Application layer)**
- `CreateVisitCommand`/`UpdateVisitCommand`: succeeds with zero medications; succeeds with one medication (all five fields populated); succeeds with multiple medications; succeeds with a medication row that has only Name populated (Dosage/Frequency/Duration/Instructions blank — R1 doesn't mandate all five per row, only Name as the identifying field per §4); fails (`400`/`Failure`) for a row with blank Name and other fields populated; a fully-blank row is silently dropped from the persisted result, not rejected; `UpdateVisitCommand` replaces the full medication set on each save (previously-saved rows omitted from a resubmission are removed; newly-added rows appear); `SortOrder` is persisted in submission order and round-trips correctly on read.
- `VisitMapper`: `VisitDto.Medications` is correctly populated and ordered from the persisted `Medication` rows.

**Integration tests (xUnit + `WebApplicationFactory`)**
- `POST /api/visits` with a medication list → `201`, and `GET /api/visits/{id}` immediately returns the same medications in the same order.
- `PUT /api/visits/{id}` replacing the medication list (add a new row, remove a previously-saved row, edit an existing row's Dosage) → `200`, and a follow-up `GET /api/visits/{id}` reflects exactly the new set.
- `POST /api/visits` with a malformed medication row (blank Name, populated Dosage) → `400`.
- `POST`/`PUT` without a Bearer token → `401`.
- `GET /api/visits?patientId=` includes a medication count/summary per visit.

**E2E / component-level (Angular)**
- Doctor opens the Consultation form, adds three medicines with all five fields, removes the second one, saves, and the saved visit reflects exactly the remaining two medicines with correct field values.
- Doctor saves a consultation with zero medicines (no prescription needed for this visit) — save succeeds, no forced minimum-one-medicine requirement.
- Doctor opens a previously-saved consultation for edit, removes all medications, and re-saves — the visit now shows zero medications on subsequent read.
- Doctor clicks "Print Prescription" from a saved visit with multiple medications; the Print view renders header, patient details, vitals, diagnosis, and the full medication list correctly, and triggers the browser print dialog on "Print" click.
- Doctor clicks "Print Prescription" on a visit with zero medications; the Print view renders a "No medications prescribed" state rather than an empty/broken table, and the rest of the document (header/patient/vitals/diagnosis/footer) still renders correctly.
- Header/footer text on the Print view matches the fixed configured content exactly and has no editable control anywhere on the page (AC4 verification).
- Doctor times a full "complete consultation with 2–3 medicines → print" pass and it completes with no noticeable added delay versus the Module 4 baseline (informal UX validation of R7, not an automated test).

**Performance**
- `GET /api/visits/{id}` (now eager-loading `Medications`) and the Print view's render stay comfortably within the BRD's general < 2 second Performance NFR for a realistic medication-list size (a handful to a dozen rows) — validate once the `VisitId` index is in place; no dedicated load test given inherently small per-visit medication volume in a single-clinic app.

## 13. Acceptance Criteria

- AC1: Doctor can add, edit, and remove medicine entries (Name, Dosage, Frequency, Duration, Instructions) within a single visit's prescription before saving, with no upper limit on the number of entries. (Modules\05 §10)
- AC2: The generated prescription document correctly includes the fixed header, patient details, vitals, diagnosis, the full medication list, and the fixed footer for the visit it was generated from. (Modules\05 §10)
- AC3: Print output is legible and properly formatted on a standard page size (print stylesheet verified visually and via component test rendering). (Modules\05 §10)
- AC4: Header/footer content matches the fixed clinic/doctor information configured at deployment, with no UI path anywhere in the application to edit it. (Modules\05 §10)
- AC5: A visit can be saved with zero medications (a prescription is not mandatory for every consultation); saving is never blocked by the medication list being empty. (Modules\05 §5 "no limit... on the number of medicines" interpreted alongside Module 4's "never hard-block a save" precedent)
- AC6: Editing a previously-saved visit's medication list fully replaces the persisted set to match what was submitted (additions, edits, and removals all reflected on the next read). (§4 replace-on-save decision)
- AC7: All extended Visit endpoints and the new Prescription Print route reject unauthenticated access. (BRD Security NFR)
- AC8: Printing a prescription introduces no perceptible added delay to the consultation workflow's 2–3 minute target. (Modules\05 §11)

## 14. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Modules\05 does not explicitly state whether a `Prescription` is a separate persisted entity or purely a `Visit` extension | A future module (e.g., Module 6/8) could assume a `Prescriptions` table exists and build against the wrong shape | Modules\05 §7 explicitly says "no separate 'document' entity is required" — this plan follows that literally; documented explicitly (§2, §4) as the interpretation taken; flagged for Module 6/8 planning to reuse `VisitDto.Medications` rather than inventing a parallel query |
| Whether medications save via the same action as vitals/complaints/diagnosis, or a separate step, is not stated in the BRD | If Product Owner expects a distinct "Save Prescription" action/moment, this plan's single-combined-save UX would need rework | Flagged explicitly (§2, §14 Open Question 1) as an assumption grounded in AC1's "before saving" language and the 2–3 minute target; low rework cost if wrong, since it's additive form state, not a schema change |
| Literal fixed header/footer content (clinic name, doctor name/credentials, footer notes/signature area) is not specified anywhere in the BRD/Modules docs | Placeholder text could ship to production unnoticed if not explicitly swapped before go-live | Isolated to one clearly-named, clearly-commented config file (§4, §9 task 14) rather than scattered inline strings; flagged as a hard go-live blocker requiring explicit Product Owner content before Increment 2 is considered done (§9 task 21) |
| Print mechanism choice (browser-native `window.print()` vs. server-generated PDF) is not specified by the BRD, and Module 8 (Data Export) will independently need PDF generation later | Two different, potentially inconsistent rendering approaches could end up built across Modules 5 and 8 | Flagged explicitly (§4, §14 Open Question 4) for Product Owner/architect confirmation before Increment 2; Module 8's planning should explicitly reconsider whether to share this module's print-view rendering or introduce a separate PDF pipeline, rather than silently diverging |
| Replace-on-save semantics for medication edits (delete-then-reinsert every save) could be a measurable amount of churn if a visit's medication list is edited very frequently | Minor: unnecessary row ID churn (rows get new GUIDs on every edit since they're deleted/recreated, not patched) — no functional bug, but loses row-level history/audit granularity if that's ever wanted | Explicitly documented as an accepted trade-off for simplicity (§4) given this module's scope has no stated need for row-level medication audit trail; easy to revisit as a targeted diff-based update if Module 9 (Backup/Reliability) or a future audit requirement calls for finer-grained change tracking |
| `Medications` extends the already-most-sensitive table (`Visits`) in the app, with encryption-at-rest/backup responsibility living in not-yet-built Module 9 | Until Module 9 is implemented, "encrypted at rest" (R9) is not actually enforced at the infrastructure level | Same gap already flagged in Module 4's plan §11/§14; this module adds no new exposure beyond what Module 4 already surfaced — reiterated here so whoever plans/builds Module 9 knows `Medications` is now part of the table set requiring coverage |

---

## Open Questions — Requiring Product Owner Confirmation

1. **Combined vs. separate save action**: this plan saves medications as part of the same Consultation "Save" action used for vitals/complaints/diagnosis (one `POST`/`PUT /api/visits` call). Confirm this matches intent, versus a distinct "Save Prescription" step separate from saving the consultation itself.
2. **Blank-row handling**: this plan silently drops a fully-blank medication row (no fields filled) rather than treating it as an error, and rejects a row with a blank Name but other fields populated. Confirm this handling, versus requiring Name whenever any field in a row is populated (already the plan) or requiring Name unconditionally per non-empty row addition (a UI-level "can't add an empty row" gate instead of a submit-time drop).
3. **Fixed header/footer literal content**: this plan ships with placeholder clinic/doctor/footer text pending real values. Confirm the actual clinic name, doctor name/credentials, and any required footer notes/signature-area text before Increment 2 is considered complete for go-live.
4. **Print mechanism**: this plan defaults to browser-native `window.print()` against a print-styled HTML view (no new PDF-generation dependency). Confirm this is acceptable for Phase 1, or whether a server-generated PDF is preferred/required — and if so, whether that decision should be made jointly with Module 8 (Data Export), which will need PDF generation regardless.
5. **No separate `Prescription` entity**: confirm the interpretation that a "prescription" is a `Visit`'s medication list plus its existing vitals/diagnosis, not a distinct persisted business object with its own identity/lifecycle (e.g., no "void/reissue a prescription" concept is in scope, consistent with Modules\05 §7's "no separate document entity" statement).

---

## Dependencies Recap (for sequencing awareness)

This module sits fifth in the fixed build order (Authentication → Patient Management → Appointment Management → Consultation & Clinical Records → **Prescription & Medication Management** → Patient History → Search & Navigation → Data Export → Data Backup & Reliability → Administration). It takes a direct FK dependency on the `Visits` table (Module 4, merged) and reuses `Patients` (Module 2, merged) read endpoints — no new upstream blockers. Module 6 (Patient History) and Module 8 (Data Export) are the downstream consumers: both should build against `VisitDto.Medications` as the authoritative prescription data shape this module establishes, rather than introducing a parallel `Prescription` query or entity.
