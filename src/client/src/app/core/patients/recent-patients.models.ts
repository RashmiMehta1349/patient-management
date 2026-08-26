/**
 * Minimal identifying/contact-only shape persisted for "recently viewed" (Module 7, §4.2 step 5)
 * — deliberately excludes any clinical data (vitals/diagnosis/complaints) per R2's "patient
 * records only" scoping applied by extension to this feature.
 */
export interface RecentPatient {
  id: number;
  fullName: string;
  phoneNumber: string;
  /** ISO 8601 timestamp of when this patient was last recorded as viewed. */
  viewedAt: string;
}
