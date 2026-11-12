import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { Area, Appointment, AreasService } from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';
import { EveningLightComponent } from './evening-light.component';
import { HomeCommunityComponent } from './home-community.component';

interface HomeAppointment extends Appointment {
  areaId: number;
  areaName: string;
  areaLocation?: string | null;
}

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink, EveningLightComponent, HomeCommunityComponent],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.css'
})
export class HomePageComponent implements OnInit {

  private readonly areasService = inject(AreasService);
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);

  featuredAreas: Area[] = [];
  upcomingAppointments: HomeAppointment[] = [];

  areaCount = 0;
  routeCount = 0;
  userCount = 0;

  loading = true;
  errorMessage = '';
  appointmentsErrorMessage = '';

  username: string | null = null;

  /** Fotos, die verwendet werden, wenn ein Gebiet kein eigenes Bild hat (nach Position in der Liste). */
  private readonly fallbackAreaPhotos = [
    '/assets/home/gebiet-grat.jpg',
    '/assets/home/gebiet-fels-wald.jpg',
    '/assets/home/gebiet-schlucht.jpg'
  ];

  ngOnInit(): void {
    this.username = this.authService.getUsername();
    this.loadHomeData();
  }

  get nextAppointment(): HomeAppointment | null {
    return this.upcomingAppointments[0] ?? null;
  }

  /** Das erste Gebiet wird groß als „Titelgeschichte" gezeigt. */
  get leadArea(): Area | null {
    return this.featuredAreas[0] ?? null;
  }

  /** Die restlichen Gebiete stehen versetzt nebeneinander. */
  get otherAreas(): Area[] {
    return this.featuredAreas.slice(1);
  }

  /**
   * Einträge für das Laufband unter dem Hero. Die Liste wird verdoppelt,
   * damit die CSS-Animation nahtlos von vorne beginnen kann.
   */
  get marqueeItems(): string[] {
    const names = this.featuredAreas.map(area => area.name);
    const items = [...names, 'Gemeinsam am Fels'];
    return [...items, ...items, ...items, ...items];
  }

  /** True, wenn die nächste Session in diesem Gebiet stattfindet. */
  isNextSessionArea(area: Area): boolean {
    return !!this.nextAppointment && this.nextAppointment.areaId === area.id;
  }

  /** Formatiert 1 → "01" für die großen Nummern auf den Fotos. */
  pad(position: number): string {
    return position.toString().padStart(2, '0');
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  loadHomeData(): void {
    this.loading = true;
    this.errorMessage = '';
    this.appointmentsErrorMessage = '';

    forkJoin({
      areas: this.areasService.getAreas().pipe(
        catchError(() => {
          this.errorMessage = 'Gebiete konnten nicht geladen werden.';
          return of([] as Area[]);
        })
      ),
      routeCount: this.areasService.getRouteCount().pipe(
        catchError(() => of(0))
      ),
      users: this.userService.getUsers().pipe(
        catchError(() => of([]))
      )
    }).subscribe(({ areas, routeCount, users }) => {
      this.areaCount = areas.length;
      this.routeCount = routeCount;
      this.userCount = users.length;
      this.featuredAreas = this.getFeaturedAreas(areas);
      this.loadUpcomingAppointments();
    });
  }

  /** Lädt die nächsten 3 Termine mit einer einzigen Anfrage. */
  private loadUpcomingAppointments(): void {
    this.areasService.getUpcomingAppointments(3).subscribe({
      next: appointments => {
        this.upcomingAppointments = appointments.map(appointment => ({
          ...appointment,
          areaId: appointment.areaId ?? 0,
          areaName: appointment.areaName ?? '',
          areaLocation: appointment.areaLocation ?? null
        }));
        this.loading = false;
      },
      error: () => {
        this.appointmentsErrorMessage = 'Termine konnten nicht geladen werden.';
        this.upcomingAppointments = [];
        this.loading = false;
      }
    });
  }

  private getFeaturedAreas(areas: Area[]): Area[] {
    return [...areas]
      .sort((a, b) => {
        const visitorDifference = (b.todayVisitors ?? 0) - (a.todayVisitors ?? 0);
        if (visitorDifference !== 0) {
          return visitorDifference;
        }

        const appointmentDifference = (b.todayAppointments ?? 0) - (a.todayAppointments ?? 0);
        if (appointmentDifference !== 0) {
          return appointmentDifference;
        }

        return (b.createdAtUtc ?? '').localeCompare(a.createdAtUtc ?? '');
      })
      .slice(0, 3);
  }

  /** Gibt das echte Bild des Gebiets zurück oder ein passendes Kletterfoto als Ersatz. */
  getAreaImage(area: { id: number; imageUrl?: string | null }, position: number): string {
    return area.imageUrl || this.fallbackAreaPhoto(position);
  }

  /** Ersatzfoto, wenn imageUrl nicht geladen werden kann. */
  onImageError(event: Event, position: number): void {
    const img = event.target as HTMLImageElement;
    // Verhindern, dass der Fehler-Handler sich selbst erneut aufruft
    img.onerror = null;
    img.src = this.fallbackAreaPhoto(position);
  }

  private fallbackAreaPhoto(position: number): string {
    return this.fallbackAreaPhotos[position % this.fallbackAreaPhotos.length];
  }
}
