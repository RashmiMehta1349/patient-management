# Module 4: Consultation & Clinical Records (EMR Core)

## 1. Overview
This module is the clinical heart of the application — it is where the doctor actually documents what happens during a visit: vitals, the patient's complaints, and the diagnosis. Together with the Prescription module, it forms the digital equivalent of the paper consultation note the BRD is designed to replace. Every consultation is captured as one "visit" record tied to a patient (and typically to an appointment).

## 2. Purpose
Provide a fast, structured way to record the clinical details of a consultation so the doctor spends minimal time on data entry and maximal time with the patient — directly supporting the BRD's success criterion of completing a consultation record in 2–3 minutes.

## 3. Scope
### In Scope (Phase 1)
- Mandatory vitals capture: Temperature, Blood Pressure, Pulse — with an explicit "not recorded" option per field
- Free-text complaints/symptoms entry
- Diagnosis notes entry
- Saving the consultation as a single visit record linked to the patient

### Out of Scope (Phase 1)
- Structured/coded diagnosis (e.g., ICD-10 lookup) — BRD specifies free-text diagnosis notes only
- AI-assisted diagnosis or recommendations (explicitly out of scope)
- Lab result integration (explicitly out of scope)
- Templates/macros for common complaints (not mentioned in BRD; possible future enhancement)

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Vitals Capture | Record Temperature, BP, and Pulse for the visit; any field can be marked "not recorded" instead of a value. |
| 2 | Complaints Entry | Free-text field for the patient's reported symptoms. |
| 3 | Diagnosis Entry | Free-text field for the doctor's diagnosis notes. |
| 4 | Save Visit Record | Persist vitals + complaints + diagnosis as one consultation record tied to the patient (and appointment, if applicable). |

## 5. Business Rules
- Vitals are conceptually mandatory (a field must exist and be addressed for every consultation) but are **not a hard validation block** — the doctor can explicitly flag a vital as "not recorded" and still save the consultation.
- Complaints and diagnosis are plain free text; no required minimum length or structured coding.
- A consultation record cannot exist without a linked patient.
- Once saved, the consultation becomes part of the patient's permanent visit history (feeds Module 6).

## 6. Related BRD Requirements / User Stories
- *Functional Requirements → Consultation Workflow → Vitals Capture (Mandatory)*: "Record for every consultation: Temperature, Blood Pressure, Pulse"; "If a vital isn't measured... allow marking the field as 'not recorded'; the consultation can still be saved (not a hard block)."
- *Functional Requirements → Complaints*: "Enter patient symptoms (free text)."
- *Functional Requirements → Diagnosis*: "Record diagnosis notes."
- *Success Criteria*: "Doctor can complete a consultation record within 2–3 minutes."

**User Story:** *As the clinic doctor, I want to quickly enter vitals, complaints, and diagnosis in one screen, so that I can finish documenting a consultation in a couple of minutes.*

**User Story:** *As the clinic doctor, I want to mark a vital as "not recorded" when I didn't measure it, so that I'm not blocked from saving the consultation.*

## 7. Data Considerations
- **Entities:** `Visit`/`Consultation` — fields: visit ID, patient ID (FK), appointment ID (FK, optional), visit date/time, temperature (value or "not recorded"), blood pressure (value or "not recorded"), pulse (value or "not recorded"), complaints (text), diagnosis (text), created timestamp.
- This entity is the parent record that the Prescription module attaches medications to, and that the Patient History module lists and displays.

## 8. Dependencies
- **Depends on:** Patient Management (visit must reference a patient), Appointment Management (visit is typically tied to a scheduled/walk-in appointment), Authentication & Authorization.
- **Depended on by:** Prescription & Medication Management (prescription is generated from this visit's data), Patient History (visits are the core history entries).

## 9. Priority
**High** — this is the core clinical documentation capability the entire application exists to digitize.

## 10. Acceptance Criteria
- Doctor can enter Temperature, BP, and Pulse, or mark any of them "not recorded," and successfully save.
- Doctor can enter free-text complaints and diagnosis and have them persist with the visit record.
- A saved consultation is immediately visible in that patient's history (Module 6) and available to Prescription (Module 5).
- The full flow — open patient, enter vitals/complaints/diagnosis, save — can realistically be completed in 2–3 minutes by a doctor familiar with the UI.

## 11. Non-Functional Notes
- Form should be optimized for keyboard-first, minimal-click data entry per the Usability requirement.
- Consultation data must be encrypted at rest and in transit, and included in the daily backup (Module 9).
