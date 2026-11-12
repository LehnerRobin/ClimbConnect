import { Component, Input, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of, switchMap } from 'rxjs';
import { AppointmentDetail, Area, AreasService } from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';
import { PinnwandComponent } from '../pinnwand/pinnwand.component';
import { PageBandComponent } from '../../shared/page-band.component';
import { areaFallbackPhoto, gradeClass } from '../../shared/design-utils';

/**
 * Eigene Seite für einen Termin (/appointments/:id).
 * Zeigt alle Infos, erlaubt Beitreten/Austreten und – für Ersteller und Admin – Bearbeiten/Löschen.
 */
@Component({
  selector: 'app-appointment-detail',
  standalone: true,
  imports: [DatePipe, RouterLink, PinnwandComponent, PageBandComponent],
  templateUrl: './appointment-detail.component.html',
  styleUrl: './appointment-detail.component.css'
})
export class AppointmentDetailComponent implements OnInit {

  private areasService = inject(AreasService);
  private authService  = inject(AuthService);
  private userService  = inject(UserService);
  private router       = inject(Router);

  /** Kommt über withComponentInputBinding direkt aus der URL. */
  @Input() id!: string;

  appointment: AppointmentDetail | null = null;
  area: Area | null = null;
  creatorName: string | null = null;
  gradeScale = 'french';

  loading = true;
  notFound = false;
  loadError = '';

  actionRunning = false;
  actionError = '';

  readonly currentUserId = this.authService.getUserId();
  readonly gradeClass = gradeClass;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const appointmentId = Number(this.id);
    if (!appointmentId) {
      this.notFound = true;
      this.loading = false;
      return;
    }

    this.loading = true;
    this.notFound = false;
    this.loadError = '';

    // Erst die bevorzugte Skala, damit der Ø-Grad gleich richtig umgerechnet kommt
    this.userService.getPreferredScale().pipe(
      switchMap(scale => {
        this.gradeScale = scale;
        return this.areasService.getAppointmentById(appointmentId, scale);
      })
    ).subscribe({
      next: (detail: AppointmentDetail) => {
        this.appointment = detail;
        this.loading = false;
        this.loadArea(detail.areaId);
        this.loadCreatorName(detail);
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        if (err.status === 404) {
          this.notFound = true;
        } else {
          this.loadError = 'Termin konnte nicht geladen werden.';
        }
      }
    });
  }

  // ── Berechnete Werte ──────────────────────────────────────────────────────

  get isLoggedIn(): boolean {
    return this.currentUserId !== null;
  }

  get isSubscribed(): boolean {
    return !!this.appointment?.participants.some(p => p.userId === this.currentUserId);
  }

  get isCreator(): boolean {
    return this.currentUserId !== null && this.appointment?.createdByUserId === this.currentUserId;
  }

  /** Ersteller und Admin dürfen bearbeiten und löschen. */
  get canManage(): boolean {
    return this.isCreator || this.authService.hasRole('admin');
  }

  get isFull(): boolean {
    const max = this.appointment?.maxParticipants;
    return !!max && (this.appointment?.participantCount ?? 0) >= max;
  }

  get isPast(): boolean {
    return !!this.appointment?.date && new Date(this.appointment.date).getTime() < Date.now();
  }

  /** z. B. "3 / 5" – ohne Maximum nur die Anzahl. */
  get participantLabel(): string {
    const count = this.appointment?.participantCount ?? 0;
    const max   = this.appointment?.maxParticipants;
    return max ? `${count} / ${max}` : `${count}`;
  }

  /** Füllstand für den Balken (0–100). */
  get fillPercent(): number {
    const max = this.appointment?.maxParticipants;
    if (!max) return 0;
    return Math.min(100, Math.round(((this.appointment?.participantCount ?? 0) / max) * 100));
  }

  get scaleLabel(): string {
    switch (this.gradeScale) {
      case 'uiaa':     return 'UIAA';
      case 'american': return 'YDS';
      default:         return 'Französisch';
    }
  }

  /** Bild des Gebiets für den Kopfbereich, sonst ein festes Ersatzfoto. */
  get bandImage(): string {
    if (this.area?.imageUrl) return this.area.imageUrl;
    return areaFallbackPhoto(this.appointment?.areaId ?? 0);
  }

  // ── Aktionen ──────────────────────────────────────────────────────────────

  join(): void {
    if (!this.appointment || this.actionRunning) return;
    this.actionRunning = true;
    this.actionError = '';

    this.areasService.subscribeToAppointment(this.appointment.id).subscribe({
      next: () => this.reloadAfterAction(),
      error: (err: HttpErrorResponse) => {
        this.actionRunning = false;
        this.actionError = err.error?.error ?? 'Beitreten ist fehlgeschlagen.';
      }
    });
  }

  leave(): void {
    if (!this.appointment || this.actionRunning) return;
    this.actionRunning = true;
    this.actionError = '';

    this.areasService.unsubscribeFromAppointment(this.appointment.id).subscribe({
      next: () => this.reloadAfterAction(),
      error: () => {
        this.actionRunning = false;
        this.actionError = 'Austreten ist fehlgeschlagen.';
      }
    });
  }

  deleteAppointment(): void {
    if (!this.appointment || this.actionRunning) return;
    if (!window.confirm(`Termin "${this.appointment.title}" wirklich löschen?`)) return;

    const areaId = this.appointment.areaId;
    this.actionRunning = true;
    this.actionError = '';

    this.areasService.deleteAppointment(this.appointment.id).subscribe({
      next: () => this.router.navigate(['/areas', areaId]),
      error: (err: HttpErrorResponse) => {
        this.actionRunning = false;
        this.actionError = err.status === 401 || err.status === 403
          ? 'Du darfst diesen Termin nicht löschen.'
          : 'Termin konnte nicht gelöscht werden.';
      }
    });
  }

  // ── Laden ─────────────────────────────────────────────────────────────────

  /** Nach Beitreten/Austreten Teilnehmerliste und Ø-Grad neu holen. */
  private reloadAfterAction(): void {
    if (!this.appointment) return;
    this.areasService.getAppointmentById(this.appointment.id, this.gradeScale).subscribe({
      next: (detail: AppointmentDetail) => {
        this.appointment = detail;
        this.actionRunning = false;
      },
      error: () => {
        this.actionRunning = false;
      }
    });
  }

  private loadArea(areaId: number): void {
    this.areasService.getAreaById(areaId).pipe(
      catchError(() => of(null))
    ).subscribe(area => this.area = area);
  }

  /** Name des Erstellers: aus der Teilnehmerliste, sonst über das öffentliche Profil. */
  private loadCreatorName(detail: AppointmentDetail): void {
    const fromParticipants = detail.participants.find(p => p.userId === detail.createdByUserId);
    if (fromParticipants) {
      this.creatorName = fromParticipants.username;
      return;
    }
    this.userService.getPublicProfile(detail.createdByUserId).pipe(
      catchError(() => of(null))
    ).subscribe(profile => this.creatorName = profile?.username ?? null);
  }
}
