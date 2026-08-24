# Application Module Breakdown

*Based on analysis of BRD_Final — a single-user, single-clinic, web-based Patient Management Application (Phase 1 scope).*

## Module 1: Authentication & Authorization
- **Purpose:** Secure single-user login and session control; no self-registration since only one pre-provisioned doctor account exists.
- **Key Functionalities:**
  - Login with pre-provisioned credentials (no sign-up flow)
  - Password recovery/reset flow
  - Inactivity-based session timeout (no concurrent-session handling)
  - Enforce HTTPS/TLS for data in transit
- **Related BRD Requirements/User Stories:** Non-Functional Requirements → Security: "Secure login (single user authentication)," "Account is pre-provisioned... password recovery flow included," "Simple inactivity-based session timeout only"
- **Dependencies on Other Modules:** None (foundational — all other modules depend on this)
- **Priority:** High

---

## Module 2: Patient Management
- **Purpose:** Central repository of patient demographic and contact information, the anchor entity all clinical data attaches to.
- **Key Functionalities:**
  - Add / edit / view patient profile
  - Capture Name, Age/DOB, Gender, Contact details
  - Search patients by name or phone number
- **Related BRD Requirements/User Stories:** Functional Requirements → Patient Management (all bullets); Scope → "Patient registration and profile management"
- **Dependencies on Other Modules:** Authentication & Authorization
- **Priority:** High

---

## Module 3: Appointment Management
- **Purpose:** Schedule and track daily patient appointments and their status through the visit lifecycle.
- **Key Functionalities:**
  - Schedule new appointments
  - Daily appointment list view
  - Status updates: Scheduled / Completed / Cancelled / No-show
  - Overlap detection with a soft warning (double-booking still allowed/saved)
- **Related BRD Requirements/User Stories:** Functional Requirements → Appointment Management (all bullets)
- **Dependencies on Other Modules:** Patient Management (appointments belong to a patient); Authentication & Authorization
- **Priority:** High

---

## Module 4: Consultation & Clinical Records (EMR core)
- **Purpose:** Capture the structured clinical record of each visit — vitals, complaints, diagnosis — forming the core electronic medical record.
- **Key Functionalities:**
  - Mandatory vitals capture: Temperature, BP, Pulse (each field can be explicitly marked "not recorded" without blocking save)
  - Free-text complaints/symptoms entry
  - Diagnosis notes entry
  - Consultation saved as a single visit record linked to a patient
- **Related BRD Requirements/User Stories:** Functional Requirements → Consultation Workflow: Vitals Capture, Complaints, Diagnosis; Success Criteria: "complete a consultation record within 2–3 minutes"
- **Dependencies on Other Modules:** Patient Management, Appointment Management (a consultation is typically tied to a scheduled/walk-in visit)
- **Priority:** High

---

## Module 5: Prescription & Medication Management
- **Purpose:** Record prescribed medications for a visit and generate a printable prescription document.
- **Key Functionalities:**
  - Add medicines: Name, Dosage, Frequency, Duration, Instructions
  - Generate printable prescription combining clinic/doctor header, patient details, vitals, diagnosis, medications, and footer
  - Header/footer content is fixed/hardcoded (not admin-editable in Phase 1)
- **Related BRD Requirements/User Stories:** Functional Requirements → Medication/Prescription (all bullets); Scope → "Printable prescriptions"
- **Dependencies on Other Modules:** Consultation & Clinical Records (needs vitals/diagnosis for the document), Patient Management
- **Priority:** High

---

## Module 6: Patient History
- **Purpose:** Give the doctor a chronological, filterable view of a patient's past visits for quick reference during consultations.
- **Key Functionalities:**
  - List previous visits per patient
  - View vitals, complaints, diagnosis, prescriptions per past visit
  - Filter visit history by date
- **Related BRD Requirements/User Stories:** Functional Requirements → Patient History; Success Criteria: "Patient search and history retrieval within 2–5 seconds"
- **Dependencies on Other Modules:** Patient Management, Consultation & Clinical Records, Prescription & Medication Management
- **Priority:** High

---

## Module 7: Search & Navigation
- **Purpose:** Provide fast, low-friction lookup of patients and smooth movement between related screens during a live consultation.
- **Key Functionalities:**
  - Quick patient search with partial-match support (scoped to patient records only, not visit/diagnosis text)
  - Recently-viewed patients list
  - Cross-navigation between patient profile and visit records
- **Related BRD Requirements/User Stories:** Functional Requirements → Search & Navigation; Non-Functional → Performance: "Fast patient search and retrieval"
- **Dependencies on Other Modules:** Patient Management, Patient History
- **Priority:** Medium

---

## Module 8: Data Export
- **Purpose:** Allow the doctor to export individual patient or visit records for external use (sharing, backup, records).
- **Key Functionalities:**
  - Export a single patient's data as CSV or PDF
  - Export a single visit's data as CSV or PDF
  - Manual, on-demand only — no bulk "export all," no scheduling
- **Related BRD Requirements/User Stories:** Functional Requirements → Data Export; Success Criteria: "Successful export of data in CSV/PDF format"
- **Dependencies on Other Modules:** Patient Management, Patient History
- **Priority:** Medium

---

## Module 9: Data Backup & Reliability
- **Purpose:** Guarantee no data loss through automated, retained backups — an infrastructure/ops-facing module rather than a doctor-facing screen.
- **Key Functionalities:**
  - Automated daily backup job
  - 30-day retention policy
  - Encrypted storage at rest
