# Module 6: Patient History

## 1. Overview
Patient History is the "read side" of the clinical record — it lets the doctor look back at everything previously documented for a patient. It aggregates data produced by the Consultation and Prescription modules into a chronological, filterable timeline, giving the doctor fast context before or during a new visit.

## 2. Purpose
Provide quick, structured access to a patient's past visits so the doctor can make informed decisions in the current consultation without digging through paper files — directly addressing the BRD Problem Statement's "slow patient lookup and history tracking."

## 3. Scope
### In Scope (Phase 1)
- List of a patient's previous visits
- View vitals, complaints, diagnosis, and prescriptions for each past visit
- Filter visit history by date

### Out of Scope (Phase 1)
- Trend charts/graphs of vitals over time (not mentioned in BRD; Advanced Analytics is explicitly out of scope)
- Cross-patient history search or reporting
- Editing historical visit records from the history view (edits would occur in the originating module, not addressed by BRD)

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Visit List | Show all past visits for a selected patient in chronological order. |
| 2 | Visit Detail View | Drill into a specific past visit to see its vitals, complaints, diagnosis, and prescription. |
| 3 | Date Filter | Narrow the visit list to a specific date or date range. |

## 5. Business Rules
- History is strictly patient-scoped — there is no aggregate/multi-patient history view in Phase 1.
- Every visit created in Module 4 (and its associated prescription from Module 5) automatically appears in this history; no separate "publish to history" step exists.
- Filtering by date operates on the visit date, not on complaint/diagnosis text (that kind of full-text scoping is explicitly excluded — see Module 7 rules).

## 6. Related BRD Requirements / User Stories
- *Functional Requirements → Patient History*: "View previous visits"; "Access: Vitals, Complaints, Diagnosis, Prescriptions"; "Filter by date."
- *Success Criteria*: "Patient search and history retrieval within 2–5 seconds."

**User Story:** *As the clinic doctor, I want to see a patient's past visits at a glance, so that I understand their medical background before starting today's consultation.*

**User Story:** *As the clinic doctor, I want to filter a patient's history by date, so that I can quickly find a specific past visit (e.g., "the visit from last month").*

## 7. Data Considerations
- **Entities:** No new entity required — this module is primarily a read/query layer over `Visit`, `Prescription`, and `Medication` records already created in Modules 4 and 5, filtered by `patient_id` and optionally by date range.
- Performance consideration: visit lists should be indexed by `patient_id` and `visit_date` to meet the 2–5 second retrieval target.

## 8. Dependencies
- **Depends on:** Patient Management (history is scoped to a patient), Consultation & Clinical Records (source of vitals/complaints/diagnosis), Prescription & Medication Management (source of prescription data).
- **Depended on by:** Data Export (a visit or patient's history can be exported), Search & Navigation (navigation flows from search results into history).

## 9. Priority
**High** — directly supports a core BRD success criterion and the central problem statement (fast history retrieval).

## 10. Acceptance Criteria
- Opening a patient's profile shows a correctly ordered list of their past visits.
- Selecting a visit displays its full vitals, complaints, diagnosis, and prescription details.
- Applying a date filter correctly narrows the visible visit list.
- History for a patient with a typical visit volume loads within 2–5 seconds.

## 11. Non-Functional Notes
- Should follow the same minimal, fast-entry-oriented UI style even though it is primarily a read view, for consistency with Usability requirements.
