# Module 7: Search & Navigation — Implementation Plan

## 1. Module Overview

Search & Navigation is the connective tissue layered over Patient Management (Module 2) and Patient History (Module 6): it owns no clinical data of its own and introduces no new business entity. Its job is purely to shrink the time between "I need to see this patient" and "I'm looking at their record" — a persistent, fast, partial-match patient search reachable from anywhere in the app, a short "recently viewed" list for one-click return access, and tight cross-navigation between a patient's profile, appointments, and visit history. This directly serves the BRD's usability goal of a UI "optimized for fast data entry and retrieval during consultations" and the Non-Functional Requirement "Fast patient search and retrieval" (Modules\07 §6, §11).

**This is a delta plan, not a from-scratch plan.** A substantial share of this module's functional surface already exists, built incidentally as part of Module 2's Increment 3 (the Patients browse grid) and Module 6's read-navigation work. What genuinely remains is: (1) making search **persistent/global** rather than confined to the `/patients` page, (2) building **Recently Viewed Patients** from nothing — it does not exist anywhere in the codebase today, and (3) closing one concrete cross-navigation gap — the Appointments list shows a patient's name as plain text, not a link to their profile. This plan scopes precisely those gaps, reusing the existing search endpoint and query semantics rather than duplicating them.

## 2. Business Requirements

| # | Requirement | Source |
|---|---|---|
| R1 | Provide a quick patient search matching by partial name or phone, available as the doctor types | Modules\07 §4 #1; BRD Functional Requirements → Search & Navigation "Quick patient search" |
| R2 | Search is scoped strictly to patient records (name, phone) — never visit history, complaints, diagnosis, or prescription text | Modules\07 §3/§5; BRD "Search supports partial match, scoped to patient records only (not visit history/diagnosis text)" |
| R3 | Show a short list of recently viewed patients for one-click return | Modules\07 §4 #2; BRD "View recent patients" |
| R4 | Provide clear, low-click navigation between a patient's profile, appointments, and visit history | Modules\07 §4 #3; BRD "Easy navigation between patient profile and visits" |
| R5 | Search matching is partial (substring/prefix), not exact-match only | Modules\07 §5 |
| R6 | Navigation between profile and appointments/visits requires no more than one or two clicks | Modules\07 §10 |
| R7 | Search response feels near-instant (sub-second perceived latency) | Modules\07 §6, §11; BRD Performance NFR |
| R8 | Must be authenticated (JWT) to search or view any patient/navigation data | BRD Security NFR; module Dependencies (Auth) |

**Explicitly out of scope for this module** (do not build; flag rather than implement if requested): full-text search across complaints/diagnosis/prescription text, global/system-wide search across appointments or other non-patient entities, search filters/facets beyond partial name/phone match (Modules\07 §3 Out of Scope). These are deliberate BRD scoping decisions to keep search simple and fast, not gaps to close.

**Explicit assumption carried into this plan**: Modules\07 §7 describes "recently viewed" as "a UX convenience with no formal retention/audit requirement... kept minimal," and offers two implementation shapes without choosing one — "session-based or a small per-user table." This plan recommends a client-side (browser `localStorage`) implementation over a new server-side table; see §5 Architecture Approach and Open Question 1 for the rationale and the explicit flag for Product Owner confirmation.

## 3. Delta Analysis — What Already Exists vs. What Module 7 Adds

