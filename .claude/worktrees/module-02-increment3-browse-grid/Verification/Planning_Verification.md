# Planning Verification — Requirement Coverage & Traceability

**Documents reviewed:** `.claude\agents\planning-agent.md` (planning contract/design) and `Modules\*.md` (the module decomposition planning is built on)
**Baseline:** `BRD\Doc_BRD_Final.md`
**Reviewed by:** Verification pass (planning-phase alignment review)
**Review date:** 2026-08-21
**Purpose:** Confirm that the project's planning documentation fully and correctly covers every business and functional requirement in the BRD, with two-way traceability (requirement → module, module → requirement), and surface gaps, assumptions, dependencies, risks, and inconsistencies before implementation begins.

---

## 0. Scope & Method Note (Important)

This workspace currently contains:
- `Modules\Application_Module_Breakdown.md` + `Modules\01`–`10_*.md` — the approved module decomposition, each with a "Related BRD Requirements / User Stories" section (BRD traceability), Dependencies, and Priority.
- `.claude\agents\planning-agent.md` — the agent contract that defines *how* per-module technical plans (`Planning\NN_*_Plan.md`) are to be produced (14-section template: Business Requirements, Workflows, Architecture, DB Entities, APIs, UI, Dependencies, Tasks, File Structure, Security, Test Strategy, Acceptance Criteria, Risks).

