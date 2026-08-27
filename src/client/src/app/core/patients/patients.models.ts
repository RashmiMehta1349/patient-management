/** Fixed picklist, per the approved plan's resolved Open Question #1. */
export type PatientGender = 'Male' | 'Female' | 'Other';

export const PATIENT_GENDERS: PatientGender[] = ['Male', 'Female', 'Other'];

export interface CreatePatientRequest {
  fullName: string;
  /** ISO 8601 date string (yyyy-MM-dd), as produced by an HTML date input. */
  dateOfBirth: string;
  gender: PatientGender | '';
  /** International dial code, e.g. "+91". Stored as its own DB column. */
  countryCode: string;
  /** National number only (no dial code), max 10 digits. */
  phoneNumber: string;
}

/** Same four-field shape as CreatePatientRequest — full-payload PUT per the approved plan §4. */
export type UpdatePatientRequest = CreatePatientRequest;

/** Generic paged response envelope matching the backend's PagedResultDto<T> (§9b.1/§9b.5). */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface Patient {
  id: number;
  fullName: string;
  dateOfBirth: string;
  /** Computed server-side on read from dateOfBirth — never sent on create. */
  age: number;
  gender: PatientGender;
  countryCode: string;
  phoneNumber: string;
  createdAt: string;
  updatedAt: string;
}
