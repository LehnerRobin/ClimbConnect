import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Area,
  AreaComment,
  Appointment,
  AreasService,
  ClimbingRoute,
  Sector
} from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';

interface SectorWithRoutes extends Sector {
  expanded: boolean;
  loadingRoutes: boolean;
  routes: ClimbingRoute[];
}

@Component({
  selector: 'app-area-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './area-detail.component.html',
  styleUrl: './area-detail.component.css'
})
export class AreaDetailComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private areasService = inject(AreasService);
  private authService = inject(AuthService);
  private userService = inject(UserService);

  area?: Area;
  sectors: SectorWithRoutes[] = [];
  appointments: Appointment[] = [];
  comments: AreaComment[] = [];

  // IDs der Termine, bei denen der User schon angemeldet ist
  subscribedIds = new Set<number>();
  currentUserId: number | null = null;
  deletingAppointmentId: number | null = null;
  appointmentActionError = '';

  expandedAppointmentId: number | null = null;
  appointmentDetails: { [id: number]: any } = {};

  gradeScale = 'french';

  loading = true;
  errorMessage = '';

  showCommentForm = false;
  commentText = '';
  commentSending = false;
  commentError = '';
  deletingCommentId: number | null = null;

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserId();
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {
      this.errorMessage = 'Ungültige Area-ID.';
      this.loading = false;
      return;
    }

    if (this.authService.isAuthenticated()) {
      this.userService.getMe().subscribe({
        next: (data) => { this.gradeScale = data.preferredGradeScale ?? 'french'; },
        error: () => {}
      });
    }

    this.loadAreaDetails(id);
  }

  loadAreaDetails(areaId: number): void {
    this.loading = true;
    this.errorMessage = '';

    this.areasService.getAreaById(areaId).subscribe({
      next: (area) => {
        this.area = area;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Gebiet konnte nicht geladen werden.';
        this.loading = false;
      }
    });

    this.areasService.getSectorsByArea(areaId).subscribe({
      next: (sectors) => {
        this.sectors = sectors.map(sector => ({
          ...sector,
          expanded: false,
          loadingRoutes: false,
          routes: []
        }));
      },
      error: () => {}
    });

    this.areasService.getAppointmentsByArea(areaId).subscribe({
      next: (appointments) => { this.appointments = appointments; },
      error: () => {}
    });

    this.areasService.getCommentsByArea(areaId).subscribe({
      next: (comments) => { this.comments = comments; },
      error: () => {}
    });
  }

  toggleSector(sector: SectorWithRoutes): void {
    sector.expanded = !sector.expanded;

    if (sector.expanded && sector.routes.length === 0) {
      this.loadRoutesForSector(sector);
    }
  }

  loadRoutesForSector(sector: SectorWithRoutes): void {
    sector.loadingRoutes = true;

    this.areasService.getRoutesBySector(sector.id, this.gradeScale).subscribe({
      next: (routes) => {
        sector.routes = routes;
        sector.loadingRoutes = false;
      },
      error: () => { sector.loadingRoutes = false; }
    });
  }

  isLoggedIn(): boolean {
    return this.authService.isAuthenticated();
  }

  isSubscribed(appointmentId: number): boolean {
    return this.subscribedIds.has(appointmentId);
  }

  isOwnAppointment(appointment: Appointment): boolean {
    return !!this.currentUserId && appointment.createdByUserId === this.currentUserId;
  }

  deleteAppointment(appointment: Appointment): void {
    const confirmed = window.confirm(`Termin "${appointment.title}" wirklich löschen?`);
    if (!confirmed) {
      return;
    }

    this.appointmentActionError = '';
    this.deletingAppointmentId = appointment.id;

    this.areasService.deleteAppointment(appointment.id).subscribe({
      next: () => {
        this.appointments = this.appointments.filter(item => item.id !== appointment.id);
        this.subscribedIds.delete(appointment.id);
        this.deletingAppointmentId = null;
      },
      error: (error) => {
        console.error('Appointment delete error:', error);
        this.appointmentActionError = error.status === 401 || error.status === 403
          ? 'Du darfst nur eigene Termine löschen.'
          : 'Termin konnte nicht gelöscht werden.';
        this.deletingAppointmentId = null;
      }
    });
  }

  subscribe(appointment: Appointment): void {
    this.areasService.subscribeToAppointment(appointment.id).subscribe({
      next: () => {
        this.subscribedIds.add(appointment.id);
        appointment.participantCount = (appointment.participantCount ?? 0) + 1;
      },
      error: () => {}
    });
  }

  unsubscribe(appointment: Appointment): void {
    this.areasService.unsubscribeFromAppointment(appointment.id).subscribe({
      next: () => {
        this.subscribedIds.delete(appointment.id);
        appointment.participantCount = Math.max((appointment.participantCount ?? 1) - 1, 0);
      },
      error: () => {}
    });
  }

  toggleAppointmentDetail(appointmentId: number): void {
    if (this.expandedAppointmentId === appointmentId) {
      this.expandedAppointmentId = null;
      return;
    }
    this.expandedAppointmentId = appointmentId;
    if (!this.appointmentDetails[appointmentId]) {
      this.areasService.getAppointmentById(appointmentId, this.gradeScale).subscribe({
        next: (detail) => { this.appointmentDetails[appointmentId] = detail; },
        error: () => {}
      });
    }
  }

  isOwnComment(comment: { userId?: number }): boolean {
    return !!this.currentUserId && comment.userId === this.currentUserId;
  }

  deleteComment(commentId: number): void {
    if (!window.confirm('Kommentar wirklich löschen?')) return;
    this.deletingCommentId = commentId;
    this.areasService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        this.deletingCommentId = null;
      },
      error: () => { this.deletingCommentId = null; }
    });
  }

  submitComment(): void {
    if (!this.commentText.trim() || !this.area) return;
    this.commentSending = true;
    this.commentError = '';

    this.areasService.addAreaComment(this.area.id, this.commentText.trim()).subscribe({
      next: (comment: AreaComment) => {
        this.comments = [comment, ...this.comments];
        this.commentText = '';
        this.commentSending = false;
        this.showCommentForm = false;
      },
      error: () => {
        this.commentError = 'Kommentar konnte nicht gespeichert werden.';
        this.commentSending = false;
      }
    });
  }

  /** Gibt das echte Bild zurück oder wählt eine lokale SVG-Illustration als Fallback. */
  getAreaImage(area: { id: number; imageUrl?: string | null }): string {
    if (area.imageUrl) return area.imageUrl;
    const imgs = [
      '/assets/areas/area-alpine.svg',
      '/assets/areas/area-wall.svg',
      '/assets/areas/area-ridge.svg'
    ];
    return imgs[area.id % imgs.length];
  }
  /** Fallback auf lokale SVG-Illustration wenn imageUrl nicht geladen werden kann. */
  onImageError(event: Event, area: { id: number }): void {
    const img = event.target as HTMLImageElement;
    const imgs = [
      '/assets/areas/area-alpine.svg',
      '/assets/areas/area-wall.svg',
      '/assets/areas/area-ridge.svg'
    ];
    // Verhindern dass der Fehler-Handler sich selbst aufruft wenn die SVG auch nicht lädt
    img.onerror = null;
    img.src = imgs[area.id % imgs.length];
  }

}
