import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/appointments/appointment.service';
import { APPOINTMENT_STATUSES, Appointment, AppointmentStatus } from '../../../core/appointments/appointments.models';

/**
 * Daily Appointment List — the doctor's "start of day" screen (approved plan §7). Defaults to
 * today; prev/next-day controls and a date picker navigate other days. Status changes are
 * applied in place (no full list re-fetch) to satisfy the one/two-click fast-interaction NFR (R9).
 */
@Component({
  selector: 'app-appointments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './appointments-list.component.html',
  styleUrl: './appointments-list.component.scss'
})
export class AppointmentsListComponent implements OnInit {
  private readonly appointmentService = inject(AppointmentService);

  readonly statuses = APPOINTMENT_STATUSES;

  selectedDate = toIsoDate(new Date());
  appointments: Appointment[] = [];
  loading = true;
  error = false;

  /** Tracks in-flight status updates per row id, so a failure surfaces inline without losing
   * the row (rather than a global error banner replacing the whole list). */
  statusUpdateErrorId: number | null = null;

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.appointments.length === 0;
  }

  ngOnInit(): void {
    this.fetch();
  }

  onDateChange(value: string): void {
    this.selectedDate = value;
    this.fetch();
  }

  previousDay(): void {
    this.selectedDate = shiftIsoDate(this.selectedDate, -1);
    this.fetch();
  }

  nextDay(): void {
    this.selectedDate = shiftIsoDate(this.selectedDate, 1);
    this.fetch();
  }

  retry(): void {
    this.fetch();
  }

  onStatusChange(appointment: Appointment, status: AppointmentStatus): void {
    if (status === appointment.status) {
      return;
    }

    this.statusUpdateErrorId = null;
    const previousStatus = appointment.status;
    appointment.status = status;

    this.appointmentService.updateStatus(appointment.id, status).subscribe({
      next: (updated) => {
        appointment.status = updated.status;
      },
      error: () => {
        appointment.status = previousStatus;
        this.statusUpdateErrorId = appointment.id;
      }
    });
  }

  private fetch(): void {
    this.loading = true;
    this.error = false;

    this.appointmentService.listByDate(this.selectedDate).subscribe({
      next: (appointments) => {
        this.loading = false;
        this.appointments = appointments;
      },
      error: () => {
        this.loading = false;
        this.error = true;
      }
    });
  }
}

/**
 * Formats using local date parts (not toISOString(), which converts to UTC and can shift the
 * calendar date depending on the browser's timezone offset — a real bug caught during testing).
 */
function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function shiftIsoDate(iso: string, deltaDays: number): string {
  const [year, month, day] = iso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  date.setDate(date.getDate() + deltaDays);
  return toIsoDate(date);
}
