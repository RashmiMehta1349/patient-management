# Module 3: Appointment Management

## 1. Overview
Appointment Management lets the doctor plan and track the day's patient flow. It bridges Patient Management and the clinical consultation workflow — an appointment is typically the trigger that brings a patient into a visit. The BRD explicitly allows flexible, real-world clinic behavior such as walk-ins and intentional double-booking, so this module favors soft warnings over hard blocks.

## 2. Purpose
Give the doctor a simple, reliable way to schedule patient visits, see what the day looks like at a glance, and keep an accurate status of each appointment as it moves through the visit lifecycle.

## 3. Scope
### In Scope (Phase 1)
- Schedule a new appointment for a patient
- View a daily appointment list
- Update appointment status: Scheduled, Completed, Cancelled, No-show
- Detect and warn on overlapping appointments, while still allowing the save

### Out of Scope (Phase 1)
- Automated reminders/alerts to patients (explicitly out of scope in BRD)
- Multi-doctor calendar or resource scheduling
- Recurring appointment series
- Online/patient-initiated booking

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Schedule Appointment | Create an appointment tied to an existing patient, with date/time. |
| 2 | Daily Appointment List | View all appointments for a selected day, typically the doctor's primary daily working view. |
| 3 | Status Update | Transition an appointment through Scheduled → Completed / Cancelled / No-show. |
| 4 | Overlap Detection | When a new appointment's time overlaps an existing one, display a warning but permit the doctor to save anyway (supports walk-ins/double-booking). |

## 5. Business Rules
- Overlap warnings are advisory only — never a hard validation block, since the BRD explicitly supports intentional double-booking for walk-ins.
- Every appointment must be linked to a patient record (no anonymous/placeholder appointments described in BRD).
- Status is a simple enum with four values; no additional sub-statuses are defined.
- There is no reminder/notification triggered by appointment status changes (Notifications/reminders are explicitly out of scope).

## 6. Related BRD Requirements / User Stories
- *Functional Requirements → Appointment Management*: "Schedule appointments"; "View daily appointment list"; "Update appointment status: Scheduled, Completed, Cancelled, No-show"; "If a new appointment overlaps an existing one, warn the doctor of the conflict but allow it to be saved."

**User Story:** *As the clinic doctor, I want to see today's appointments in one list, so that I know who is coming in and in what order.*

**User Story:** *As the clinic doctor, I want to be warned (not blocked) when I double-book a time slot, so that I can still fit in an urgent walk-in.*

**User Story:** *As the clinic doctor, I want to mark an appointment as completed, cancelled, or no-show, so that my daily record accurately reflects what happened.*

## 7. Data Considerations
- **Entities:** `Appointment` — fields: appointment ID, patient ID (FK), date, time, status (Scheduled/Completed/Cancelled/No-show), created date, notes (optional).
- Overlap detection logic queries existing appointments for the same date/time window before save.

## 8. Dependencies
- **Depends on:** Patient Management (an appointment must reference a patient), Authentication & Authorization.
- **Depended on by:** Consultation & Clinical Records (a consultation is typically initiated from a Scheduled/checked-in appointment).

## 9. Priority
**High** — core daily workflow entry point for the doctor's clinic day.

## 10. Acceptance Criteria
- Doctor can create an appointment against an existing patient with a date and time.
- The daily list correctly filters to show only that day's appointments, in time order.
- Changing status updates the record and reflects immediately in the daily list.
- Creating an appointment that overlaps an existing time slot shows a warning message but still allows saving upon confirmation.

## 11. Non-Functional Notes
- The daily appointment list should load within the < 2 second page-load target.
- UI should make status changes a one- or two-click action to support fast consultations (Usability requirement).
