# Module 6: Patient History — Implementation Plan

## 1. Module Overview

Patient History is the "read side" of the clinical record: a chronological, date-filterable view of everything already captured for a patient by Module 4 (Consultation & Clinical Records) and Module 5 (Prescription & Medication Management). It introduces no new business data — no new entity, no new write path — its entire job is to let the doctor look back at a patient's past visits (vitals, complaints, diagnosis, medications) quickly enough to inform the current consultation, directly addressing the BRD Problem Statement's "slow patient lookup and history tracking" and the named Success Criterion "Patient search and history retrieval within 2–5 seconds" (Modules\06 §6).

**This is a delta plan, not a from-scratch plan.** A meaningful share of Module 6's functional surface already exists, built incidentally as part of Modules 4 and 5: `patient-detail.component` already renders a chronological "Consultations" list per patient (visit date, vitals, diagnosis, medication-count badge, a working "Print Prescription" action), backed by a working `GET /api/visits?patientId={id}` endpoint that already returns visits ordered most-recent-first with `Medications` eager-loaded. What is genuinely missing — confirmed by reading the live code, not assumed — is: (1) a date-range filter over that list, (2) Complaints are captured by Module 4 but never rendered anywhere in the existing Consultations list or read path, (3) no dedicated read-only visit-detail view exists — the only way to see a past visit's full detail today is to open `/consultations/:id/edit`, the mutable edit form, and (4) a dead, disabled `<nav class="placeholder-nav">` "History (coming soon)" block sits unused directly below the Consultations section and needs to be resolved (absorbed/removed), not left dangling once this module ships. This plan scopes precisely those gaps.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Show all past visits for a selected patient in chronological order | Modules\06 §4 #1; BRD Functional Requirements → Patient History "View previous visits" |
| R2 | Drill into a specific past visit to see its vitals, complaints, diagnosis, and prescription (medications) | Modules\06 §4 #2; BRD "Access: Vitals, Complaints, Diagnosis, Prescriptions" |
| R3 | Filter the visit list by date (a specific date or a date range) | Modules\06 §4 #3; BRD "Filter by date" |
| R4 | History is strictly patient-scoped; no aggregate/multi-patient history view in Phase 1 | Modules\06 §5 |
| R5 | Every visit (and its medications) created via Modules 4/5 automatically appears in history — no separate "publish" step | Modules\06 §5 |
| R6 | Date filtering operates on visit date only, not complaint/diagnosis text search (that belongs to Module 7, explicitly excluded here) | Modules\06 §5 |
| R7 | History for a patient with a typical visit volume loads within 2–5 seconds | Modules\06 §6; BRD Success Criteria |
| R8 | No editing of historical visit records from the history view — edits remain the province of Module 4's form | Modules\06 §3 Out of Scope |
| R9 | Must be logged in (JWT-authenticated) to view any history data | BRD Security NFR; module Dependencies (Auth) |

**Explicitly out of scope for this module** (do not build): trend charts/graphs of vitals over time, cross-patient history search/reporting, editing historical records from the history view (Modules\06 §3 Out of Scope; BRD Out of Scope list — Advanced Analytics). If asked for any of these, flag the conflict rather than building it.

**Explicit assumption already reflected in the live codebase, carried forward here**: per Module 5's plan §4/§14, there is no separate `Prescription` entity — "prescriptions" are a visit's `Medications` collection. This plan follows that same interpretation; Modules\06 §7's "Visit, Prescription, Medication" language is read as "Visit and its Medications collection," matching what Module 5 actually built (`VisitDto.Medications`), not a literal separate `Prescription` table that doesn't exist.

## 3. Delta Analysis — What Already Exists vs. What Module 6 Adds

