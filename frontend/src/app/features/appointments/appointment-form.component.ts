import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AreasService, AppointmentCreateRequest } from '../../../services/areas.service';

@Component({
  selector: 'app-appointment-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './appointment-form.component.html',
  styleUrl: './appointment-form.component.css'
})
export class AppointmentFormComponent {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private areasService = inject(AreasService);

  areaId = Number(this.route.snapshot.paramMap.get('id'));

  submitting = false;
  errorMessage = '';

  form = {
    title: '',
    date: new Date().toISOString().split('T')[0],
    time: '18:00',
    meetingPoint: '',
    description: '',
    minParticipants: 1,
    maxParticipants: 4
  };

  createAppointment(): void {
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

    if (this.form.minParticipants && this.form.maxParticipants) {
      if (this.form.minParticipants > this.form.maxParticipants) {
        this.errorMessage = 'Minimale Teilnehmerzahl darf nicht größer als maximale Teilnehmerzahl sein.';
        return;
      }
    }

    const appointment: AppointmentCreateRequest = {
      title: this.form.title.trim(),
      date: `${this.form.date}T${this.form.time}:00`,
      meetingPoint: this.form.meetingPoint.trim() || null,
      description: this.form.description.trim() || null,
      minParticipants: this.form.minParticipants || null,
      maxParticipants: this.form.maxParticipants || null
    };

    this.submitting = true;

    this.areasService.createAppointment(this.areaId, appointment).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/areas', this.areaId]);
      },
      error: (error) => {
        console.error('Appointment create error:', error);
        this.submitting = false;

        if (error.status === 401 || error.status === 403) {
          this.errorMessage = 'Du musst eingeloggt sein, um eine Klettersession zu erstellen.';
        } else {
          this.errorMessage = 'Klettersession konnte nicht erstellt werden.';
        }
      }
    });
  }
}