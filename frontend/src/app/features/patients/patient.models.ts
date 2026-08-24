export type Gender = 'Male' | 'Female' | 'Other';

export const GENDER_OPTIONS: Gender[] = ['Male', 'Female', 'Other'];

export interface CreatePatientRequest {
  fullName: string;
  dateOfBirth: string | null;
  approxAgeAtEntry: number | null;
  gender: Gender | '';
  phoneNumber: string;
  email?: string | null;
  address?: string | null;
}

export interface UpdatePatientRequest {
  fullName: string;
  dateOfBirth: string | null;
  approxAgeAtEntry: number | null;
  gender: Gender | '';
  phoneNumber: string;
  email?: string | null;
  address?: string | null;
}

export interface PatientResponse {
  id: string;
  patientCode: string;
  fullName: string;
  dateOfBirth: string | null;
  approxAgeAtEntry: number | null;
  entryDate: string | null;
  gender: Gender;
  phoneNumber: string;
  email: string | null;
  address: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface DuplicateCheckResponse {
  possibleDuplicate: boolean;
  existingPatientId: string | null;
  existingPatientCode: string | null;
}

/** Field-level validation error shape returned by the API on 400 responses. */
export interface FieldError {
  field: string;
  message: string;
}

export interface ValidationErrorResponse {
  message: string;
  errors: FieldError[];
}