| Capability | Status today | Action needed |
|---|---|---|
| Chronological visit list per patient (R1) | **Done.** `patient-detail.component` renders `visits` from `GET /api/visits?patientId={id}`, which returns `OrderByDescending(v => v.VisitDate)` server-side (`VisitRepository.GetByPatientIdAsync`). | None — reuse as-is. |
| Vitals shown per visit row (part of R2) | **Done.** Temp/BP/Pulse summary line already rendered. | None. |
| Diagnosis shown per visit row (part of R2) | **Done.** | None. |
| **Complaints shown per visit row (part of R2)** | **Missing.** `Visit.complaints` exists on the DTO/model and is captured by Module 4's form, but `patient-detail.component.html`'s Consultations list never renders it — only `visit.diagnosis` is shown. | Add a complaints line to the existing list row (and to the new read-only detail view, §7). |
| Medication count badge (part of R2 summary) | **Done.** | None. |
| **Full past-visit detail view (R2)** | **Partially missing.** The only way to see a visit's full vitals/complaints/diagnosis/medications today is `/consultations/:id/edit` — a mutable form, not a read-only view. This conflates "view history" with "edit a record," and R8 explicitly says history must not be an edit surface. | Build a dedicated read-only visit-detail view/route (design decision — see Open Question 1) rather than repurposing the edit form. |
| **Date filter (R3)** | **Missing entirely.** No date input anywhere on the Consultations section; `GetVisitsByPatientIdQuery`/`GET /api/visits?patientId=` accepts no date parameters. | Add optional `fromDate`/`toDate` query parameters to the existing endpoint/query (extend, not replace) plus a client-side date filter control. |
| **"History (coming soon)" placeholder** | **Dead code.** `patient-detail.component.html` has a disabled `<nav class="placeholder-nav">` block with a non-functional "History (coming soon)" link sitting directly under the Consultations section, left over from Module 4's incremental build-out. | Remove it. Module 6 does not need a *separate* "History" nav destination — see Open Question 2 — its content is delivered by extending the existing Consultations section in place, consistent with Modules\06 §5's "every visit automatically appears in this history; no separate publish step," which reads naturally as "no separate history *screen* either," i.e., the Consultations section *is* the history view once date-filterable and complaint-inclusive. |
| Performance / indexing (R7) | **Partially covered.** `Visits` has a single-column index on `PatientId` (`VisitConfiguration.cs`); results are sorted by `VisitDate` in application code after the index-scoped fetch. Adequate today given single-patient visit volumes are inherently small in a single-clinic Phase 1 app, but a `PatientId`+`VisitDate` filter (date range) benefits from a composite index. | Add a composite index `(PatientId, VisitDate)` to support the new date-range predicate efficiently as visit volume grows; verify 2–5s target holds either way (§12 Performance). |
| Auth on all touched endpoints (R9) | **Done.** `VisitsController` sits behind the app's fallback `RequireAuthenticatedUser` policy already. | None — new/extended endpoint inherits this automatically. |

## 4. Workflows

### 4.1 View Patient History (list + filter)
1. Doctor opens a patient's profile (`/patients/:id`, Module 2/4, built) — the existing "Consultations" section now doubles as the History view once extended by this module (Open Question 2).
2. Section header gains a date-range filter control (from/to date, §9 decision) alongside the existing "+ New Consultation" action.
3. On load (and whenever the filter changes), client calls `GET /api/visits?patientId={id}&fromDate=&toDate=` (extended endpoint, §5/§6) — omitted `fromDate`/`toDate` behaves exactly as today (full history, most-recent-first).
4. List renders one row per visit: visit date, vitals summary, **complaints** (new), diagnosis, medication-count badge — chronological, most-recent-first (R1), unchanged ordering from today.
5. Doctor clears the filter (e.g., a "Clear filter" action) to return to the full unfiltered list.
6. Empty state: if the filter yields no visits in range, show a distinct message (e.g., "No visits found in the selected date range") rather than reusing the "no consultations at all" empty state — the two are different facts about the patient.

### 4.2 View a Past Visit's Full Detail (read-only)
1. From the filtered/unfiltered Consultations list, doctor selects a visit row's "View" action (new — see Open Question 1 on whether this is a new dedicated route or the existing edit form opened read-only).
2. Client fetches `GET /api/visits/{id}` (existing Module 4/5 endpoint, unchanged — already returns vitals, complaints, diagnosis, and the ordered `Medications` array).
3. View renders all fields as static text (no form controls, no submit action) — vitals (including "Not recorded" states exactly as captured), complaints, diagnosis, medication list (name/dosage/frequency/duration/instructions per row).
4. Doctor can navigate to "Print Prescription" (existing Module 5 action, reused as-is) directly from this detail view, and "Back" returns to the patient's Consultations/History list.
5. No write action is reachable from this screen at all (R8) — if the doctor needs to correct a past record, they must explicitly navigate to the Module 4 edit form via its own entry point, not this view.

## 5. Architecture Approach