| Capability | Status today | Action needed |
|---|---|---|
| Partial-match patient search, name + phone, case-insensitive (R1, R2, R5) | **Done.** `GET /api/patients?query=&page=&pageSize=` (`SearchPatientsQuery`/`SearchPatientsQueryHandler`) performs a case-insensitive partial match against `FullName` and `PhoneNumber` only, paginated, ordered by `FullName` ascending. `PatientRepository.SearchAsync` is the sole matcher — it does not touch `Visit`, `Medication`, or any clinical table, so R2's scoping constraint is already structurally satisfied. | None to the matching logic itself — reuse as-is. |
| Search UI, debounced input (part of R1, R7) | **Done, but page-scoped.** `patients-list.component` has a `<input type="search">` wired to a 300ms-debounced `Subject` (`SEARCH_DEBOUNCE_MS`), calling the same `PatientService.list({ query, page, pageSize })` used for browse-all. This is a solid, working pattern — reused, not replaced. | **Gap: this search box only exists on `/patients`.** It is not a "persistent/global" search box reachable from Appointments, Dashboard, Consultation, or Patient Detail screens, which is what Modules\07 §4 #1 and the "connective tissue" framing call for. Add a global search entry point (§5, §7). |
| Recently viewed patients (R3) | **Missing entirely.** No service, storage, model, or UI renders anything resembling "recent patients" anywhere in the client. `dashboard.component` is a placeholder greeting only (`"You are signed in."` + email) with no content beyond that. | Build from scratch: a recording mechanism (on patient-detail view) + a small storage/service layer + a UI surface (§5, §7, §8). |
| Patient Profile → Appointments cross-navigation (part of R4) | **Done.** `patient-detail.component` renders an inline "Appointments" section (date, time, status) directly on the patient profile — zero extra clicks, better than the "1–2 click" bar in R6. | None. |
| Patient Profile → Consultations/Visit History cross-navigation (part of R4) | **Done** (built by Module 6). Same page renders a date-filterable "Consultations" list; each row links to `/visits/:id` (read-only detail, one click) and separately to `/consultations/:id/edit`. | None. |
| Visit Detail → Patient Profile / Print (part of R4) | **Done** (Module 6). `visit-detail.component` has a "Back" link to the patient and reuses the existing prescription-PDF print action. | None. |
| Appointments list → Patient Profile cross-navigation (part of R4) | **Missing.** `appointments-list.component.html` renders `{{ appointment.patientName }}` as plain, unlinked text in the table — the one place in the app today where a patient's name is visible but not clickable through to their profile. Inconsistent with every other list-of-patient-things screen (Patients grid, Consultations list), which do link. | Wrap the patient-name cell in a `routerLink` to `/patients/:patientId` (one click, satisfies R6). |
| "View all results" / deep-linkable search state | **Missing.** `patients-list.component` reads no query parameter from the route on init — search state (`searchTerm`) lives only in component memory, reset on navigation away. A global search widget needs somewhere to send "show me everything, not just the top N" for a query with many matches. | Extend `patients-list.component` to read an optional `?query=` route/query param on init and pre-populate the search box (small, additive change — no backend change, same endpoint). |
| Auth on all touched surfaces (R8) | **Done.** `GET /api/patients` sits behind the existing JWT bearer requirement; all client routes touched by this module (`/patients`, `/patients/:id`, `/appointments`, `/dashboard`) already sit behind `authGuard`. The app shell itself renders nothing (no nav, no search box by extension) when `isAuthenticated()` is false. | None — the new global search widget inherits this for free by living inside `AppShellComponent`'s existing `@if (isAuthenticated())` block. |
| Search performance / indexing (R7) | **Partially covered, inherited from Module 2, not owned by this module.** `PatientConfiguration.cs` has single-column (non-composite) indexes on `FullName` and `PhoneNumber`. A `Contains(...)` (SQL `LIKE '%term%'`) predicate — which is what partial/substring matching requires — cannot use a standard b-tree index for a leading wildcard; the index helps prefix/equality lookups but not arbitrary substrings. This is a **carried-forward Module 2 characteristic**, not something this module introduces or is scoped to fix. | No index/schema change proposed by this module (would be a Module 2 concern to revisit if real-world volume ever stresses it); flagged as a risk (§15) rather than silently re-engineered here, since Phase 1's single-clinic patient volume is small and the existing informal timing evidence from Module 6 (§10 task 19, sub-100ms visit queries at 30-row scale) suggests this is not yet a real bottleneck. |

## 4. Workflows

