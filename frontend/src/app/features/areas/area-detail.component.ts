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

  loading = true;
  errorMessage = '';

  ngOnInit(): void {
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
      error: () => {
        console.error('Sektoren konnten nicht geladen werden.');
      }
    });

    this.areasService.getAppointmentsByArea(areaId).subscribe({
      next: (appointments) => {
        this.appointments = appointments;
      },
      error: () => {
        console.error('Termine konnten nicht geladen werden.');
      }
    });

    this.areasService.getCommentsByArea(areaId).subscribe({
      next: (comments) => {
        this.comments = comments;
      },
      error: () => {
        console.error('Kommentare konnten nicht geladen werden.');
      }
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

    const scale = this.getPreferredGradeScale();

    this.areasService.getRoutesBySector(sector.id, scale).subscribe({
      next: (routes) => {
        sector.routes = routes;
        sector.loadingRoutes = false;
      },
      error: () => {
        sector.loadingRoutes = false;
        console.error('Routen konnten nicht geladen werden.');
      }
    });
  }

  getPreferredGradeScale(): string {
    const token = this.authService.getToken();

    if (!token) {
      return 'french';
    }

    const payload = this.authService.getRole();

    return payload === 'admin' ? 'french' : 'french';
  }

  isLoggedIn(): boolean {
    return this.authService.isAuthenticated();
  }
}