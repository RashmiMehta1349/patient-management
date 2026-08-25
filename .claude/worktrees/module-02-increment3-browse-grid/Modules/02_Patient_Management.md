# Module 2: Patient Management

## 1. Overview
Patient Management is the anchor module of the entire application. Every appointment, consultation, prescription, and history record ultimately hangs off a `Patient` entity created here. It gives the doctor a simple way to register new patients, keep their demographic details current, and quickly find an existing patient — the first step in almost every workflow in the app.

## 2. Purpose
Maintain an accurate, easily searchable directory of patients so the doctor can register new patients in seconds and pull up an existing patient's profile without friction, reducing the paper-based lookup delays described in the BRD's Problem Statement.

## 3. Scope
### In Scope (Phase 1)
- Create (register) a new patient
- Edit/update an existing patient's details
- View a patient's profile
- Search patients by name or phone number

### Out of Scope (Phase 1)
- Merging duplicate patient records (not mentioned in BRD)
- Patient self-service portal or patient-facing login
- Multi-clinic patient sharing
- Insurance / billing identifiers (billing is out of scope entirely)

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Add Patient | Capture Name, Age/DOB, Gender, and Contact details to create a new patient profile. |
| 2 | Edit Patient | Update any captured field when patient details change (e.g., new phone number). |
| 3 | View Patient | Display the full profile as the entry point into appointments, consultations, and history. |
| 4 | Search Patient | Look up patients by name or phone number to quickly locate an existing profile. |

## 5. Business Rules
- Name, Age/DOB, Gender, and Contact details are the minimum required fields to register a patient (per BRD scope).
- Patient search in this module is a simple lookup by name/phone; the more general "quick search with partial match" behavior is implemented in the Search & Navigation module but queries this module's data.
- A patient record, once created, persists indefinitely (no deletion workflow specified in BRD) — patients accumulate visit history over time.

## 6. Related BRD Requirements / User Stories
- *Scope*: "Patient registration and profile management."
- *Functional Requirements → Patient Management*: "Add, edit, and view patient details"; "Capture: Name, Age/DOB, Gender, Contact details"; "Search patients by name or phone number."

**User Story:** *As the clinic doctor, I want to register a new patient in under a minute, so that I can move quickly into the consultation without paperwork delays.*

**User Story:** *As the clinic doctor, I want to update a patient's contact number, so that my records stay accurate for future visits.*

**User Story:** *As the clinic doctor, I want to search by name or phone number, so that I can find a returning patient instantly.*

## 7. Data Considerations
- **Entities:** `Patient` — fields: patient ID, full name, date of birth or age, gender, phone number, (optionally address/email if the team chooses to extend beyond the minimum BRD fields), created date, last-updated date.
- All other clinical modules (Appointment, Consultation, Prescription, History) store a foreign key reference to `Patient.patient_id`.

## 8. Dependencies
- **Depends on:** Authentication & Authorization (must be logged in to access patient data).
- **Depended on by:** Appointment Management, Consultation & Clinical Records, Prescription & Medication Management, Patient History, Search & Navigation, Data Export.

## 9. Priority
**High** — this is the foundational data entity; nearly every other module cannot function without it.

## 10. Acceptance Criteria
- Doctor can create a new patient with the four required fields and the record is immediately retrievable.
- Doctor can edit any field on an existing patient and the changes persist.
- Searching by full or partial name, or by phone number, returns the correct matching patient(s).
- Patient profile view surfaces enough information to identify the correct person before starting a consultation.

## 11. Non-Functional Notes
- Patient search/retrieval should complete within the 2–5 second target defined in Success Criteria.
- Simple, minimal UI for fast entry, consistent with the BRD's Usability requirement.