### 4.1 Global Quick Search
1. From any authenticated screen (Dashboard, Patients, Appointments, Patient Detail, Consultation form, Visit Detail), the doctor sees a persistent search box in the app header (new — `AppShellComponent`, §5/§7).
2. As the doctor types (debounced ~300ms, matching the existing `patients-list` pattern), the client calls the existing `GET /api/patients?query=&page=1&pageSize=8` (small page size — a typeahead, not the full grid).
3. A dropdown renders up to 8 matching patients (name, phone, DOB/age) below the search box. Matching is partial/substring and scoped to name/phone only (R2, R5) — unchanged from the existing endpoint's behavior.
4. Selecting a result (click or keyboard Enter/arrow-navigate) records it as "recently viewed" (§4.2, side effect) and navigates to `/patients/:id`.
5. If there are more matches than fit in the dropdown (`totalCount > pageSize`), a "View all N results" link at the bottom of the dropdown navigates to `/patients?query=<term>` — the existing Patients grid, now able to read the query from the route (delta item, §3) and pre-populate/re-run the same search server-side, full pagination included.
6. Clearing the search box (or navigating away) closes the dropdown without side effects; no query is re-run against a blank/whitespace-only term (mirrors the existing `list({ query: this.searchTerm || undefined })` convention — an empty term is browse-all, not "zero results").
7. Empty state: typing a term with zero matches shows a small "No patients found" row in the dropdown rather than nothing at all (distinguishes "still loading" from "confirmed no matches").

### 4.2 Recently Viewed Patients
1. Whenever the doctor lands on a patient's profile (`/patients/:id`, i.e., `patient-detail.component` successfully loads a patient), the client records that patient (id, full name, phone, viewed-at timestamp) into a small client-side "recent patients" store (§5 — recommended: `localStorage`, capped at the last 5 entries, most-recent-first, de-duplicated by patient id so re-viewing the same patient moves them to the top rather than creating a duplicate row).
2. The Dashboard screen (currently a placeholder greeting, §3) gains a "Recently Viewed Patients" section listing up to 5 entries; each is a one-click link to `/patients/:id` (R3, R6).
3. The same recent list also appears as the global search dropdown's default content when the search box is focused but empty (before the doctor types anything) — a zero-keystroke shortcut back to whoever was just being seen, directly serving the "get back to someone I was just looking at" user story (Modules\07 §6).
4. If the recent list is empty (first login, or `localStorage` was cleared), both surfaces show a small "No recently viewed patients yet" placeholder rather than an empty gap.
5. Recent-patient entries carry no clinical data (no vitals/diagnosis/complaints) — only identifying/contact fields already visible on the Patients grid, consistent with R2's "patient records only" scoping applied here by extension.

### 4.3 Cross-Navigation (Appointments → Patient, and existing paths)
1. From the Appointments list (`/appointments`), the doctor clicks a patient's name (newly a `routerLink`, §3 delta item) and lands directly on that patient's profile — one click, matching the pattern already used on the Patients grid and Consultations list.
2. From a patient's profile, the doctor reaches that patient's Appointments (inline section, already built) and Consultations/Visit History (inline section + `/visits/:id`, already built by Module 6) with zero or one click respectively — unchanged, verified as still meeting R6 by this plan's test pass rather than rebuilt.
3. From a Visit Detail screen, "Back" returns to the patient profile (already built by Module 6) — unchanged.

## 5. Architecture Approach

