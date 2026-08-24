# BRD Verification — Discovery Review

**Document reviewed:** `BRD\Doc_BRD_Final.md`
**Reviewed by:** Brainstorming Agent (discovery-phase review)
**Review date:** 2026-08-20
**Purpose:** Pre-implementation-planning gap/ambiguity/conflict check. This document does not modify the BRD; it is a standalone verification record.

---

## 1. What Was Reviewed

The full finalized BRD, section by section:
Product Goal, Users and Stakeholders, Problem Statement, Scope, Out of Scope, Success Criteria, Functional Requirements (Patient Management, Appointment Management, Consultation Workflow — Vitals/Complaints/Diagnosis/Medication-Prescription, Patient History, Search & Navigation, Data Export), Non-Functional Requirements (Usability, Performance, Reliability, Security, Scalability, Compatibility), and Open Questions.

No separate `Doc_BRD_Clarifications.md` file exists at this time — the BRD states clarifications have already been merged into `Doc_BRD_Final.md` directly (vitals "not recorded," appointment overlap warning, fixed prescription header/footer, manual export scope, backups, pre-provisioned account, best-effort encryption, inactivity timeout).

---

## 2. Checks Performed

- Internal consistency across sections (Scope vs. Functional Requirements vs. Out of Scope)
- Ambiguity scan for undefined validation rules, data types/formats, and UI behaviors that would block a developer
- Out-of-Scope items cross-checked against Functional/Non-Functional Requirements for implicit conflicts
- Success Criteria assessed for measurability/testability
- Non-Functional Requirements assessed for actionable specificity
- Sanity check on the "no open questions" claim by attempting to construct concrete implementation questions a developer would need answered

---

## 3. Findings

### 3.1 Ambiguities That Would Block a Developer

