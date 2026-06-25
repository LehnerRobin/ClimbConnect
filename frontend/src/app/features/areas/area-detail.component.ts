import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  Area,
  AreaComment,
  Appointment,
  AreasService,
  ClimbingRoute,
  Sector
} from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';

interface SectorWithRoutes extends Sector {
  expanded: boolean;
  loadingRoutes: boolean;
  routes: ClimbingRoute[];
}

@Component({
  selector: 'app-area-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './area-detail.component.html',
  styleUrl: './area-detail.component.css'
})
export class AreaDetailComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private areasService = inject(AreasService);
  private authService = inject(AuthService);

  area?: Area;
  sectors: SectorWithRoutes[] = [];
  appointments: Appointment[] = [];
  comments: AreaComment[] = [];

  // IDs der Termine, bei denen der User schon angemeldet ist
  subscribedIds = new Set<number>();
  currentUserId: number | null = null;
  deletingAppointmentId: number | null = null;
  appointmentActionError = '';

  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserId();
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {
      this.errorMessage = 'Ungültige Area-ID.';
      this.loading = false;
      return;
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

    this.areasService.getRoutesBySector(sector.id, 'french').subscribe({
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
}
