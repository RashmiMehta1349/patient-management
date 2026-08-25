/** Fixed picklist — no additional sub-statuses (Modules\03 §5). */
export type AppointmentStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';

export const APPOINTMENT_STATUSES: AppointmentStatus[] = ['Scheduled', 'Completed', 'Cancelled', 'NoShow'];

export interface CreateAppointmentRequest {
  patientId: string;
  /** ISO 8601 date string (yyyy-MM-dd). */
  appointmentDate: string;
  /** 24-hour "HH:mm" string. */
  appointmentTime: string;
  notes?: string;
}

/** Date/time/notes only — reschedule flow (Increment 3); PatientId is not changeable via edit. */
export interface UpdateAppointmentRequest {
  appointmentDate: string;
  appointmentTime: string;
  notes?: string;
}

export interface UpdateAppointmentStatusRequest {
  status: AppointmentStatus;
}

export interface ConflictingAppointment {
  id: string;
  patientName: string;
  appointmentTime: string;
}

export interface Appointment {
  id: string;
  patientId: string;
  /** Hydrated server-side for display — not a client-side join. */
  patientName: string;
  appointmentDate: string;
  appointmentTime: string;
  status: AppointmentStatus;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  /** Advisory only — never blocks the save (approved plan §3.4/§4). */
  hasOverlapWarning: boolean;
  conflictingAppointments: ConflictingAppointment[];
}
