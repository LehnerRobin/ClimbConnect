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

  subscribedIds = new Set<number>();

  gradeScale = 'french';

  loading = true;
  errorMessage = '';

  showCommentForm = false;
  commentText = '';
  commentSending = false;
  commentError = '';
  commentSuccess = false;

  ngOnInit(): void {
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

  submitComment(): void {
    if (!this.commentText.trim() || !this.area) return;
    this.commentSending = true;
    this.commentError = '';
    this.commentSuccess = false;

    this.areasService.addAreaComment(this.area.id, this.commentText.trim()).subscribe({
      next: (comment) => {
        this.comments = [comment, ...this.comments];
        this.commentText = '';
        this.commentSending = false;
        this.commentSuccess = true;
        this.showCommentForm = false;
      },
      error: () => {
        this.commentError = 'Kommentar konnte nicht gespeichert werden.';
        this.commentSending = false;
      }
    });
  }
}
