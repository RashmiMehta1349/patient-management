# Module 7: Search & Navigation

## 1. Overview
Search & Navigation is the connective tissue of the application's UX. Rather than owning its own data, it provides a fast lookup layer over Patient Management and a set of navigation conveniences (recent patients, cross-links between profile and visits) that keep the doctor moving quickly between screens during a live consultation.

## 2. Purpose
Minimize the time and clicks needed to get from "I need to see this patient" to "I'm looking at their record," supporting the BRD's usability goal of a UI optimized for fast data entry and retrieval during consultations.

## 3. Scope
### In Scope (Phase 1)
- Quick patient search with partial-match support
- Search scoped to patient records only (name, phone) — not visit history or diagnosis text
- Recently-viewed patients list
- Easy navigation between a patient's profile and their visit records

### Out of Scope (Phase 1)
- Full-text search across complaints/diagnosis/prescriptions (explicitly excluded by BRD scoping decision)
- Global/system-wide search across appointments or other entities
- Search filters/facets beyond partial name/phone match

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Quick Search | A persistent/global search box that matches patients by partial name or phone number as the doctor types. |
| 2 | Recent Patients | Show a short list of recently accessed patients for one-click return access. |
| 3 | Cross-Navigation | Provide clear links/breadcrumbs between a patient's profile, their appointments, and their visit history. |

## 5. Business Rules
- Search matching is **partial** (substring/prefix match), not exact-match only.
- Search is explicitly **scoped to patient records only** — it must not search inside visit history, diagnosis notes, or prescription text. This is a deliberate BRD decision to keep search simple and fast.
- "Recently viewed" is a UX convenience with no formal retention/audit requirement in the BRD (kept minimal — e.g., last N patients viewed in the current or recent sessions).

## 6. Related BRD Requirements / User Stories
- *Functional Requirements → Search & Navigation*: "Quick patient search"; "Search supports partial match, scoped to patient records only (not visit history/diagnosis text)"; "View recent patients"; "Easy navigation between patient profile and visits."
- *Non-Functional Requirements → Performance*: "Fast patient search and retrieval."

**User Story:** *As the clinic doctor, I want to type a few letters of a patient's name and see matching results instantly, so that I don't have to remember the exact spelling.*

**User Story:** *As the clinic doctor, I want to see my recently viewed patients, so that I can quickly get back to someone I was just looking at.*

## 7. Data Considerations
- No dedicated entity beyond a lightweight "recently viewed" list (could be session-based or a small per-user table of `patient_id` + `viewed_at`).
- Search queries the `Patient` entity's name and phone fields; may use a database index or lightweight search index for partial-match performance.

## 8. Dependencies
- **Depends on:** Patient Management (source of searchable data), Patient History (navigation target).
- **Depended on by:** All clinical workflows that begin with "find the patient" (Appointment, Consultation, Prescription).

## 9. Priority
**Medium** — significantly improves usability and speed but the application can technically function with a more basic patient list/search in an early build.

## 10. Acceptance Criteria
- Typing a partial name or phone number returns matching patients without requiring an exact match.
- Search results never include matches based on diagnosis, complaint, or prescription text.
- A recently viewed list is visible and clicking an entry navigates directly to that patient's profile.
- Navigation between a patient's profile and their appointments/visits requires no more than one or two clicks.

## 11. Non-Functional Notes
- Search response time should feel near-instant (sub-second perceived latency) to support the "fast patient search and retrieval" non-functional requirement.