1. **Patient identity/uniqueness rule undefined.** No mention of how duplicate patients are detected or prevented (e.g., same name + phone). Without this, "Add patient" behavior when a similar record already exists is undefined — search may return duplicates, undermining the "accurate history" goal.
2. **Age / DOB field — no format or authority rule.** Both "Age" and "DOB" are listed as capture fields. Is one derived from the other, or are both independently entered (risk of contradiction, e.g., Age=40 but DOB implies 25)? Undefined which is source of truth, and what date format is used.
3. **Contact details — no field-level definition.** "Contact details" is undefined: phone only, or phone + address + email? Required vs. optional? No validation rule (e.g., phone number format/length) is specified, yet phone number is also the primary search key (Section: Patient Management, "Search patients by name or phone number") — an unspecified format makes exact/partial phone matching unpredictable.
4. **Vitals — no units, ranges, or format specified.** Temperature, Blood Pressure, and Pulse are mandatory-or-"not recorded" fields, but no units (°F/°C, mmHg), expected ranges, or free-text-vs-structured-input rule is defined. This affects both data entry UX and how "search/filter" or future analytics would interpret the values.
5. **Appointment scheduling — no defined time-slot granularity or duration model.** "Schedule appointments" and "overlap warning" are specified, but there's no stated appointment duration (fixed slot length? doctor-specified start/end?) — without this, "overlap" is undefined at the data-model level (though this BRD intentionally stays non-technical, the *business rule* of what constitutes a time range is still a product decision, not an implementation detail).
6. **Appointment status transitions unspecified.** Four statuses exist (Scheduled, Completed, Cancelled, No-show) but no rule states which transitions are valid (e.g., can a "Cancelled" appointment be reopened to "Scheduled"? Can "Completed" be reverted?).
7. **Search partial-match rules incomplete.** BRD clarifies search is "partial match, scoped to patient records only," but doesn't state which fields the partial match applies to — name only, or name + phone? (Patient Management section says search is "by name or phone number," but Search & Navigation section doesn't confirm phone is included in the partial-match scope.) This is a minor internal-consistency gap between two sections describing the same feature.
8. **"Recent patients" — no definition of recency window or list size.** Undefined how many patients are shown or what qualifies (last viewed? last visit date?).
9. **Data export field content undefined.** "Export patient or visit data as CSV/PDF" doesn't specify which fields are included in each export type, or whether CSV and PDF exports contain equivalent data (PDF likely mirrors the prescription/visit format; CSV format for structured data is unclear — single patient record row vs. one row per visit, etc.).
10. **Password recovery flow — mechanism undefined.** BRD confirms password recovery is in scope but does not state the mechanism (security question, email-based reset link, admin reset). Since this is explicitly a single pre-provisioned account with no self-registration, the recovery channel (e.g., is email even collected/available for this account?) is a genuine open item, not just an implementation detail — the BRD doesn't establish that an email/contact channel exists for the doctor's account at all.
11. **Session timeout duration unspecified.** "Simple inactivity-based session timeout" has no defined threshold (5 min? 30 min?) — a product/UX decision, not just a technical default, since it directly affects consultation workflow interruption risk during the "2-3 minute consultation record" success criterion.
12. **Backup scope undefined.** "Automated daily backups, 30-day retention" doesn't state whether this covers exported files too, or only the primary data store; also doesn't state whether backup restoration is a doctor-facing capability or an operational/admin-only process (relevant since there's no admin role defined anywhere in the BRD).

### 3.2 Internal Consistency Issues

- **Minor duplication/ambiguity between sections describing the same feature differently:** Patient Management states "Search patients by name or phone number," while Search & Navigation states search "supports partial match, scoped to patient records only." These aren't contradictory, but the second section doesn't explicitly reconfirm both fields (name and phone) are covered by partial matching, or whether phone search is exact-match only (phone numbers are sometimes intentionally treated as exact-match for precision). Recommend explicit reconciliation.
- **No conflicts found** between Out of Scope and Functional/Non-Functional Requirements. Cross-checked specifically: multi-user/receptionist access, billing, insurance, lab/pharmacy integration, AI diagnosis, offline mode, mobile app, advanced analytics, multi-doctor/clinic, and follow-up alerts/reminders — none are implied or required by any Functional or Non-Functional requirement as currently worded. This area is clean.

### 3.3 Success Criteria — Measurability Assessment

| Criterion | Measurable as written? | Notes |
|---|---|---|
| "Consultation record within 2–3 minutes" | Partially | Measurable via user timing tests, but no defined starting/ending event (does the timer start at patient selection, or at vitals entry? Ends at save, or at prescription print?). Needs an operational definition to be testable. |
| "Patient search and history retrieval within 2–5 seconds" | Yes | Matches Non-Functional Performance requirement; testable, though "moderate patient volume" (see 3.4) affects what data volume this should be tested against. |
| "At least 80% reduction in paper usage" | Difficult to test pre-launch | No baseline paper-usage measurement method defined; likely only measurable in production after adoption, not at acceptance/QA time. Should be flagged as a post-launch/business-outcome metric, not a pre-release acceptance criterion. |
| "Smooth generation and printing of prescriptions" | No | "Smooth" is subjective/qualitative; not testable without a defined acceptance check (e.g., prescription generates and renders correctly within X seconds, prints without layout errors on standard paper size). |
| "Successful export of data in CSV/PDF format" | Yes | Binary pass/fail, testable. |
| "High usability with minimal training required" | No | Subjective; no defined usability testing method (e.g., task completion rate, time-to-first-successful-use by an untrained doctor). |

**3 of 6 Success Criteria are not objectively testable as currently worded** (paper reduction, "smooth" generation, "high usability"). These should either be reworded with measurable thresholds or explicitly labeled as qualitative/aspirational goals rather than acceptance criteria.

### 3.4 Non-Functional Requirements — Completeness Gaps

- **"Moderate patient volume" (Scalability) is vague**, as flagged by the review brief itself. No concrete numbers (e.g., total patient count, visits/day, records over N years) are given. This affects performance testing (the 2-5 second search target is meaningless without a target data volume) and backup/storage planning.
- **Reliability — "No data loss"** is an absolute statement with no defined recovery point objective (RPO) or recovery time objective (RTO) beyond "daily backups, 30-day retention." Strictly, daily backups mean up to 24 hours of potential data loss is possible in a failure scenario — this is a latent contradiction between the aspirational "no data loss" statement and the concrete backup cadence, unless "no data loss" is meant to describe normal operation (not disaster recovery), which the document doesn't clarify.
- **Compatibility** lists specific browsers (Chrome, Edge, Safari) but not minimum versions, nor mobile-browser expectations (relevant since "Mobile application" is Out of Scope, but it's unclear if the *web app accessed via a mobile browser* is expected to work at all, or is also excluded).
- **Performance** defines page load and search targets but not concurrent-load expectations (though single-user context likely makes this low-risk) — acceptable gap given single-user scope, but worth an explicit one-line confirmation that concurrency is a non-concern.
- **Accessibility** is not addressed at all (no mention of keyboard navigation, screen reader support, font sizing) — may be an acceptable Phase 1 omission given single physician user, but is currently silent rather than explicitly deferred.

### 3.5 Assessment of "Open Questions: None"

The claim that there are no open questions is **not fully supported** by the review. While the BRD has clearly resolved the major product-shape decisions (via the merged clarifications), the items in Section 3.1 above (ambiguities) and Section 3.3/3.4 (measurability and NFR gaps) represent genuine decisions still needed before a developer could build against this document without making unstated assumptions. These are appropriately raised now, before implementation planning, rather than discovered mid-build.

---

## 4. New Open Questions Raised By This Review

1. What determines patient uniqueness/duplicate detection — is duplicate prevention needed, or is duplicate creation acceptable and left to the doctor's judgment?
2. Is DOB or Age the source of truth for patient records, and are both fields required or is one derived?
3. What specific fields make up "Contact details," and are any required vs. optional? What phone number format/length is expected?
4. What units and input format apply to Temperature, Blood Pressure, and Pulse (structured fields with units vs. free text)?
5. What defines an appointment's time span (fixed slot duration vs. doctor-specified start/end) for overlap detection purposes?
6. Which appointment status transitions are valid/invalid?
7. Does partial-match search apply to phone number as well as name, or is phone search exact-match only?
8. How many "recent patients" are shown, and what defines recency (last viewed vs. last visit date)?
9. What specific fields/rows are included in a CSV export vs. a PDF export, and are they expected to contain equivalent data?
10. What is the password recovery mechanism, and does the doctor's pre-provisioned account have an associated recovery channel (e.g., email) at all?
11. What is the specific inactivity timeout duration?
12. Does "no data loss" describe normal-operation durability only, or is it meant to bound disaster-recovery data loss tighter than the stated 24-hour backup cadence implies?
13. What is the target patient volume / data volume (patients, visits/year) that "moderate patient volume" and the 2-5 second search target should be validated against?
14. Is browser access from a mobile device's browser (not a native app) expected to work, or is it excluded along with "Mobile application"?
15. What is the intended operational definition (start/end trigger) for measuring the "2-3 minute consultation record" success criterion?

---

## 5. Recommendations

- Treat items 1-11 in Section 4 as a short follow-up clarification round before finalizing field-level requirements for implementation planning — most are quick, low-controversy product decisions (e.g., "phone search is exact-match," "session timeout is 15 minutes") rather than open-ended debates.
- Reword or annotate the three non-testable Success Criteria (paper reduction, "smooth" prescription generation, "high usability") to either add measurable thresholds/methods or explicitly label them as qualitative/business-outcome goals distinct from release acceptance criteria.
- Add one clarifying line to Reliability NFR reconciling "no data loss" with the daily-backup cadence, to avoid the criterion being read as a stricter guarantee than the infrastructure supports.
- Add a concrete target patient/visit volume number to the Scalability NFR so performance targets (2-5 second search) have a testable basis.
- Explicitly state whether mobile-browser access is supported or excluded, since "Mobile application" (native app) being out of scope does not by itself answer this.
- These are recommended discovery follow-ups, not implementation decisions — final direction on each remains the user's/stakeholder's call.

---

## 6. Scope Impact

None of the findings in this review require a Scope or Out-of-Scope change. All items identified are clarifications/refinements within the already-defined Phase 1 boundaries (Patient Management, Appointment Management, Consultation Workflow, Patient History, Search & Navigation, Data Export) — no new feature area is being proposed, and no Out-of-Scope item is being challenged.
