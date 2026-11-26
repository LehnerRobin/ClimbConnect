import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminAppointment, AdminService } from '../../../services/admin.service';
import { AreasService } from '../../../services/areas.service';
import { DudesComponent } from '../../shared/dudes.component';
import { VIENNA_TZ } from '../../shared/vienna-date';

/** Eintrag für eine Filter-Auswahl (Gebiet oder Ersteller). */
interface FilterOption {
  id: number;
  name: string;
}

/** "01.08.2030, 18:00" nach Wiener Ortszeit – Termine sind in UTC gespeichert (#198). */
const dateTimeFormatter = new Intl.DateTimeFormat('de-AT', {
  timeZone: VIENNA_TZ, day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
});

/**
 * Admin: alle Termine aller User (#223).
 * Filter nach Gebiet und Ersteller, auf Wunsch auch vergangene Termine.
 * Öffnen führt zur Termin-Seite (dort auch die Pinnwand), Bearbeiten zum Termin-Formular,
 * Löschen geht direkt aus der Liste.
 */
@Component({
  selector: 'app-admin-appointments',
  standalone: true,
  imports: [FormsModule, RouterLink, DudesComponent],
  templateUrl: './admin-appointments.component.html',
  styleUrl: './admin-appointments.component.css'
})
export class AdminAppointmentsComponent implements OnInit {

  private adminService = inject(AdminService);
  private areasService = inject(AreasService);

  appointments: AdminAppointment[] = [];
  loading = true;
  error = '';
  /** Id des Termins, der gerade gelöscht wird (Knopf sperren). */
  busyId: number | null = null;

  /** true = auch vergangene Termine laden. */
  showPast = false;
  /** null = alle. */
  areaFilter: number | null = null;
  creatorFilter: number | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.adminService.getAdminAppointments(this.showPast).subscribe({
      next: appointments => { this.appointments = appointments; this.loading = false; },
      error: () => { this.error = 'Termine konnten nicht geladen werden.'; this.loading = false; }
    });
  }

  // ── Filter ────────────────────────────────────────────────────────────────

  /** Termine nach Gebiet und Ersteller gefiltert. */
  get filtered(): AdminAppointment[] {
    return this.appointments.filter(a =>
      (this.areaFilter === null || a.areaId === this.areaFilter) &&
      (this.creatorFilter === null || a.createdByUserId === this.creatorFilter));
  }

  /** Gebiete, in denen es Termine gibt – alphabetisch. */
  get areaOptions(): FilterOption[] {
    return this.uniqueOptions(this.appointments.map(a => ({ id: a.areaId ?? 0, name: a.areaName ?? 'Klettergebiet' })));
  }

  /** User, die Termine erstellt haben – alphabetisch. */
  get creatorOptions(): FilterOption[] {
    return this.uniqueOptions(this.appointments.map(a => ({ id: a.createdByUserId, name: a.createdByUsername })));
  }

  get hasFilter(): boolean {
    return this.areaFilter !== null || this.creatorFilter !== null;
  }

  resetFilters(): void {
    this.areaFilter = null;
    this.creatorFilter = null;
  }

  // ── Aktionen ──────────────────────────────────────────────────────────────

  remove(appointment: AdminAppointment): void {
    const question = `Termin „${appointment.title}" von ${appointment.createdByUsername} endgültig löschen? ` +
      'Alle Teilnahmen und Pinnwand-Beiträge werden mitgelöscht.';
    if (!window.confirm(question)) return;

    this.busyId = appointment.id;
    this.error = '';
    this.areasService.deleteAppointment(appointment.id).subscribe({
      next: () => {
        this.appointments = this.appointments.filter(a => a.id !== appointment.id);
        this.busyId = null;
      },
      error: () => { this.error = 'Termin konnte nicht gelöscht werden.'; this.busyId = null; }
    });
  }

  // ── Hilfen ────────────────────────────────────────────────────────────────

  formatDate(value: string | null | undefined): string {
    return value ? dateTimeFormatter.format(new Date(value)) : '–';
  }

  isPast(appointment: AdminAppointment): boolean {
    return !!appointment.date && new Date(appointment.date).getTime() < Date.now();
  }

  /** Doppelte Einträge (gleiche Id) entfernen und nach Namen sortieren. */
  private uniqueOptions(options: FilterOption[]): FilterOption[] {
    const byId = new Map(options.map(o => [o.id, o]));
    return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name, 'de'));
  }
}