- **No new search backend, no new query/handler, no new endpoint.** `SearchPatientsQuery`/`SearchPatientsQueryHandler` and `GET /api/patients?query=` already implement exactly what R1/R2/R5 require — case-insensitive partial match against `FullName`/`PhoneNumber` only, paginated. This plan's global search widget calls the same endpoint with a smaller `pageSize` (typeahead-sized), not a parallel "search-lite" endpoint — avoids two sources of truth for "what counts as a patient match," consistent with the "extend, don't fork" precedent set by Modules 5 and 6.
- **Global search lives in `AppShellComponent`, not a new top-level route.** Since the shell already renders the persistent header/nav on every authenticated screen and already gates all of its content behind `isAuthenticated()`, adding the search box there is the natural, lowest-effort way to make search truly global without touching every feature route individually. The dropdown-of-results pattern (not a full-page redirect on every keystroke) keeps the doctor on their current screen unless they explicitly choose a result — minimizing workflow interruption during a live consultation, directly serving Modules\07's "connective tissue" framing.
- **Recently viewed — client-side `localStorage`, not a new server-side table (recommended; flagged as Open Question 1).** Modules\07 §7 explicitly leaves this open ("session-based or a small per-user table") and states there is "no formal retention/audit requirement." Rationale for choosing client-side: (a) this is a single-user, single-clinic, effectively single-workstation application per the BRD's Phase 1 scope — there is no cross-device sync requirement anywhere in the BRD that would justify a server round-trip and a new table just to remember "who did I just look at"; (b) it avoids a write on every single patient-profile view, which a server-backed "recently viewed" table would otherwise require, with no corresponding read benefit given the single-user premise; (c) it avoids scope creep into a new entity/migration/repository/endpoint for a feature the BRD itself describes as a minimal UX convenience. **This is a genuine architecture decision the BRD doesn't dictate — flagged for Product Owner/architect confirmation before implementation** (Open Question 1): if the doctor is expected to routinely switch browsers/devices/profiles and expects "recent patients" to follow them, a small server-side table (`patient_id`, `viewed_at`, keyed to the single user) would be the correct alternative and is a small, low-risk pivot from this plan's default.
- **Recording side effect placed on successful patient-detail load, not on search-result selection alone.** A patient can be reached via global search, the Patients grid, an Appointments-list link, or a Consultation/Visit-detail "back" link — recording centrally in `patient-detail.component`'s successful load path (rather than scattering "record as recent" calls across every possible entry point) guarantees consistent behavior regardless of how the doctor arrived, and avoids missed recordings from a link this plan didn't anticipate.
- **Deep-linkable search state via a `query` route/query-param on `/patients`**, read once on `patients-list.component` init (falls back to the existing empty-string default when absent) — a small, additive change that lets the global search widget's "View all N results" link hand off to the existing grid without needing the grid to change its core fetch/pagination logic at all.
- **Validation/scoping enforcement stays server-side, unchanged**: R2's "patient records only" guarantee is structural (the repository query never joins to `Visit`/`Medication`), not a client-side filter that could be bypassed or drift — this plan does not add any new server-side matching logic that could reintroduce that risk.
- **No caching layer, no search index (e.g., Elasticsearch) introduced.** Phase 1's single-clinic, moderate patient-volume scope (per the BRD and Module 2's plan) does not justify the operational overhead; the existing SQL `Contains` predicate is judged adequate for R7 at this scale, consistent with the informal timing precedent set in Module 6's plan (§10 task 19).

## 6. Database Entities

**No new table or entity.** This module introduces no schema change. It is a pure UI/navigation layer over the existing `Patients` table (Module 2) and reuses `Visits`/`Medications` read paths already built by Modules 4–6 for cross-navigation targets. If Open Question 1 is resolved in favor of a server-side "recently viewed" store instead of the recommended `localStorage` approach, the following would be the minimal addition (documented here for completeness, not committed to by default):

| Table (conditional, only if Open Q1 resolves server-side) | Field | Type | Notes |
|---|---|---|---|
| `RecentlyViewedPatients` | `Id` | GUID, PK | |
| | `PatientId` | GUID, FK → `Patients.Id` | Cascade delete if a patient is ever removed (no delete flow exists in Phase 1, but keeps referential integrity honest) |
| | `ViewedAt` | `DateTime` (UTC) | Used to order most-recent-first and to trim beyond the last N |

No indexes beyond a straightforward `(ViewedAt DESC)` would be needed given the tiny row count this table would ever hold (capped list, single user). **This table is not part of the default plan** — see §5/Open Question 1.

## 7. APIs

No new endpoints are introduced by the default (client-side recent-patients) architecture. This module is entirely a consumer of the existing Module 2 search endpoint.

| Method | Path | Purpose | Auth | Notes |
|---|---|---|---|---|
| `GET` | `/api/patients?query={term}&page=1&pageSize=8` | *(existing, unchanged)* Powers the new global search typeahead — same endpoint as the Patients grid, called with a smaller `pageSize` for dropdown-sized results | Bearer JWT required | No server change; client-only reuse with a different `pageSize` |
| `GET` | `/api/patients?query={term}&page=&pageSize=25` | *(existing, unchanged)* Powers the "View all N results" hand-off from the search dropdown into the full Patients grid | Bearer JWT required | No server change |
| `GET` | `/api/patients/{id}` | *(existing, unchanged, Module 2)* Reused by Patient Detail; its successful load is where "recently viewed" recording happens (client-side, no API involved) | Bearer JWT required | No server change |

**If Open Question 1 resolves server-side** (not the default), one additive pair would be needed: `GET /api/recent-patients` (returns the last N, most-recent-first) and `POST /api/recent-patients` (upserts a `patient_id` + `viewed_at`, trims beyond N) — both behind the existing JWT policy, following the same controller/handler pattern as every other module. Not built unless that open question is resolved away from the default.