**No `Planning\` folder exists yet in this workspace** — the individual per-module technical plans (`Planning\NN_*_Plan.md`) referenced by `planning-agent.md` have not been generated here. This review therefore validates BRD-to-module traceability at the level that concretely exists today (`Modules\*.md`, the direct input to planning) and validates that `planning-agent.md`'s design, if executed, would carry that traceability forward correctly. It does **not** review any individual module's Architecture/DB/API/Test content, because none has been produced in this workspace.

**Action implied:** run `planning-agent` per module to generate `Planning\NN_*_Plan.md` files; a follow-up verification pass should then validate those documents' internal technical content (schema correctness, API completeness, test coverage) against this traceability baseline.

---

## 1. Requirement Coverage — Full Traceability Matrix

Every BRD requirement, mapped to the module(s) that own it, per `Modules\*.md`'s own stated traceability (not re-derived — cross-checked against source).

### 1.1 Scope Items (BRD § Scope)

| BRD Scope Item | Owning Module(s) | Coverage |
|---|---|---|
| Web-based access (browser-based) | Cross-cutting (all modules; Compatibility NFR) | ✅ Implicit, not owned by a single module |
| Patient registration and profile management | 2 — Patient Management | ✅ Covered |
| Appointment scheduling and tracking | 3 — Appointment Management | ✅ Covered |
| Recording patient complaints (symptoms) | 4 — Consultation & Clinical Records | ✅ Covered |
| Diagnosis documentation | 4 — Consultation & Clinical Records | ✅ Covered |
| Medication and prescription management | 5 — Prescription & Medication Management | ✅ Covered |
| Printable prescriptions (header/footer/content) | 5 — Prescription & Medication Management | ✅ Covered |
| Mandatory vitals capture (temperature, BP, pulse) | 4 — Consultation & Clinical Records | ✅ Covered |
| Patient visit history tracking | 6 — Patient History | ✅ Covered |
| Basic search functionality | 7 — Search & Navigation | ✅ Covered |
| Data export (CSV/PDF) | 8 — Data Export | ✅ Covered |

**Result: 11/11 Scope items map to an owning module. No orphaned Scope item.**

### 1.2 Functional Requirements (BRD § Functional Requirements)

| BRD Functional Requirement | Owning Module | Coverage |
|---|---|---|
| Add, edit, view patient details | 2 — Patient Management | ✅ Covered |
| Capture Name, Age/DOB, Gender, Contact details | 2 — Patient Management | ✅ Covered |
| Search patients by name or phone number | 2 — Patient Management (basic) / 7 — Search & Navigation (quick/partial) | ✅ Covered — split ownership documented (see 3.2 note) |
| Schedule appointments | 3 — Appointment Management | ✅ Covered |
| View daily appointment list | 3 — Appointment Management | ✅ Covered |
| Update appointment status (4 states) | 3 — Appointment Management | ✅ Covered |
| Overlap warning, not a hard block | 3 — Appointment Management | ✅ Covered |
| Vitals capture (Temp/BP/Pulse), "not recorded" allowed | 4 — Consultation & Clinical Records | ✅ Covered |
| Complaints (free text) | 4 — Consultation & Clinical Records | ✅ Covered |
| Diagnosis notes | 4 — Consultation & Clinical Records | ✅ Covered |
| Add medicines (Name/Dosage/Frequency/Duration/Instructions) | 5 — Prescription & Medication Management | ✅ Covered |
| Generate printable prescription (header/patient/vitals/diagnosis/meds/footer) | 5 — Prescription & Medication Management | ✅ Covered |
| Header/footer fixed/hardcoded, not UI-editable | 5 — Prescription & Medication Management / 10 — Administration (negative scope confirmation) | ✅ Covered — reinforced in two modules, consistent |
| View previous visits | 6 — Patient History | ✅ Covered |
| Access vitals/complaints/diagnosis/prescriptions per visit | 6 — Patient History | ✅ Covered |
| Filter history by date | 6 — Patient History | ✅ Covered |
| Quick patient search | 7 — Search & Navigation | ✅ Covered |
| Partial match, scoped to patient records only | 7 — Search & Navigation | ✅ Covered |
| View recent patients | 7 — Search & Navigation | ✅ Covered |
| Easy navigation between profile and visits | 7 — Search & Navigation | ✅ Covered |
| Export patient/visit data as CSV/PDF | 8 — Data Export | ✅ Covered |
| Export manual only, per-patient/per-visit, no bulk/scheduled | 8 — Data Export | ✅ Covered |

**Result: 22/22 Functional Requirement statements map to an owning module. No orphaned functional requirement.**

### 1.3 Non-Functional Requirements (BRD § Non-Functional Requirements)

| BRD NFR | Owning Module(s) | Coverage |
|---|---|---|
| Usability — simple, minimal, fast-entry UI | 2, 3, 4, 6 (each has a "Non-Functional Notes" line citing Usability) | ⚠️ Partially covered — see Gap G1 |
| Performance — page load < 2s | *(none)* | ❌ Gap — see Gap G2 |
| Performance — fast search/retrieval (2–5s) | 2 — Patient Management, 6 — Patient History, 7 — Search & Navigation | ✅ Covered |
| Reliability — no data loss | 9 — Data Backup & Reliability | ✅ Covered |
| Reliability — daily backups, 30-day retention | 9 — Data Backup & Reliability | ✅ Covered |
| Security — secure single-user login | 1 — Authentication & Authorization | ✅ Covered |
| Security — pre-provisioned account, password recovery | 1 — Authentication & Authorization | ✅ Covered |
| Security — inactivity session timeout, no concurrent-session handling | 1 — Authentication & Authorization | ✅ Covered |
| Security — encryption at rest and in transit | 1 (credentials) + 9 — Data Backup & Reliability (data/backups) | ✅ Covered — split ownership, consistent |
| Scalability — single clinic, moderate volume | *(none)* | ❌ Gap — see Gap G3 |
| Compatibility — Chrome/Edge/Safari | *(none)* | ❌ Gap — see Gap G4 |

**Result: 7/11 NFR statements have a clear owning module; 4 are cross-cutting NFRs with no explicit module ownership (expected for true cross-cutting concerns, but currently undocumented anywhere — see Gaps).**

### 1.4 Success Criteria (BRD § Success Criteria)

| Success Criterion | Traceable to a Module? | Notes |
|---|---|---|
| Consultation record within 2–3 minutes | ✅ 4 — Consultation & Clinical Records (cites this explicitly) | Covered |
| Search/history retrieval within 2–5 seconds | ✅ 2, 6, 7 (all cite this) | Covered |
| ≥80% reduction in paper usage | ❌ Not referenced in any module | Gap — see G5 (also flagged in prior BRD discovery review as not objectively testable) |
| Smooth generation/printing of prescriptions | ✅ 5 — Prescription & Medication Management (cites this explicitly) | Covered |
| Successful CSV/PDF export | ✅ 8 — Data Export (cites this explicitly) | Covered |
| High usability, minimal training | ❌ Not referenced in any module | Gap — see G5 |

**Result: 4/6 Success Criteria are explicitly traced in a module doc; 2 are business-outcome/qualitative criteria with no module owner (consistent with the earlier discovery-phase finding that these two are not objectively testable as worded).**

### 1.5 Out-of-Scope Boundary — Leakage Check

Scanned all `Modules\*.md` for Billing, Insurance, AI-diagnosis, Offline, Mobile app, Multi-doctor/clinic, Receptionist/multi-user, Follow-up alerts/reminders. **Result: zero leakage.** Every occurrence found is a deliberate, explicit exclusion statement (e.g., Module 3 "Automated reminders/alerts to patients — explicitly out of scope"; Module 2 "Insurance / billing identifiers — billing is out of scope entirely"; Module 10's entire scope is defined by *what the BRD excludes*). No module plans, implies, or requires an Out-of-Scope capability.

---

## 2. Module-by-Module Summary

| # | Module | BRD Sections Traced | Priority (matches BRD emphasis?) | Dependency Chain Valid? |
|---|---|---|---|---|
| 1 | Authentication & Authorization | NFR-Security (3 bullets) | High — ✅ correctly foundational | ✅ No upstream; correctly gates all others |
| 2 | Patient Management | Scope, Functional Req. (all Patient Mgmt bullets) | High — ✅ correct, anchor entity | ✅ Depends only on Module 1 |
| 3 | Appointment Management | Functional Req. (all Appointment bullets) | High — ✅ correct | ✅ Depends on 1, 2 |
| 4 | Consultation & Clinical Records | Functional Req. (Vitals/Complaints/Diagnosis), Success Criteria | High — ✅ correct, EMR core | ✅ Depends on 1, 2, 3 |
| 5 | Prescription & Medication Management | Scope, Functional Req. (Medication/Prescription), Success Criteria | High — ✅ correct | ✅ Depends on 2, 4 |
| 6 | Patient History | Functional Req. (Patient History), Success Criteria | High — ✅ correct | ✅ Depends on 2, 4, 5 |
| 7 | Search & Navigation | Functional Req. (Search & Navigation), NFR-Performance | Medium — ✅ reasonable (UX layer, not core data) | ✅ Depends on 2, 6 (both confirmed in source doc) |
| 8 | Data Export | Scope, Functional Req. (Data Export), Success Criteria | Medium — ✅ reasonable | ✅ Depends on 2, 6 |
| 9 | Data Backup & Reliability | NFR-Reliability, NFR-Security (encryption) | Medium — ✅ reasonable (infra, not user-facing) | ✅ Cross-cutting over 2–5, correctly has no downstream dependents |
| 10 | Administration | Users/Stakeholders, Out of Scope (multi-user/multi-clinic) | Low — ✅ correct, intentionally minimal | ✅ Depends only on 1 |

**All 10 modules have valid, internally-consistent dependency chains** — no module depends on one that would be built later than it in the Recommended Development Order, and no circular dependency exists.

---

## 3. Gaps

| ID | Gap | Impact | Recommendation |
|---|---|---|---|
| **G1** | Usability NFR is referenced piecemeal in 4 of 10 modules' "Non-Functional Notes" (2, 3, 4, 6) but has no single owning module or consolidated checklist — Modules 1, 5, 7, 8, 9, 10 are silent on it. | A developer implementing Module 5 (Prescription) or 7 (Search) has no explicit reminder that Usability applies there too, even though both are consultation-critical, fast-entry screens. | Add a short Usability line to the remaining modules' Non-Functional Notes, or (better) maintain one cross-module Usability checklist (as `planning-agent.md`'s aggregate output already does when multiple modules are planned together) so it isn't lost per-module. |
| **G2** | Performance NFR "page load < 2 seconds" is not referenced in any `Modules\*.md` file. | Risk that page-load performance is only tested informally/late, since no module explicitly owns it. | Treat as a cross-cutting, application-wide NFR — verify explicitly during integration/QA across all screens, not attributed to one module. |
| **G3** | Scalability NFR ("single clinic, moderate patient volume") is not referenced in any module. | Data-volume assumptions (e.g., index design, pagination thresholds) may be made ad hoc per module without a shared baseline number. | Define a concrete target volume (patients, visits/year) once — a Product Owner decision already flagged as an open BRD ambiguity in the prior discovery review — and reference it from any module doing data-volume-sensitive design (2, 6, 7). |
| **G4** | Compatibility NFR (Chrome/Edge/Safari) is not referenced in any module. | Browser-compatibility testing could be skipped or done inconsistently per module, especially print rendering (Module 5) and file downloads (Module 8), which are the most browser-sensitive features. | Explicitly call out cross-browser testing in Modules 5 and 8 at minimum when their technical plans are written; treat as a release-wide QA gate otherwise. |
| **G5** | Two Success Criteria ("≥80% paper reduction," "high usability, minimal training") are not owned by any module and are not objectively testable as worded (consistent with `Verification\BrainStorming_Verification.md` § 3.3, which flagged this at the discovery stage and was never subsequently resolved). | These criteria cannot be verified at release/acceptance time by any module's Acceptance Criteria — they will silently fall through unmeasured. | Reword with measurable thresholds/methods, or explicitly relabel as post-launch business-outcome metrics rather than release acceptance criteria — this decision is still outstanding from the earlier discovery review and should be closed before or during planning. |
| **G6** | No `Planning\NN_*_Plan.md` documents exist yet in this workspace (see § 0). | The technical depth `planning-agent.md` promises (DB entities, APIs, security, test strategy, acceptance criteria) has not actually been produced/verified for any module. | Run `planning-agent` per module (following the Recommended Development Order) to produce the actual `Planning\` documents, then run a follow-up technical-content verification pass. |

---

## 4. Assumptions Carried Forward

These are not defects — they are decisions `Modules\*.md` and `planning-agent.md` correctly flag as open, inherited from the earlier BRD discovery review, that will need to be resolved before or during per-module technical planning:

1. **Patient identity/uniqueness** — no dedup rule defined; Module 2 treats this as an accepted Phase 1 limitation.
2. **Age vs. DOB source of truth** — undefined which is authoritative; Module 2 flags DOB as the likely preferred field, pending Product Owner confirmation.
3. **Appointment slot duration/end-time model** — undefined; Module 3 flags this as required before overlap-detection logic can be built.
4. **Vitals units/format** (°F vs °C, mmHg) — undefined; not explicitly flagged in Module 4, should be added when its technical plan is written.
5. **Password recovery delivery mechanism** (email vs. admin-assisted) — undefined; Module 1 flags this as a blocking decision for Sprint 1.
6. **Session inactivity-timeout duration** — undefined; Module 1 does not specify a number, should be resolved during its technical plan.
7. **"Recent patients" list size/recency definition** — undefined; Module 7 does not specify a number.
8. **Export field composition** (which fields appear in CSV vs. PDF) — undefined; Module 8 flags this as needing Product Owner confirmation.

All eight originate from `Verification\BrainStorming_Verification.md`'s discovery-phase findings and remain open — none has been closed by a `Doc_BRD_Clarifications.md` entry (which still does not exist in this workspace).

---

## 5. Dependency Validation

- The dependency edges stated in each `Modules\NN_*.md`'s own "Dependencies" section were cross-checked against `planning-agent.md`'s embedded Module Dependency Graph. They now match exactly, including the Patient History (6) → Search & Navigation (7) edge that was previously missing from the graph and has since been corrected (see `Verification\Planning_Verification.md` history / this document supersedes that narrower check).
- The Recommended Development Order in `planning-agent.md` (1→2→3→4→5→6→7→8→9→10) is consistent with every module's stated upstream dependencies — no module is scheduled before a module it depends on.
- Module 9 (Data Backup & Reliability) correctly has no downstream dependents and is positioned late enough to finalize after the schema (Modules 2–5) stabilizes, per its own module doc's stated rationale.
- Module 10 (Administration) correctly depends only on Module 1 and has no downstream dependents.

**No dependency inconsistencies found.**

---

## 6. Risks

| Risk | Source | Likelihood/Impact | Mitigation |
|---|---|---|---|
| Cross-cutting NFRs (Performance page-load, Scalability, Compatibility) have no owning module, so may be deprioritized or forgotten during module-by-module implementation. | Gaps G2–G4 | Medium likelihood / Medium impact — these are release-blocking quality bars, not features, so they're easy to defer silently. | Maintain (or reinstate) a cross-module NFR checklist verified at integration/regression testing — the same idea `planning-agent.md` already applies when planning multiple modules together, but it should not depend on multi-module sessions to exist. |
| Two Success Criteria are unmeasurable as worded (G5) and have sat unresolved since the discovery-phase review. | Gap G5 | Low likelihood of blocking a specific module, but high impact on being able to declare "Phase 1 done" against the BRD's own success bar. | Close this specific open item with the Product Owner before final UAT sign-off, not after. |
| No `Planning\` documents exist yet (G6); the deeper technical risks (schema soundness, API completeness, security implementation, test coverage) are entirely unverified at this stage. | Gap G6 | Certain — this is a known, expected state, not a surprise, but worth stating plainly. | Treat this document as a pre-planning gate: proceed to run `planning-agent` module-by-module, then re-verify. |
| Split ownership of "search patients by name or phone" across Modules 2 (basic) and 7 (quick/partial) could cause duplicated or divergent search logic if not coordinated during technical planning. | § 1.2 note | Low-medium — both modules already cross-reference each other's dependency, but the actual query logic hasn't been designed yet. | When planning Modules 2 and 7, explicitly decide (and document once) which module owns the actual search implementation the other consumes, to avoid two separate search code paths. |

---

## 7. Inconsistencies Found

- **None material.** Priorities, dependencies, and BRD traceability in `Modules\*.md` are internally consistent with each other and with `planning-agent.md`'s embedded summary of them (the one previously-found graph inconsistency — the missing Patient History → Search & Navigation edge — has already been corrected in `planning-agent.md`, per the change history of this verification).
- **Minor terminology note (not a defect):** BRD's Functional Requirements list "Search patients by name or phone number" once under Patient Management and again, with more detail, under Search & Navigation. `Modules\02` and `Modules\07` both correctly acknowledge and cross-reference this rather than treating it as two separate requirements — flagged here only so the split ownership is visible in one place (see Risk table, last row) rather than because it's wrong.

---

## 8. Recommendations

1. **Close the four cross-cutting NFR gaps (G1–G4)** by adding a standing "Cross-Module NFR Checklist" — Usability, Performance (page load), Scalability, Compatibility — that every module's technical plan must reference or explicitly opt out of, rather than leaving these to be picked up incidentally by 4 of 10 modules.
2. **Resolve G5 with the Product Owner** — either reword the two unmeasurable Success Criteria with concrete thresholds, or formally relabel them as qualitative/business-outcome goals separate from Phase 1 release acceptance criteria. This has been open since the discovery-phase review and should not carry further into implementation.
3. **Generate the missing `Planning\NN_*_Plan.md` documents** by invoking `planning-agent` per module, following the Recommended Development Order (Authentication → Patient Management → Appointment Management → Consultation → Prescription → Patient History → Search & Navigation → Data Export → Backup & Reliability → Administration).
4. **Resolve the eight carried-forward assumptions (§ 4)** — most are quick, low-controversy Product Owner decisions — before or during each module's technical planning, so `planning-agent` doesn't have to repeatedly flag the same open item across multiple module plans.
5. **Decide search-ownership split (Modules 2 vs. 7)** explicitly during their technical planning to avoid duplicated query logic.
6. Once `Planning\*.md` documents exist, run a second, deeper verification pass focused on technical content (schema correctness, API completeness, security implementation detail, test coverage against each module's Acceptance Criteria) — this document intentionally stops at the traceability/coverage level available today.

---

## 9. Scope Impact

None. Every finding in this review is a coverage/traceability/process gap within the already-approved Phase 1 module set (`Modules\Application_Module_Breakdown.md`, Modules 1–10). No Out-of-Scope BRD item is implicated, and no change to the BRD's Scope or Out-of-Scope sections is required. The only structural action implied is completing the planning artifacts (`Planning\`) that `planning-agent.md` is designed to produce but that do not yet exist in this workspace.

---

## 10. Sign-Off Checklist

| Item | Status |
|---|---|
| Every BRD Scope item maps to an owning module | ✅ Pass (11/11) |
| Every BRD Functional Requirement maps to an owning module | ✅ Pass (22/22) |
| Every BRD Non-Functional Requirement maps to an owning module or is explicitly flagged as cross-cutting/unowned | ⚠️ Partial — 7/11 owned, 4 flagged as gaps (G2–G4, plus partial G1) |
| Every BRD Success Criterion maps to an owning module or is explicitly flagged as unmeasurable | ⚠️ Partial — 4/6 owned, 2 flagged as gaps (G5) |
| No Out-of-Scope BRD item appears in any module's planned functionality | ✅ Pass |
| All module dependency chains are valid and non-circular | ✅ Pass |
| All module priorities align with BRD emphasis (core clinical workflow = High) | ✅ Pass |
| Per-module technical plans (`Planning\NN_*_Plan.md`) exist and have been verified | ❌ Not yet started — see Gap G6 |

**Overall status: Conditional Pass.** The module decomposition and its BRD traceability are sound and ready to plan from. Four NFR gaps and one Success-Criteria gap should be closed (or explicitly deferred with Product Owner sign-off) before Phase 1 UAT, and the actual `Planning\` documents still need to be produced and separately verified.
