# Module 8: Data Export

## 1. Overview
Data Export gives the doctor a way to take patient or visit data outside the application — for example, to share a record with another provider, keep a personal copy, or satisfy a patient's request for their records. The BRD deliberately keeps this feature narrow: manual, per-record exports only, with no bulk or scheduled export capability.

## 2. Purpose
Allow controlled, on-demand extraction of patient or visit data in common formats (CSV, PDF), supporting the BRD's success criterion of "successful export of data in CSV/PDF format" while avoiding the complexity of bulk data handling.

## 3. Scope
### In Scope (Phase 1)
- Export a single patient's data as CSV or PDF
- Export a single visit's data as CSV or PDF
- Manual, user-triggered export only

### Out of Scope (Phase 1)
- Bulk "export all patients" functionality
- Scheduled or automatic export jobs
- Export formats other than CSV and PDF (e.g., HL7, FHIR — not mentioned in BRD)

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Export Patient (CSV) | Export a selected patient's profile (and optionally summarized history) as a CSV file. |
| 2 | Export Patient (PDF) | Export a selected patient's profile/summary as a formatted PDF. |
| 3 | Export Visit (CSV) | Export a single visit's structured data (vitals, complaints, diagnosis, medications) as CSV. |
| 4 | Export Visit (PDF) | Export a single visit's data as a formatted PDF (distinct from the prescription-only print in Module 5, though may share rendering logic). |

## 5. Business Rules
- Export is always **manual and scoped to one patient or one visit at a time** — there is no "select multiple" or "export all" action in Phase 1.
- No export is triggered automatically by any system event (e.g., end of day); the doctor must explicitly request each export.
- Exported files should exclude any data outside the scope of what BRD defines as patient/visit data (no cross-patient aggregation).

## 6. Related BRD Requirements / User Stories
- *Scope*: "Data export (CSV/PDF)."
- *Functional Requirements → Data Export*: "Export patient or visit data as: CSV, PDF"; "Export is manual only, per-patient or per-visit; no bulk 'export all' and no scheduled/automatic export."
- *Success Criteria*: "Successful export of data in CSV/PDF format."

**User Story:** *As the clinic doctor, I want to export a patient's record as a PDF, so that I can share it with a specialist or keep an external copy.*

**User Story:** *As the clinic doctor, I want to export a single visit's data as CSV, so that I can analyze or archive it outside the application.*

## 7. Data Considerations
- No new persistent entity required; this module reads from `Patient`, `Visit`, and `Prescription`/`Medication` data and renders it into CSV or PDF on demand.
- Export generation should reuse the same data-access logic as Patient History and Prescription rendering to avoid duplicating formatting logic.

## 8. Dependencies
- **Depends on:** Patient Management, Patient History (source of the data being exported).
- **Depended on by:** None (terminal/output-only module).

## 9. Priority
**Medium** — required for a BRD success criterion, but the core clinical workflow (registration → appointment → consultation → prescription) functions independently of export.

## 10. Acceptance Criteria
- Doctor can export a selected patient's data as a valid, correctly formatted CSV file.
- Doctor can export a selected patient's data as a valid, correctly formatted PDF file.
- Doctor can export a single visit's data in both CSV and PDF formats.
- No UI path exists for exporting multiple patients/visits at once or scheduling a recurring export.

## 11. Non-Functional Notes
- Export generation should not meaningfully block the UI; for larger PDFs, an async/progress indicator may be appropriate, though volumes are expected to be small given the single-clinic scale.
