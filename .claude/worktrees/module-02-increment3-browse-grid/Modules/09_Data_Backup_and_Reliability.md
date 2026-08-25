# Module 9: Data Backup & Reliability

## 1. Overview
Unlike the other modules, Data Backup & Reliability has no doctor-facing UI — it is an operational/infrastructure capability that runs behind the scenes to guarantee the BRD's "no data loss" promise. It exists because the application is replacing paper records; losing digital data would be strictly worse than the paper system it replaces, so reliability is treated as a first-class non-functional requirement rather than an afterthought.

## 2. Purpose
Ensure that all patient, appointment, consultation, and prescription data is durably preserved through automated backups, protecting the clinic against data loss from hardware failure, accidental deletion, or corruption.

## 3. Scope
### In Scope (Phase 1)
- Automated daily backup job covering all application data
- 30-day backup retention policy
- Encrypted storage of data at rest (including backups)

### Out of Scope (Phase 1)
- Doctor-facing backup/restore UI (not mentioned in BRD; likely an ops/admin task, not an end-user feature)
- Point-in-time recovery beyond daily granularity
- Geo-redundant/multi-region backup (not specified; may be an infrastructure decision beyond BRD scope)
- Formal disaster-recovery runbook (not requested in BRD, though good practice to define at implementation time)

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Automated Daily Backup | A scheduled job takes a full backup of the application database at least once per day without manual intervention. |
| 2 | 30-Day Retention | Backups older than 30 days are rotated out automatically. |
| 3 | Encryption at Rest | All stored data, including backups, is encrypted. |

## 5. Business Rules
- Backups run automatically; no manual trigger is required for routine operation (though an ops-only manual trigger may be useful for maintenance, it is not a BRD requirement).
- Retention is fixed at 30 days — backups beyond that window are not required to be kept per BRD.
- No specific regulatory/compliance standard (e.g., HIPAA) is targeted per the BRD; encryption is implemented as general best practice, not for formal compliance certification.

## 6. Related BRD Requirements / User Stories
- *Non-Functional Requirements → Reliability*: "No data loss"; "Automated daily backups, 30-day retention."
- *Non-Functional Requirements → Security*: "Data encryption (at rest and in transit) as a general best-practice expectation... no specific regulatory/compliance standard targeted."

**User Story (Stakeholder/Ops framing):** *As the clinic owner, I want my patient data automatically backed up every day, so that a system failure never results in losing clinical records.*

## 7. Data Considerations
- No new application-facing entity; this module operates at the infrastructure/database layer (e.g., automated DB snapshot or dump job, encrypted object storage for backup files).
- Applies uniformly across all data-holding modules: Patient Management, Appointment Management, Consultation & Clinical Records, Prescription & Medication Management.

## 8. Dependencies
- **Depends on:** All data-producing modules (it backs up their combined data store).
- **Depended on by:** None directly user-facing, but underpins the Reliability guarantee the whole product relies on.

## 9. Priority
**Medium** — not visible to the doctor day-to-day, but essential before production go-live; can be finalized once the core data schema stabilizes (per Recommended Development Order).

## 10. Acceptance Criteria
- A backup job runs automatically on a daily schedule without manual intervention.
- Backups older than 30 days are automatically removed.
- Backup files are stored encrypted, matching the encryption-at-rest standard applied to the live database.
- A restore-from-backup drill successfully reconstructs the application data set (verification step recommended even though not explicitly required by BRD wording).

## 11. Non-Functional Notes
- Backup jobs should be scheduled to avoid impacting the application's performance targets (e.g., run during low-usage hours).
- Encryption keys/credentials for backups should be managed securely and separately from the application's regular access credentials.
