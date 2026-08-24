# Module 5: Prescription & Medication Management

## 1. Overview
This module turns the doctor's clinical decision into a concrete, printable artifact: the prescription. It captures the list of medicines prescribed during a consultation and formats them, together with vitals, diagnosis, and patient details, into a document the patient can walk away with — directly replacing the handwritten prescription pad described in the BRD's Problem Statement.

## 2. Purpose
Let the doctor record prescribed medications quickly and generate a clean, printable prescription without manual formatting, supporting the BRD's success criterion of "smooth generation and printing of prescriptions."

## 3. Scope
### In Scope (Phase 1)
- Add one or more medicines to a visit, each with Name, Dosage, Frequency, Duration, and Instructions
- Generate a printable prescription combining: clinic/doctor header, patient details, vitals, diagnosis, medications, and footer
- Fixed/hardcoded clinic/doctor header and footer content (not editable via UI in Phase 1)

### Out of Scope (Phase 1)
- Drug interaction checking or dosage validation against a drug database (not mentioned in BRD)
- Pharmacy integration / e-prescription transmission (explicitly out of scope)
- Editable header/footer templates or branding customization UI
- Medication inventory or stock tracking

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Add Medicine | Capture Name, Dosage, Frequency, Duration, and Instructions for each prescribed medicine. |
| 2 | Multiple Medicines per Visit | Support adding several medicines to the same consultation's prescription. |
| 3 | Generate Printable Prescription | Compose a document with header, patient details, vitals, diagnosis, medication list, and footer. |
| 4 | Print | Trigger browser/system print of the generated prescription. |

## 5. Business Rules
- A prescription is always generated in the context of a specific visit/consultation (it pulls vitals and diagnosis from Module 4).
- Header and footer (clinic name, doctor name/credentials, signature area, basic notes) are fixed for this deployment — no admin UI to edit them in Phase 1.
- There is no limit specified in the BRD on the number of medicines per prescription; the UI should support an arbitrary list (add/remove rows).
- Each medicine entry requires Name, Dosage, Frequency, Duration, and Instructions as the defined field set — no additional fields (e.g., drug code) are in scope.

## 6. Related BRD Requirements / User Stories
- *Scope*: "Printable prescriptions (with basic header, footer, and content)."
- *Functional Requirements → Medication / Prescription*: "Add medicines with: Name, Dosage, Frequency, Duration, Instructions"; "Generate printable prescription including: Clinic/doctor header, Patient details, Vitals, Diagnosis, Medications, Footer"; "Clinic/doctor header and footer details are fixed/hardcoded for this deployment, not editable through the UI."
- *Success Criteria*: "Smooth generation and printing of prescriptions."

**User Story:** *As the clinic doctor, I want to add prescribed medicines with dosage and instructions in seconds, so that I don't slow down the consultation.*

**User Story:** *As the clinic doctor, I want to print a complete, professional-looking prescription with one click, so that the patient leaves with clear instructions.*

## 7. Data Considerations
- **Entities:** `Prescription` (1:1 or 1:many with `Visit`) and `Medication` (many per `Prescription`) — fields: prescription ID, visit ID (FK), and per medication: name, dosage, frequency, duration, instructions.
- The printable document is rendered by combining `Patient`, `Visit` (vitals + diagnosis), and `Medication` data with static header/footer content — no separate "document" entity is required.

## 8. Dependencies
- **Depends on:** Consultation & Clinical Records (needs vitals/diagnosis from the same visit), Patient Management (patient details for the header).
- **Depended on by:** Patient History (past prescriptions are shown as part of visit history), Data Export (a prescription can be exported as PDF).

## 9. Priority
**High** — directly tied to two explicit BRD success criteria (prescription generation/printing) and a core scope item.

## 10. Acceptance Criteria
- Doctor can add, edit, and remove medicine entries within a single visit's prescription before saving.
- Generated prescription document correctly includes header, patient details, vitals, diagnosis, full medication list, and footer.
- Print output is legible and properly formatted on a standard page size.
- Header/footer content matches the fixed clinic/doctor information configured at deployment, with no UI path to edit it.

## 11. Non-Functional Notes
- Print rendering should not noticeably delay the consultation workflow (supports the 2–3 minute consultation target).
- Prescription data is included in the encrypted-at-rest data set and daily backups (Module 9).
