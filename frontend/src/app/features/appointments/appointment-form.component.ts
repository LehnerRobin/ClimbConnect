import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AppointmentCreateRequest,
  AppointmentUpdateRequest,
  AreasService
} from '../../../services/areas.service';

@Component({
  selector: 'app-appointment-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './appointment-form.component.html',
  styleUrl: './appointment-form.component.css'
})
export class AppointmentFormComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private areasService = inject(AreasService);

  areaId = Number(this.route.snapshot.paramMap.get('id'));
  appointmentId = Number(this.route.snapshot.paramMap.get('appointmentId')) || null;

  loading = false;
  submitting = false;
  errorMessage = '';

  /** Ursprünglicher Zeitpunkt beim Bearbeiten – ein unveränderter vergangener Termin darf gespeichert werden. */
  private loadedStart: number | null = null;

  form = {
    title: '',
    date: toLocalDateInput(new Date()),
    time: '18:00',
    meetingPoint: '',
    description: '',
    minParticipants: 1,
    maxParticipants: 4
  };

  get isEditMode(): boolean {
    return this.appointmentId !== null;
  }

  ngOnInit(): void {
    if (this.isEditMode && this.appointmentId) {
      this.loadAppointment(this.appointmentId);
    }
  }

  loadAppointment(appointmentId: number): void {
    this.loading = true;
    this.errorMessage = '';

    this.areasService.getAppointmentById(appointmentId).subscribe({
      next: (appointment) => {
        this.form.title = appointment.title ?? '';
        this.form.meetingPoint = appointment.meetingPoint ?? '';
        this.form.description = appointment.description ?? '';
        this.form.minParticipants = appointment.minParticipants ?? 1;
        this.form.maxParticipants = appointment.maxParticipants ?? 4;

        if (appointment.date) {
          // Das Backend liefert UTC ("...Z"); das Formular zeigt Ortszeit
          const date = new Date(appointment.date);
          this.form.date = toLocalDateInput(date);
          this.form.time = toLocalTimeInput(date);
          this.loadedStart = date.getTime();
        }

        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Termin konnte nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  saveAppointment(): void {
    this.errorMessage = '';

    if (!this.areaId) {
      this.errorMessage = 'Ungültige Gebiets-ID.';
      return;
    }

    if (!this.form.title.trim()) {
      this.errorMessage = 'Bitte gib einen Titel ein.';
      return;
    }

    if (!this.form.date || !this.form.time) {
      this.errorMessage = 'Bitte gib Datum und Uhrzeit ein.';
      return;
    }

    // Datum und Uhrzeit aus dem Formular sind Ortszeit des Browsers
    const start = new Date(`${this.form.date}T${this.form.time}:00`);
    if (isNaN(start.getTime())) {
      this.errorMessage = 'Bitte gib ein gültiges Datum und eine gültige Uhrzeit ein.';
      return;
    }
    if (start.getTime() < Date.now() && (!this.isEditMode || start.getTime() !== this.loadedStart)) {
      this.errorMessage = 'Der Termin darf nicht in der Vergangenheit liegen.';
      return;
    }

    if (this.form.minParticipants && this.form.maxParticipants) {
      if (this.form.minParticipants > this.form.maxParticipants) {
        this.errorMessage = 'Minimale Teilnehmerzahl darf nicht größer als maximale Teilnehmerzahl sein.';
        return;
      }
    }

    const appointment: AppointmentCreateRequest | AppointmentUpdateRequest = {
      title: this.form.title.trim(),
      // Als UTC senden ("...Z"), damit Backend und alle Browser denselben Zeitpunkt meinen
      date: start.toISOString(),
      meetingPoint: this.form.meetingPoint.trim() || null,
      description: this.form.description.trim() || null,
      minParticipants: this.form.minParticipants || null,
      maxParticipants: this.form.maxParticipants || null
    };

    this.submitting = true;

    const request = this.isEditMode && this.appointmentId
      ? this.areasService.updateAppointment(this.appointmentId, appointment)
      : this.areasService.createAppointment(this.areaId, appointment);

    request.subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/areas', this.areaId]);
      },
      error: (error) => {
        console.error('Appointment save error:', error);
        this.submitting = false;

        if (error.status === 401 || error.status === 403) {
          this.errorMessage = this.isEditMode
            ? 'Du darfst nur eigene Termine bearbeiten.'
            : 'Du musst eingeloggt sein, um eine Klettersession zu erstellen.';
        } else {
          this.errorMessage = this.isEditMode
            ? 'Termin konnte nicht gespeichert werden.'
            : 'Klettersession konnte nicht erstellt werden.';
        }
      }
    });
  }
}

/** Datum im Format yyyy-MM-dd in Ortszeit (für <input type="date">). */
function toLocalDateInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Uhrzeit im Format HH:mm in Ortszeit (für <input type="time">). */
function toLocalTimeInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