## 8. UI / Screens

- **App Shell header — new global search widget** (`core/shell/app-shell.component`, extended): a search `<input>` in the header (visible only when `isAuthenticated()`, matching the existing nav/logout visibility rule) with a debounced (~300ms) typeahead dropdown showing up to 8 matching patients (name, phone, age) or, when the input is empty/focused, the Recently Viewed list (§4.2 step 3). Keyboard-navigable (arrow keys + Enter, `Escape` to close) for fast keyboard-driven use during a consultation. A "View all N results" row appears when matches exceed the dropdown size, linking to `/patients?query=`.
- **Dashboard — new "Recently Viewed Patients" section** (`features/dashboard/dashboard.component`, extended from its current placeholder): up to 5 entries (name, phone, last-viewed relative time e.g. "2 hours ago"), each a one-click link to `/patients/:id`; an explicit "No recently viewed patients yet" empty state for first use.
- **Patients grid (`features/patients/list/patients-list.component`) — minor extension**: reads an optional `query` route/query parameter on init to pre-populate the search box and re-run the existing search, supporting deep-linking from the new global search widget's "View all" action. No visual change to the grid itself.
- **Appointments list (`features/appointments/list/appointments-list.component`) — minor extension**: the patient-name table cell becomes a `routerLink` to `/patients/:patientId`, matching the link styling already used elsewhere (Patients grid name column, Consultations list rows).
- No other screens change. Patient Detail, Visit Detail, and the Consultation form's existing cross-navigation (built by Modules 2, 4–6) already satisfy R4/R6 and are verified, not rebuilt, by this plan's test pass.

## 9. Dependencies

- **Upstream**: Authentication & Authorization (Module 1, built) — `authGuard`/JWT/the shell's `isAuthenticated()` gate protect every surface this module adds, no changes needed. Patient Management (Module 2, built) — this module's entire search surface is the existing `GET /api/patients?query=` endpoint and `PatientService`, reused as-is with a different `pageSize` for the typeahead case. Patient History (Module 6, built) — the Consultations/Visit-Detail cross-navigation this module verifies (rather than rebuilds) was delivered there.
- **Downstream**: Data Export (Module 8) — is reached from a patient's profile/history, which this module makes faster to get to, but Module 8 does not depend on any new capability this module introduces (no new query shape, no new entity). No other module depends on Search & Navigation's output.

## 10. Implementation Tasks

