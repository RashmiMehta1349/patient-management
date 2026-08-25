# Module 1: Authentication & Authorization

## 1. Overview
The Authentication & Authorization module is the entry-point gate of the application. Because the BRD defines a **single physician user** with no receptionist or multi-user access in Phase 1, this module is intentionally lightweight: there is no role hierarchy, no permission matrix, and no self-registration. Its job is simply to verify that the person accessing the system is the pre-provisioned doctor, keep the session secure while idle, and let that doctor recover access if credentials are lost.

Despite its simplicity, this module is foundational — every other module sits behind it, and the application should not render any clinical screen without a valid, active session.

## 2. Purpose
- Confirm the identity of the single authorized user before exposing any patient data.
- Protect against unauthorized access to sensitive health information.
- Provide a safe, low-friction way to regain access after a forgotten password, without building a full user-management system.

## 3. Scope
### In Scope (Phase 1)
- Login screen (username/email + password)
- Password recovery / reset flow (e.g., email-based reset link or admin-issued reset)
- Inactivity-based session timeout
- Logout
- Enforcing HTTPS/TLS on all requests carrying credentials or patient data

### Out of Scope (Phase 1)
- User registration / sign-up
- Multiple user accounts or roles (e.g., receptionist, admin, nurse)
- Concurrent-session detection or device management
- Two-factor authentication (not mentioned in BRD; can be a future enhancement)
- OAuth/SSO integrations

## 4. Key Functionalities
| # | Functionality | Description |
|---|----------------|--------------|
| 1 | Login | Doctor enters credentials; system validates against the pre-provisioned account and issues a session/token. |
| 2 | Password Recovery | Doctor requests a reset (e.g., via registered email); system issues a secure, time-limited reset link or code. |
| 3 | Password Reset | Doctor sets a new password via the recovery flow; old sessions are invalidated. |
| 4 | Session Timeout | After a period of inactivity, the session expires automatically and the user must log in again. |
| 5 | Logout | Manual termination of the active session. |
| 6 | Transport Security | All authentication traffic and subsequent API calls occur over HTTPS. |

## 5. Business Rules
- Exactly one user account exists in Phase 1; there is no in-app account creation UI.
- The account is pre-provisioned by the development/deployment team, not by the end user.
- Session timeout is time-based only (no concurrent-session conflict handling is required).
- Passwords must never be stored or transmitted in plain text (hashed at rest, encrypted in transit).
- A password reset must invalidate any previously issued session tokens for security.

## 6. Related BRD Requirements / User Stories
- *Non-Functional Requirements → Security*: "Secure login (single user authentication)."
- *Non-Functional Requirements → Security*: "Account is pre-provisioned (no self-registration); password recovery flow is included in Phase 1."
- *Non-Functional Requirements → Security*: "Simple inactivity-based session timeout only; no concurrent-session handling needed."
- *Non-Functional Requirements → Security*: "Data encryption (at rest and in transit)."

**User Story:** *As the clinic doctor, I want to log in securely and be automatically logged out if I step away, so that patient data stays protected even if I forget to sign out.*

**User Story:** *As the clinic doctor, I want to reset my password if I forget it, so that I'm never permanently locked out of my own patient records.*

## 7. Data Considerations
- **Entities:** `User` (single record) — fields: user ID, email/username, hashed password, password-reset token, token expiry, last-login timestamp, last-activity timestamp.
- No audit/log table is required by the BRD (audit logging is explicitly out of scope).

## 8. Dependencies
- **Depends on:** None — this is the foundational module.
- **Depended on by:** Patient Management, Appointment Management, Consultation & Clinical Records, Prescription & Medication Management, Patient History, Search & Navigation, Data Export, Administration.

## 9. Priority
**High** — required before any other module can be used; without it, no screen in the application can be safely exposed.

## 10. Acceptance Criteria
- Given valid credentials, the doctor can log in and reach the application dashboard.
- Given invalid credentials, login is rejected with a generic error message (no hint on which field is wrong).
- After the configured inactivity period, the session expires and the next action redirects to login.
- The doctor can request a password reset and successfully set a new password without developer intervention.
- All requests to the application occur over HTTPS; no credentials or patient data travel unencrypted.

## 11. Non-Functional Notes
- Login and session checks should not noticeably affect the < 2 second page-load target.
- Encryption at rest applies to the stored password hash and any session/reset tokens.