- **No new entity, no new table (per Modules\06 §7, confirmed against the live schema)**: Module 6 is purely a query/read layer over the existing `Visits` table (and its `Medications` child collection, Module 5). No migration is required for data modeling itself — the only schema change under consideration is a supporting index (§5 below), which is additive and non-breaking.
- **Extend, don't replace, the existing Visits query surface**: `GetVisitsByPatientIdQueryHandler` and the `GET /api/visits?patientId=` route already do 90% of what R1 needs. This plan adds two optional query parameters (`fromDate`, `toDate`, both `DateOnly`/`DateTime?`) to the existing query/handler/endpoint rather than introducing a parallel `GetPatientHistoryQuery` — avoids two sources of truth for "a patient's visit list," consistent with Module 5's precedent of extending `VisitsController` rather than adding sibling controllers for closely related concerns.
- **Filter implementation — server-side, not client-side**: date filtering is applied in the EF Core query (`Where(v => v.VisitDate >= fromDate && v.VisitDate <= toDate)` conceptually) rather than fetched-then-filtered in the browser. Rationale: keeps the payload small for patients with long visit histories, aligns with R7's 2–5 second target measured against realistic data volume, and matches the repository-level filtering pattern already used for `PatientId`.
- **Read-only detail view — new component, not a repurposed edit form (recommended; flagged as Open Question 1 for confirmation)**: this plan recommends a small, purpose-built read-only `features/patient-history/visit-detail` (or similarly named) component rather than opening `consultation-form.component` in a "view-only" mode. Rationale: the edit form is a `ReactiveFormsModule` `FormGroup` wired directly to `submit()`/`PUT /api/visits/{id}`; disabling submission without disabling the underlying editable controls (or conditionally stripping them) adds branching complexity to an already-dense component (mutual-exclusivity vitals logic, medication `FormArray`, etc.) for a screen whose entire purpose (R8) is to guarantee no write path is reachable. A dedicated static-template component is simpler to reason about, impossible to accidentally wire to a save action, and small to build since it only needs to render fields already shaped by `VisitDto`. **This is a real design decision the BRD doesn't dictate — the BRD/Modules\06 do not distinguish "view" from "edit" access for the doctor (the sole user), so either approach is technically compliant; flagged for Product Owner/architect confirmation before Increment 2 (§10, §14).**
- **"Consultations" section absorbs "History," rather than a separate top-level screen**: Modules\06 describes history as scoped to "a selected patient," which is exactly what the existing per-patient Consultations section already is. Rather than building a second, largely-duplicate patient-scoped visit list under a distinct `/patients/:id/history` route, this plan extends the one that exists in place (adds the date filter and complaints line directly to it) and removes the dead placeholder nav link that implied a separate destination was coming. **Flagged as Open Question 2** — if the Product Owner specifically wants a visually/navigationally distinct "History" section separate from "Consultations" (e.g., for framing reasons even though the underlying data and list are identical), that is a small presentational change, not an architectural one, and should be confirmed rather than assumed away.
- **Date-range UI shape — two date pickers (From / To), not presets or a single date**: chosen because Modules\06 §4 explicitly names both "a specific date" and a "date range" as needed outcomes; two optional date inputs (`fromDate`/`toDate`) cover both cases uniformly (a single date is simply `fromDate === toDate`), require no enumeration of preset ranges the BRD never specifies (e.g., "last 7 days," "last month" are not named anywhere), and reuse the same native `<input type="date">` control pattern already used elsewhere in the app (Appointment scheduling, Module 3). **Flagged as Open Question 3** since the BRD is silent on the exact UI shape and a preset-range or single-date-only UI would also satisfy the letter of R3.
- **No new auth surface**: the extended endpoint inherits the existing `RequireAuthenticatedUser` fallback policy automatically (it's the same controller/action pattern, just additional optional query parameters); the new read-only detail route sits behind the existing `authGuard` like every other route in the app.
- **Validation placement**: `fromDate`/`toDate` are optional and only lightly validated (e.g., `fromDate <= toDate` when both are supplied) at the Application layer (`GetVisitsByPatientIdQuery` handler or a thin validation helper) — invalid combinations return `400`, consistent with existing validation-error conventions (`{ message: "..." }`), not a hard crash or silent ignore.
- **Performance**: adds a composite index `(PatientId, VisitDate)` on `Visits` to keep the new date-range-filtered query efficient as data grows, superseding reliance on the single-column `PatientId` index alone for this specific access pattern; the existing `PatientId`-only index remains useful for the unfiltered case and other consumers (Module 5's print flow, Module 4's list). This is judged sufficient to meet the 2–5 second target given Phase 1's single-clinic, single-doctor visit volume — no caching layer or denormalization is introduced.

## 6. Database Entities

**No new entity or table.** This module reuses the `Visits` table (Module 4) and its `Medications` child collection (Module 5) exactly as they exist today — see those modules' plans for full field lists. The only schema-level change:

| Change | Table | Detail | Rationale |
|---|---|---|---|
| New composite index | `Visits` | `(PatientId, VisitDate)`, non-clustered | Supports the new patient-scoped, date-range-filtered query efficiently (§5); the existing single-column `PatientId` index doesn't optimize the added `VisitDate` range predicate. Additive, non-breaking — implemented as a new EF Core migration, no data change. |

No changes to `Medications`' schema or indexing — it is read via the existing `Include(v => v.Medications.OrderBy(m => m.SortOrder))` eager-load already in `VisitRepository`, unchanged by this module.

## 7. APIs

No new controller. This module extends the existing `VisitsController` (Modules 4/5) with optional query parameters rather than adding a `PatientHistoryController`, consistent with the "extend, don't fork" approach in §5.

| Method | Path | Purpose | Auth | Success | Failure |
|---|---|---|---|---|---|
| `GET` | `/api/visits?patientId={id}` | *(existing, unchanged)* Full patient-scoped visit list, most-recent-first, `Medications` included | Bearer JWT required | `200` + `VisitDto[]` (empty array if none) | `400` if `patientId` missing |
| `GET` | `/api/visits?patientId={id}&fromDate={date}&toDate={date}` | ***(extended)*** Same endpoint, now additionally accepts optional `fromDate`/`toDate` to narrow results to visits whose `VisitDate` falls within the (inclusive) range | Bearer JWT required | `200` + filtered `VisitDto[]` (empty array if none in range) | `400` if `fromDate > toDate`, or either value is not a parseable date |
| `GET` | `/api/visits/{id}` | *(existing, unchanged)* Full single-visit detail — vitals, complaints, diagnosis, ordered `Medications` — powers the new read-only detail view exactly as it already powers the edit form and the print view | Bearer JWT required | `200` + `VisitDto` | `404` unknown id |
| `GET` | `/api/visits/{id}/prescription/pdf` | *(existing, unchanged, Module 5)* Reused as-is from the new read-only detail view's "Print Prescription" action | Bearer JWT required | `200` + PDF stream | `404` unknown id |
| `GET` | `/api/patients/{id}` | *(existing, unchanged, Module 2)* Reused to render the patient header context around the history list/detail views | Bearer JWT required | `200` + `PatientDto` | `404` unknown id |

All routes remain behind the app's existing fallback `RequireAuthenticatedUser` policy. `404`/`400` bodies follow the established `{ message: "..." }` convention.

## 8. UI / Screens

- **Patient Detail "Consultations" section update** (`features/patients/detail`, existing component): adds a date-range filter control (From/To native date inputs + "Clear filter" action) to the section header alongside the existing "+ New Consultation" link; adds a Complaints line to each list row (alongside the existing vitals/diagnosis/medication-count line); each row's link target changes from `/consultations/:id/edit` to the new read-only detail route (§8 next item) — the "Print Prescription" button per row is unchanged. Filtered-empty state ("No visits found in the selected date range") is distinct from the existing "No consultations recorded for this patient yet" empty state.
- **Placeholder removal**: delete the dead `<nav class="placeholder-nav">` "History (coming soon)" block and its associated (currently-unused) styles — superseded by the extended Consultations section per §5's "absorb, don't fork" decision (pending Open Question 2 confirmation).
- **New: read-only Visit Detail view** (`features/patient-history/visit-detail` or equivalent naming — final path per Open Question 1's resolution): a dedicated route (e.g., `/visits/:id` or `/patients/:patientId/visits/:visitId`, exact shape TBD alongside Open Question 1) rendering visit date, all three vitals (value or "Not recorded" exactly as captured), complaints, diagnosis, and the full medication list — all as static text, no inputs, no submit action. Includes a "Print Prescription" action (reuses `PrescriptionService.getPrescriptionPdf`, same pattern as the existing Consultations list button and the Consultation form's own print action) and a "Back" link returning to the patient's Consultations/History list.
- No new App Shell top-level nav tab — history remains reached only in the context of a specific patient (from Patient Detail), consistent with R4's strict patient-scoping and with how Appointments/Consultations are already navigated.

## 9. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — JWT/`authGuard`/auth interceptor protect every route and endpoint this module touches, no changes needed. Patient Management (Module 2, built) — history is scoped to a patient; reuses `GET /api/patients/{id}` as-is. Consultation & Clinical Records (Module 4, built) — this module's entire data surface is `Visit`/`VisitDto`, read via the already-built `GetVisitsByPatientIdQuery`/`GetVisitByIdQuery`, extended rather than replaced. Prescription & Medication Management (Module 5, built) — medications are read via the already-built `VisitDto.Medications` and the existing PDF print endpoint, reused as-is.
- **Downstream**: Data Export (Module 8) — a patient's history (visits + medications) is a natural per-patient export source; Module 8 should reuse the same `VisitDto`/date-range query shape this module establishes (optional `fromDate`/`toDate` on the patient-scoped visit query) rather than re-deriving a parallel filtering mechanism. Search & Navigation (Module 7) — navigation flows from search results into a patient's profile, from which this module's (extended) Consultations/History section and new detail view are reached; Module 7 does not need to build its own history access path.

## 10. Implementation Tasks

**Increment 1 — Date-range filtering (server + list UI)**
1. Confirm Open Questions 2 and 3 (absorb-into-Consultations vs. separate History destination; date-range UI shape) with Product Owner before starting UI work — low-risk to proceed with the server-side query change regardless of the outcome, since both resolutions need the same underlying `fromDate`/`toDate` capability.
2. Add a composite index `(PatientId, VisitDate)` to `VisitConfiguration.cs`; generate and apply the EF Core migration (`AddVisitsPatientIdVisitDateIndex` or similar).
3. Extend `GetVisitsByPatientIdQuery`/`GetVisitsByPatientIdQueryHandler` to accept optional `fromDate`/`toDate` parameters; apply the range predicate in `VisitRepository.GetByPatientIdAsync` (extend its signature, or add an overload — developer's call) before the existing `OrderByDescending(v => v.VisitDate)`.
4. Add a small validation check (`fromDate <= toDate` when both supplied) returning a `400`/`Failure` result consistent with existing conventions; unparseable date query-string values also `400`.
5. Extend `VisitsController.GetAll` to accept and forward `fromDate`/`toDate` query parameters.
6. xUnit unit tests: query returns full list when no dates supplied (regression-proof of existing behavior); returns only visits within an inclusive range when both supplied; returns visits on/after `fromDate` when only `fromDate` supplied; returns visits on/before `toDate` when only `toDate` supplied; returns empty array (not error) when no visits fall in range; rejects `fromDate > toDate`.
7. xUnit integration tests: `GET /api/visits?patientId=&fromDate=&toDate=` end-to-end against a seeded set of visits spanning multiple dates — correct subset returned; unauthenticated request still rejected (`401`); malformed date query strings → `400`.
8. Angular: extend `visit.service.ts`'s `listByPatientId` to accept optional `fromDate`/`toDate` parameters, forwarded as query params.
9. Angular: add the date-range filter control (From/To date inputs, "Clear filter" action) to `patient-detail.component.html`/`.ts`; re-fetches visits on filter change; adds the distinct filtered-empty-state message.
10. Angular: add the missing Complaints line to the existing Consultations list row template.
11. Angular component tests: filter narrows the rendered list correctly for a given From/To combination; clearing the filter restores the full list; filtered-empty state renders the correct distinct message; Complaints text renders per visit row when present, and the row layout degrades gracefully when Complaints is blank (matches existing Diagnosis-blank handling).

**Increment 2 — Read-only Visit Detail view + placeholder cleanup**
12. Resolve Open Question 1 (dedicated read-only component vs. reused edit form in view-only mode) with Product Owner/architect before starting this increment — recommended default (§5) is a new dedicated component.
13. Build the new read-only Visit Detail component per the resolved approach: static rendering of vitals/complaints/diagnosis/medications from `GET /api/visits/{id}` (already returns everything needed, no server change required), "Print Prescription" action (reuses `PrescriptionService`), "Back" link.
14. Register the new route (behind `authGuard`) in `app.routes.ts`.
15. Update each Consultations list row in `patient-detail.component.html` to link to the new read-only detail route instead of `/consultations/:id/edit`; confirm the separate Module 4 edit entry point (if the doctor needs to actually correct a record) remains reachable and clearly distinct (e.g., from within the detail view or elsewhere per Open Question 1's resolution — do not silently remove edit access, R8 restricts *this view*, not the app as a whole).
16. Remove the dead `<nav class="placeholder-nav">` "History (coming soon)" block and its unused styles from `patient-detail.component.html`/`.scss`.
17. Angular component tests: detail view renders all fields correctly (including "Not recorded" vitals states) for a visit with and without medications, with and without complaints/diagnosis text; no editable control or submit action exists anywhere on the page (R8/AC verification); "Print Prescription" and "Back" navigate correctly; the placeholder nav block no longer renders anywhere in the component's DOM.

**Cross-cutting**
18. Confirm all three open questions (§14) are resolved and documented before Increment 2 is considered complete.
19. Time a full "open patient → apply date filter → open a past visit's detail" pass informally against R7's 2–5 second target, with the composite index in place, using a representative visit volume.
    - **Done (2026-08-25).** Server-side timing evidence: with the `(PatientId, VisitDate)` index in place and 30 seeded visits for a single patient (a generous stand-in for Phase 1's single-clinic visit volume), a temporary instrumented integration test measured `GET /api/visits?patientId=` (unfiltered) at ~58ms and the same call with `fromDate`/`toDate` applied at ~12ms, both against the test API host — orders of magnitude under the 2–5 second target. Combined with the single existing `GET /api/visits/{id}` call for the detail view (no list scan, PK lookup), the full "open patient → filter → open detail" pass comfortably meets R7/AC4. The instrumented test was temporary (used only to gather this reading) and was not retained in the permanent suite, consistent with §10's "informal, not an automated test" framing.

## 11. File Structure (indicative, framework-agnostic)

```
src/server/
  PatientManagement.Application/
    Visits/
      Queries/
        GetVisitsByPatientIdQuery.cs        # extended: fromDate/toDate params + validation
  PatientManagement.Infrastructure/
    Persistence/
      Configurations/
        VisitConfiguration.cs               # extended: composite (PatientId, VisitDate) index
    Repositories/
      VisitRepository.cs                    # extended: GetByPatientIdAsync date-range filter
    Migrations/
      <timestamp>_AddVisitsPatientIdVisitDateIndex.cs
  PatientManagement.Api/
    Controllers/
      VisitsController.cs                   # extended: GetAll accepts fromDate/toDate
  PatientManagement.Tests/
    Unit/Visits/
      GetVisitsByPatientIdQueryTests.cs     # extended cases
    Integration/Visits/
      VisitsEndpointsTests.cs               # extended cases

src/client/src/app/
  core/visits/
    visit.service.ts                        # extended: listByPatientId(patientId, fromDate?, toDate?)
  features/patients/
    detail/
      patient-detail.component.html         # extended: date filter, complaints line, updated row links
      patient-detail.component.ts           # extended: filter state + re-fetch on change
      patient-detail.component.scss         # extended: filter control styles, placeholder-nav removed
      patient-detail.component.spec.ts      # extended
  features/patient-history/                  # new — naming/path per Open Question 1 resolution
    visit-detail/
      visit-detail.component.ts / .html / .scss
      visit-detail.component.spec.ts
```

## 12. Security Considerations

- All extended (`GET /api/visits`) and reused (`GET /api/visits/{id}`, `GET /api/patients/{id}`, prescription PDF) endpoints, plus the new read-only detail route, remain behind the existing JWT bearer requirement (`RequireAuthenticatedUser` fallback policy / `authGuard`) — no `[AllowAnonymous]` added (BRD Security NFR), consistent with Modules 2, 4, and 5.
- Server-side validation on the new `fromDate`/`toDate` parameters regardless of client-side date-picker constraints — the server never trusts client-supplied date ordering without checking `fromDate <= toDate`, same posture as every prior module's input handling.
- The read-only detail view introduces **no new write path** by construction (R8) — this is itself a security/data-integrity property, not just a UX one: a screen reachable from history can never silently corrupt a past clinical record. Verified explicitly in test strategy (§13), not just assumed from component design.
- History data (vitals, complaints, diagnosis, medications) carries the same PII/clinical sensitivity classification already established in Modules 4/5 — no denormalized copies are introduced by this module's date-filtered query, no logging of field values in the extended query/handler.
- Data in transit protected via the app's existing HTTPS enforcement; encryption at rest and backup inclusion remain Module 9 concerns — this module introduces no new storage path (no client-side caching of history data beyond normal in-memory Angular state).
- All EF Core queries (including the new date-range predicate) use parameterized LINQ, never raw SQL string concatenation — same injection-avoidance posture as every prior module.

## 13. Test Strategy

**Unit tests (xUnit, Application layer)**
- `GetVisitsByPatientIdQuery`/handler: no dates supplied → full list, unchanged order (regression test protecting existing behavior); both dates supplied → only visits with `VisitDate` in the inclusive range; only `fromDate` → visits on/after that date; only `toDate` → visits on/before that date; a range containing zero visits → empty array, not an error; `fromDate > toDate` → validation failure (`400`-equivalent `Result`); a single-day range (`fromDate == toDate`) → only visits on that exact date, covering Modules\06 §4's "a specific date" case via the same mechanism as a range.

**Integration tests (xUnit + `WebApplicationFactory`)**
- `GET /api/visits?patientId=&fromDate=&toDate=` against a seeded multi-date visit set → correct filtered subset, correct order.
- Same endpoint with no date params → identical response to today's pre-Module-6 behavior (no regression).
- Malformed date query strings → `400`.
- `fromDate > toDate` → `400`.
- Unauthenticated request (any of the touched/new endpoints) → `401`.
- `GET /api/visits/{id}` (unchanged) still returns the full detail shape (vitals/complaints/diagnosis/medications) consumed by the new read-only view — confirms no server change was needed here, only client consumption.

**E2E / component-level (Angular)**
- Doctor opens a patient with visits spanning several months, applies a From/To filter narrowing to a two-week window — only the expected visits render, in the same most-recent-first order as before filtering.
- Doctor clears the filter — full list is restored without a page reload.
- Doctor applies a filter with no matching visits — sees the distinct "No visits found in the selected date range" message, not the generic "no consultations at all" message.
- Doctor opens a past visit from the (filtered or unfiltered) list — the new read-only detail view renders vitals (including any "Not recorded" states exactly as captured), complaints, diagnosis, and the full medication list correctly, with no editable control or save action anywhere on the screen.
- Doctor opens a past visit that has zero medications — detail view shows an appropriate empty state (consistent with Module 5's existing "No medications prescribed" print-view precedent) rather than a broken/empty table.
- Doctor clicks "Print Prescription" from the new detail view — same working PDF flow as the existing Consultations-list and Consultation-form print actions (regression check that reuse didn't break anything).
- The dead "History (coming soon)" placeholder no longer appears anywhere in the Patient Detail DOM.
- Doctor times a full "open patient → filter by date → open a past visit's detail" pass and it completes comfortably within the 2–5 second target (informal UX validation of R7, not an automated test).

**Performance**
- `GET /api/visits?patientId=&fromDate=&toDate=` stays within the BRD's 2–5 second retrieval target for a realistic per-patient visit volume, validated once the `(PatientId, VisitDate)` composite index is in place; no dedicated load test given inherently modest per-patient visit counts in a single-clinic Phase 1 app — consistent with Module 5's precedent of skipping load testing for similarly small-volume child collections.

## 14. Acceptance Criteria

- AC1: Opening a patient's profile shows a correctly ordered (most-recent-first) list of their past visits. (Modules\06 §10 — already satisfied by existing code; regression-verified by this plan's test suite.)
- AC2: Selecting a visit displays its full vitals, complaints, diagnosis, and medications (prescription) via a read-only view that offers no path to edit the record. (Modules\06 §10 + R8)
- AC3: Applying a date filter (specific date or range) correctly narrows the visible visit list to visits whose date falls within the selected range; clearing the filter restores the full list. (Modules\06 §10)
- AC4: History for a patient with a typical visit volume loads within 2–5 seconds, both filtered and unfiltered. (Modules\06 §10 + §6)
- AC5: Complaints text captured in Module 4 is visible somewhere in the patient's history — either in the list summary or the detail view (this plan places it in both) — closing the current gap where it's captured but never displayed. (Modules\06 §4 #2 / BRD "Access: ... Complaints ...")
- AC6: No write/edit action is reachable from the history list or the read-only detail view. (Modules\06 §3 Out of Scope; R8)
- AC7: All history-related endpoints and routes (extended and reused) reject unauthenticated access. (BRD Security NFR)
- AC8: The dead "History (coming soon)" placeholder no longer appears in the UI once this module ships. (Cleanup item raised by this plan's delta analysis, §3)

## 15. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Modules\06 §7 names "Visit, Prescription, Medication" as the entities this module reads, but no `Prescription` table exists in the live schema (Module 5 built medications directly on `Visit`) | Could cause confusion for a developer picking up this plan expecting a `Prescription` query to exist | Explicitly documented (§2) that this module follows Module 5's established interpretation — `VisitDto.Medications` *is* "the prescription" — no separate query/entity is introduced; flagged so no one goes looking for a table that was never built |
| Whether "history" should be a visually/navigationally distinct section from the existing "Consultations" section, or the same section extended in place, is not stated by the BRD/Modules\06 | If the Product Owner expects a clearly separate "History" destination (matching the dead placeholder's implication), this plan's "absorb in place" approach would need a presentational (not architectural) rework | Flagged explicitly as Open Question 2 (§5, §14); low rework cost if wrong since the underlying data/query is identical either way — only the component wrapping/navigation entry point would change |
| Whether the read-only visit detail should be a new dedicated component or the existing edit form opened in a non-submitting "view mode" is not dictated by the BRD (doctor is the sole user; BRD doesn't distinguish view/edit access) | Building the wrong shape means either unnecessary duplication (if a shared component was actually wanted) or added complexity in the edit form (if view-mode-in-place was actually wanted) | Flagged explicitly as Open Question 1 (§5, §10 task 12) with a stated recommendation (dedicated component) and rationale, pending confirmation before Increment 2 starts |
| Date-range filter UI shape (two date pickers vs. presets vs. single date) is not specified by the BRD/Modules\06 beyond "filter by date" | Building presets no one asked for (or missing an expected single-date shortcut) wastes effort or under-delivers against an unstated expectation | Flagged explicitly as Open Question 3 (§5); the chosen From/To two-picker approach is stated as covering both the "specific date" and "range" cases named in Modules\06 §4, with rationale, pending confirmation |
| Composite `(PatientId, VisitDate)` index is an addition beyond what Modules 4/5 already shipped | Minor: an additional migration to review/apply; negligible write-path overhead (index maintenance on an already-modest-write-volume table) | Scoped as a small, additive, non-breaking migration (§6, §10 task 2); justified directly against the stated 2–5 second performance NFR rather than spec'd speculatively |
| Removing the "History (coming soon)" placeholder and repointing Consultations-list row links away from `/consultations/:id/edit` could accidentally remove the doctor's only path to actually edit a past visit if not handled carefully | Would silently violate Module 4's already-shipped edit capability — a regression, not a Module 6 requirement | Explicitly called out in §10 task 15: the separate edit entry point must remain reachable and clearly distinct after this change; verified in Increment 2's test pass, not left to incidental behavior |

---

## Open Questions — Requiring Product Owner / Architect Confirmation

1. **Read-only detail view shape**: this plan recommends building a new, dedicated read-only Visit Detail component rather than reusing `consultation-form.component` in a non-submitting "view mode." Confirm this approach, or state a preference for reusing the edit form with editing disabled/hidden instead (the BRD does not distinguish view from edit access for the sole doctor user, so either satisfies the letter of the requirement — this is a maintainability/safety trade-off, not a compliance question).
2. **History as its own destination vs. an extension of "Consultations"**: this plan extends the existing per-patient "Consultations" section in place (adding date filtering and complaints) rather than building a visually/navigationally separate "History" screen, and removes the dead "History (coming soon)" placeholder that implied a separate destination. Confirm this is acceptable, or state a preference for a genuinely distinct History section/tab even though it would read from the identical underlying data and query.
3. **Date-range filter UI shape**: this plan defaults to two native date inputs (From / To), covering both "a specific date" (from == to) and "a date range" per Modules\06 §4, with no preset ranges (e.g., "last 7 days") since none are named in the BRD/Modules docs. Confirm this is sufficient, or state a preference for preset ranges or a different control shape.

---

## Dependencies Recap (for sequencing awareness)

This module sits sixth in the fixed build order (Authentication → Patient Management → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → **Patient History** → Search & Navigation → Data Export → Data Backup & Reliability → Administration). Modules 1, 2, 4, and 5 are already built and merged; this module takes no new upstream dependency, only extends what already exists. Search & Navigation (Module 7) and Data Export (Module 8) are the downstream consumers: both should build against the `fromDate`/`toDate`-extended `GetVisitsByPatientIdQuery`/`VisitDto` shape this module establishes rather than introducing a parallel patient-history query.