**Increment 1 — Global search widget**
1. Confirm Open Question 1 (client-side `localStorage` vs. server-side recent-patients table) with Product Owner/architect before starting Increment 2 — low-risk to proceed with Increment 1 (global search) regardless of that outcome, since it touches an unrelated part of the app.
2. Add a `SearchWidgetComponent` (or inline into `AppShellComponent`, developer's call on componentization) rendering the header search `<input>`, debounced (~300ms, reuse the existing `patients-list` debounce pattern/constant) call to `PatientService.list({ query, page: 1, pageSize: 8 })`.
3. Render the results dropdown: up to 8 matches (name, phone, age), "No patients found" empty state, "View all N results" row when `totalCount > 8`, keyboard navigation (arrow up/down, Enter to select, Escape to close), click-outside-to-close.
4. Wire result selection to navigate to `/patients/:id` and (pending Open Question 1's default) record the selection as recently viewed.
5. Extend `patients-list.component` to read an optional `query` route/query parameter on `ngOnInit`, pre-populating `searchTerm` and triggering the initial fetch with it — supports the "View all N results" hand-off (§8).
6. Angular component tests: typing a partial term renders matching results; a term with zero matches shows the empty state; selecting a result navigates correctly; "View all" navigates to `/patients?query=` with the term preserved; keyboard navigation selects the highlighted result; dropdown closes on Escape/click-outside; search box renders nothing when unauthenticated (mirrors existing shell nav visibility tests).

**Increment 2 — Recently Viewed Patients**
7. Build a `RecentPatientsService` (`core/patients/recent-patients.service.ts` or similar): `record(patient)` (upsert by id, move-to-front, cap at 5, persist to `localStorage` under a namespaced key), `list()` (read + parse, most-recent-first), `clear()` (used on logout, §12).
8. Call `RecentPatientsService.record(...)` from `patient-detail.component`'s successful patient-load path (single recording point per §5).
9. Extend the Dashboard component/template to render the "Recently Viewed Patients" section using `RecentPatientsService.list()`, with the empty-state message.
10. Extend the global search widget (Increment 1) to show `RecentPatientsService.list()` as its default (pre-keystroke) content.
11. Wire `RecentPatientsService.clear()` into the existing logout flow (`AuthService.logout`) so recent-patient data does not persist across a session boundary on a shared/public machine (§12 Security).
12. Angular unit tests for `RecentPatientsService`: recording caps at 5 and evicts the oldest; re-viewing an existing entry moves it to the front without duplicating; `clear()` empties the list; malformed/corrupted `localStorage` content is handled gracefully (falls back to an empty list, does not throw).
13. Angular component tests: Dashboard renders the recent list and its empty state correctly; visiting patients in sequence updates the list and ordering correctly end-to-end; the global search widget's pre-keystroke content matches the recent list; logout clears the list (verified by re-checking `RecentPatientsService.list()` post-logout).

**Increment 3 — Cross-navigation gap closure**
14. Update `appointments-list.component.html` to wrap the patient-name cell in a `routerLink` to `/patients/:patientId`, matching the existing link styling/pattern used on the Patients grid and Consultations list.
15. Angular component test: clicking a patient's name in the Appointments list navigates to `/patients/:patientId`.

**Cross-cutting**
16. Manually verify (informal, consistent with Module 6's precedent) that every specified cross-navigation path (Patients grid → profile, Appointments → profile, profile → appointments/consultations inline, Consultations row → Visit Detail, Visit Detail → back to profile, Dashboard recent list → profile, global search result → profile) is reachable in one or two clicks (R6), and that global search feels sub-second end-to-end (R7) at realistic Phase 1 data volume.
17. Confirm Open Question 1 is resolved and, if it flips away from the default (`localStorage`), replace Increment 2's tasks 7–13 with the server-side equivalent (§6/§7's conditional table/endpoints) before calling this module complete.

## 11. File Structure (indicative, framework-agnostic)

```
src/client/src/app/
  core/
    shell/
      app-shell.component.ts          # extended: hosts/embeds the search widget
      app-shell.component.html        # extended: header search input + dropdown
      app-shell.component.scss        # extended: search widget styles
    patients/
      recent-patients.service.ts      # new: localStorage-backed recent list (record/list/clear)
      recent-patients.service.spec.ts # new
    shared/
      search-widget/                  # new (or inline in app-shell, developer's call)
        search-widget.component.ts
        search-widget.component.html
        search-widget.component.scss
        search-widget.component.spec.ts
  features/
    dashboard/
      dashboard.component.ts          # extended: renders Recently Viewed Patients section
      dashboard.component.html        # extended
      dashboard.component.scss        # extended
    patients/
      list/
        patients-list.component.ts    # extended: reads `query` route/query param on init
        patients-list.component.spec.ts
      detail/
        patient-detail.component.ts   # extended: calls RecentPatientsService.record(...) on load
    appointments/
      list/
        appointments-list.component.html  # extended: patient name becomes routerLink
        appointments-list.component.spec.ts
  core/auth/
    auth.service.ts                   # extended: logout() also calls RecentPatientsService.clear()

# No server-side changes under the default (client-side recent-patients) architecture.
# If Open Question 1 resolves server-side, add (not built by default):
src/server/
  PatientManagement.Application/RecentPatients/...
  PatientManagement.Infrastructure/Repositories/RecentPatientRepository.cs
  PatientManagement.Api/Controllers/RecentPatientsController.cs
```

## 12. Security Considerations

- The global search widget and all its data (via `GET /api/patients`) remain behind the existing JWT bearer requirement / `authGuard` — it is rendered only inside `AppShellComponent`'s existing `isAuthenticated()` gate, inheriting the same posture as the header nav/logout it sits beside. No new `[AllowAnonymous]` surface is introduced.
- Search continues to match name/phone only (R2) — no change to the server-side query surface that could accidentally widen matching into clinical text, preserving the deliberate BRD scoping decision.
- **`localStorage`-based recently-viewed data is unencrypted browser storage**, unlike server-persisted data which is in scope for Module 9's at-rest encryption. This is a real, if minor, gap relative to a hypothetical fully-encrypted posture: recently-viewed entries carry patient name/phone (PII) but explicitly no clinical data (vitals/diagnosis/complaints are never written to this store, §4.2 step 5), limiting exposure. Given the BRD's single-user, single-workstation Phase 1 scope and the module's own "no formal retention/audit requirement" framing, this is judged an acceptable trade-off for the default architecture — but it is the central reason Open Question 1 exists, and a server-side alternative would close this gap if the Product Owner considers it material.
- `RecentPatientsService.clear()` is wired into logout (§10 task 11) so recently-viewed data does not persist indefinitely on a shared or public machine past the end of a session — a deliberate mitigation for the `localStorage` trade-off above.
- No new write path is introduced to any clinical entity by this module — it is read-only navigation plus a client-side convenience list.
- All EF Core queries touched (only the pre-existing `SearchAsync`) remain parameterized LINQ, unchanged by this module.

## 13. Test Strategy

**Unit tests (Angular, `RecentPatientsService`)**
- Recording a new patient adds it to the front of the list.
- Recording a patient already in the list moves it to the front rather than duplicating.
- Recording beyond the cap (5) evicts the oldest entry.
- `clear()` empties the stored list.
- Reading from a corrupted/malformed `localStorage` value returns an empty list rather than throwing.

**Component tests (Angular)**
- Search widget: typing a partial name/phone renders matching results within the debounce window; a term with zero matches renders the "No patients found" state; clearing the input reverts to the pre-keystroke (recent-patients) content; "View all N results" appears only when `totalCount` exceeds the dropdown size and links to `/patients?query=` with the term preserved; arrow-key navigation plus Enter selects the highlighted result; Escape and click-outside close the dropdown; nothing renders when unauthenticated.
- Dashboard: renders up to 5 recently viewed patients, most-recent-first; renders the correct empty state with zero recent patients; each entry links to the correct patient profile.
- Patients grid: `?query=` route parameter pre-populates the search box and returns the expected filtered/paginated result set on load; grid behaves identically to today when no `query` parameter is present (regression check).
- Appointments list: patient name renders as a working link to `/patients/:patientId`; status-update and "Start Consultation" behavior for that row is unaffected by the change (regression check).
- Patient Detail: successfully loading a patient triggers exactly one `RecentPatientsService.record(...)` call with the correct patient data; a failed load (404/error) does not record anything.
- Logout: `RecentPatientsService.list()` returns empty immediately after logout.

**Integration tests (existing, regression-only — no server change)**
- `GET /api/patients?query=` continues to behave exactly as today (Module 2's existing integration test suite) — this module adds no new server-side behavior to test, only confirms via its own client tests that it consumes the existing contract correctly.

**E2E / manual pass**
- Doctor is on the Appointments screen, types a partial name into the global search box, and reaches the correct patient's profile in under 2 clicks/keystroke-selections (R6/R7).
- Doctor views three different patients in sequence, then opens the global search box with an empty query — sees exactly those three patients, most-recent-first, and can click straight to any of them.
- Doctor clicks a patient's name from the Appointments list and lands on that patient's profile.
- Doctor performs a search whose term matches a patient's diagnosis text (not name/phone) and confirms zero results are returned for that reason alone (explicit R2 negative-case check) — e.g., searching a diagnosis keyword typed by mistake into the patient search box returns "No patients found," not a false-positive match.
- A full "type in global search → select patient → view profile" pass is timed informally and completes well within the sub-second-feel target (R7), consistent with Module 6's informal timing precedent.

**Performance**
- No dedicated load test — Phase 1's single-clinic, moderate patient-volume scope (per Module 2's plan) does not warrant one; the existing `Contains`-based search is judged sufficient at this scale (§3, §15 risk).

## 14. Acceptance Criteria

- AC1: Typing a partial name or phone number into the (now-global) search box returns matching patients without requiring an exact match, from any authenticated screen. (Modules\07 §10)
- AC2: Search results never include matches based on diagnosis, complaint, or prescription text — verified with an explicit negative-case test (§13). (Modules\07 §10, R2)
- AC3: A recently viewed list is visible (Dashboard, and as the search box's default content) and clicking an entry navigates directly to that patient's profile. (Modules\07 §10, R3)
- AC4: Navigation between a patient's profile and their appointments/visits requires no more than one or two clicks — verified for every named path including the newly linked Appointments-list patient name. (Modules\07 §10, R4/R6)
- AC5: The global search widget is reachable from Dashboard, Patients, Appointments, Patient Detail, Consultation form, and Visit Detail screens — closing this plan's central identified gap (§3). (Modules\07 §4 #1, "connective tissue" framing)
- AC6: All search/navigation surfaces reject unauthenticated access (render nothing / redirect via `authGuard`). (BRD Security NFR, R8)
- AC7: Recently viewed data is cleared on logout. (§12, mitigation for the `localStorage` trade-off)

## 15. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Modules\07 §7 leaves the "recently viewed" storage mechanism unspecified (session vs. per-user table) | Building the wrong shape means either an unwanted new server table/migration, or (if the Product Owner actually wanted cross-device persistence) a `localStorage` implementation that doesn't meet their expectation | Flagged explicitly as Open Question 1 (§5) with a stated default (`localStorage`) and rationale, pending confirmation before Increment 2 starts; the server-side alternative is fully specified (§6/§7 conditional tables/endpoints) so the pivot is low-cost if needed |
| `localStorage`-based recent-patients data is unencrypted at rest, unlike server-persisted data covered by Module 9 | Minor PII exposure risk (patient name/phone only, no clinical data) on a compromised or shared workstation | Scoped tightly (name/phone only, no vitals/diagnosis, §4.2 step 5); cleared on logout (§10 task 11, §12); explicitly flagged as the central driver behind Open Question 1 rather than silently accepted |
| Existing `Contains`-based search (`SearchPatientsQuery`, inherited from Module 2) does not benefit from the standard single-column indexes on `FullName`/`PhoneNumber` for substring matches, since leading-wildcard `LIKE` patterns can't use a standard b-tree index | Search could slow down as patient volume grows, risking R7's "near-instant" feel | Not fixed by this module (a Module 2 concern); flagged here since this module is the one that makes search *global* and therefore more heavily used; recommend Module 2's owners revisit indexing (e.g., a trigram/full-text index) if real-world volume or doctor feedback indicates a problem — no speculative change made without evidence |
| Adding a global search widget to `AppShellComponent` increases that component's responsibility (already owns nav, logout, and the inactivity timer) | Could make the shell component harder to reason about/test if the search widget isn't isolated | Recommend factoring the widget into its own `SearchWidgetComponent` (§10 task 2, §11) rather than inlining all markup/logic directly into the shell, keeping the shell a thin composition root |
| Deep-linking `/patients?query=` (new) could clash with any future query-parameter usage on that route | Low — no other feature currently reads route/query params on `patients-list.component` | Verified by regression test (§13) that the grid behaves identically to today when `query` is absent; parameter name chosen (`query`) matches the existing `PatientService.list({ query })` option name for consistency |

---

## Open Questions — Requiring Product Owner / Architect Confirmation

1. **Recently-viewed storage mechanism**: this plan recommends client-side `localStorage` (capped at 5, cleared on logout) over a new server-side per-user table, on the grounds that the BRD/Modules\07 describe this as a minimal UX convenience with no retention requirement, and the app is single-user/single-workstation in Phase 1 scope. Confirm this approach, or state a preference for a small server-side table instead (fully specified in §6/§7 as a documented fallback) — e.g., if the doctor is expected to routinely use more than one browser/device and expects "recent patients" to follow them there.

---

## Dependencies Recap (for sequencing awareness)

This module sits seventh in the fixed build order (Authentication → Patient Management → Appointment Management → Consultation & Clinical Records → Prescription & Medication Management → Patient History → **Search & Navigation** → Data Export → Data Backup & Reliability → Administration). Modules 1, 2, 4, 5, and 6 are already built and merged; this module takes no new upstream dependency and adds no new schema by default — it is purely a UI/navigation layer that closes three concrete gaps (global reach, recent patients, one missing cross-nav link) against work already done. Data Export (Module 8) is the nominal downstream consumer per the module breakdown but does not require any new capability this module introduces to proceed.
