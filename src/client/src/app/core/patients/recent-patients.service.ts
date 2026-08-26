import { Injectable } from '@angular/core';
import { RecentPatient } from './recent-patients.models';

const STORAGE_KEY = 'pma_recent_patients';
const MAX_RECENT = 5;

/**
 * Client-side "recently viewed patients" store (Module 7, Open Question 1 resolved:
 * `localStorage`, not a new server-side table — see Planning\07_Search_and_Navigation_Plan.md
 * §5). Single-user, single-workstation Phase 1 scope; no clinical data ever recorded (§4.2
 * step 5). Capped at the last 5 entries, most-recent-first, de-duplicated by patient id.
 */
@Injectable({ providedIn: 'root' })
export class RecentPatientsService {
  /**
   * Upserts a patient at the front of the list. If the patient is already present, it is moved
   * to the front (its `viewedAt` refreshed) rather than duplicated. Trims beyond MAX_RECENT.
   */
  record(patient: { id: number; fullName: string; phoneNumber: string }): void {
    const existing = this.list().filter((entry) => entry.id !== patient.id);
    const updated: RecentPatient = {
      id: patient.id,
      fullName: patient.fullName,
      phoneNumber: patient.phoneNumber,
      viewedAt: new Date().toISOString()
    };

    const next = [updated, ...existing].slice(0, MAX_RECENT);
    this.save(next);
  }

  /** Reads the stored list, most-recent-first. Returns an empty list on missing/corrupted data. */
  list(): RecentPatient[] {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) {
        return [];
      }
      return parsed.filter(isRecentPatient);
    } catch {
      return [];
    }
  }

  /** Clears the recent list — wired into logout (§10 task 11) so data does not persist across a
   * session boundary on a shared/public machine. */
  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
  }

  private save(entries: RecentPatient[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(entries));
  }
}

function isRecentPatient(value: unknown): value is RecentPatient {
  if (!value || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate['id'] === 'number' &&
    typeof candidate['fullName'] === 'string' &&
    typeof candidate['phoneNumber'] === 'string' &&
    typeof candidate['viewedAt'] === 'string'
  );
}