- **Related BRD Requirements/User Stories:** Non-Functional Requirements → Reliability, Security ("Data encryption at rest and in transit")
- **Dependencies on Other Modules:** Cuts across all data-holding modules (Patient Management, Consultation, Prescription)
- **Priority:** Medium

---

## Module 10: Administration (Minimal, Phase 1 scope)
- **Purpose:** Very limited system-level configuration; explicitly not a full admin module since the BRD excludes multi-user/multi-clinic support.
- **Key Functionalities:**
  - Manage the single pre-provisioned doctor account (credentials, password recovery)
  - No editable clinic branding/header-footer UI in Phase 1 (hardcoded)
- **Related BRD Requirements/User Stories:** Users and Stakeholders → "Primary Users: General Physician (Single User)"; Out of Scope → "Receptionist or multi-user access," "Multi-doctor or multi-clinic support"
- **Dependencies on Other Modules:** Authentication & Authorization
- **Priority:** Low

---

## Modules Explicitly Excluded from Phase 1
Per BRD "Out of Scope," the following are **not** built in this release and should not appear in the Phase 1 architecture: Billing & Payments, Insurance Processing, Lab/Pharmacy Integration, AI-based Diagnosis, Offline Mode, Mobile App, Advanced Analytics/Reporting Dashboard, Multi-user/Receptionist Access, Multi-doctor/Multi-clinic Support, Follow-up Alerts/Reminders, Audit & Logging (not mentioned in BRD — no audit trail requirement stated).

---

# 1. Module Dependency Flow

```
Authentication & Authorization
        │
        ▼
 Patient Management ──────────────┐
        │                          │
        ▼                          ▼
Appointment Management     Search & Navigation
        │
        ▼
Consultation & Clinical Records (EMR core)
        │
        ▼
Prescription & Medication Management
        │
        ▼
   Patient History ───────► Data Export

Data Backup & Reliability — cross-cutting (wraps all data modules)
Administration — thin layer on top of Authentication
```

# 2. Recommended Development Order

1. Authentication & Authorization
2. Patient Management
3. Appointment Management
4. Consultation & Clinical Records
5. Prescription & Medication Management
6. Patient History
7. Search & Navigation
8. Data Export
9. Data Backup & Reliability
10. Administration

Rationale: each stage unblocks the next in the natural clinical workflow (login → find/register patient → schedule visit → run consultation → prescribe → review history), with cross-cutting concerns (export, backup, admin) layered in once the core record-keeping loop works end-to-end.

# 3. MVP Modules
Everything the doctor needs to run a full consultation and leave with a printed prescription — this is essentially the whole Phase 1 scope, since the BRD is already tightly trimmed to an MVP:
- Authentication & Authorization
- Patient Management
- Appointment Management
- Consultation & Clinical Records
- Prescription & Medication Management
- Patient History
- Search & Navigation (basic)

# 4. Deferred / Lower-Priority for Later Iterations Within Phase 1
- Data Export (CSV/PDF) — needed for success criteria but can trail the core consultation loop
- Data Backup & Reliability — an ops task that can be finalized once schema stabilizes
- Administration (password recovery, account settings) — needed before go-live but not for early internal demos

# 5. Future Enhancement Modules (explicitly Out of Scope per BRD, for post-Phase-1 roadmap)
- Multi-user Access / Receptionist Role
- Billing & Invoicing
- Insurance Processing
- Lab/Pharmacy Integrations
- AI-assisted Diagnosis/Recommendations
- Offline Mode
- Mobile Application
- Advanced Analytics & Reporting Dashboard
- Multi-doctor / Multi-clinic Support
- Follow-up Alerts & Reminders
- Audit & Logging (for compliance, if regulatory scope expands later)

# 6. High-Level System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Browser (Chrome/Edge/Safari)          │
│        Single-page/responsive web UI, optimized for       │
│        fast data entry during live consultations          │
└───────────────────────────┬────────────────────────────┘
                            │ HTTPS
┌───────────────────────────▼────────────────────────────┐
│                    Application Server                     │
│  ┌───────────────┐ ┌──────────────┐ ┌─────────────────┐ │
│  │ Auth Module    │ │ Patient Mgmt │ │ Appointment Mgmt │ │
│  └───────────────┘ └──────────────┘ └─────────────────┘ │
│  ┌───────────────────────┐ ┌────────────────────────┐   │
│  │ Consultation / EMR Core│ │ Prescription Module     │   │
│  └───────────────────────┘ └────────────────────────┘   │
│  ┌───────────────┐ ┌──────────────┐ ┌─────────────────┐ │
│  │ Patient History│ │ Search Engine│ │ Export Service   │ │
│  │                │ │ (partial-match)│ (CSV/PDF gen)    │ │
│  └───────────────┘ └──────────────┘ └─────────────────┘ │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                Encrypted Relational Database               │
│   Patients | Appointments | Visits | Vitals | Diagnoses    │
│   Prescriptions | Medications | Users (single account)     │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│         Automated Daily Backup Job (30-day retention)     │
└─────────────────────────────────────────────────────────┘

Cross-cutting: PDF/print rendering service (prescriptions + exports),
TLS everywhere, encryption at rest, session-timeout middleware.
```

This is a monolithic single-tenant architecture — appropriate given the BRD's explicit single-doctor, single-clinic, moderate-volume scope, with no need for microservices, message queues, or multi-tenant isolation at this stage.
