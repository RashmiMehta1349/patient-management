/**
 * Vitals are unit-agnostic (Temperature: free decimal, Blood Pressure: free-form string e.g.
 * "120/80") — no structured/coded validation (approved plan §4, product decision). Each vital
 * carries an explicit NotRecorded flag rather than inferring "not recorded" from a null value.
 */
/** A single prescribed medicine line item (Module 5). Name is the one required field per row when
 * any field in the row has been touched — see ValidateMedications (server) / consultation form
 * (client) for the shared "touched row needs all 5 fields" rule. */
export interface Medication {
  name: string;
  dosage?: string | null;
  frequency?: string | null;
  duration?: string | null;
  instructions?: string | null;
}

export interface CreateVisitRequest {
  patientId: string;
  appointmentId?: string;
  temperatureValue?: number | null;
  temperatureNotRecorded: boolean;
  bloodPressureValue?: string | null;
  bloodPressureNotRecorded: boolean;
  pulseValue?: number | null;
  pulseNotRecorded: boolean;
  complaints?: string;
  diagnosis?: string;
  medications?: Medication[];
}

/** Vitals/complaints/diagnosis/medications only — PatientId/AppointmentId are not changeable via edit. */
export interface UpdateVisitRequest {
  temperatureValue?: number | null;
  temperatureNotRecorded: boolean;
  bloodPressureValue?: string | null;
  bloodPressureNotRecorded: boolean;
  pulseValue?: number | null;
  pulseNotRecorded: boolean;
  complaints?: string;
  diagnosis?: string;
  medications?: Medication[];
}

export interface Visit {
  id: string;
  patientId: string;
  /** Hydrated server-side for display — not a client-side join. */
  patientName: string;
  appointmentId: string | null;
  visitDate: string;
  temperatureValue: number | null;
  temperatureNotRecorded: boolean;
  bloodPressureValue: string | null;
  bloodPressureNotRecorded: boolean;
  pulseValue: number | null;
  pulseNotRecorded: boolean;
  complaints: string | null;
  diagnosis: string | null;
  createdAt: string;
  updatedAt: string;
  /** Module 5 — ordered, empty (not null) when no medicines were prescribed. */
  medications: Medication[];
}
