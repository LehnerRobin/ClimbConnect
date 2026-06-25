import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { Area, Appointment, AreasService } from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';

interface HomeAppointment extends Appointment {
  areaId: number;
  areaName: string;
  areaLocation?: string | null;
}

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
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

  ngOnInit(): void {
    this.username = this.authService.getUsername();
    this.loadHomeData();
  }

  get nextAppointment(): HomeAppointment | null {
    return this.upcomingAppointments[0] ?? null;
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
      routes: this.areasService.getRoutes().pipe(
        catchError(() => of([]))
      ),
      users: this.userService.getUsers().pipe(
        catchError(() => of([]))
      )
    }).subscribe(({ areas, routes, users }) => {
      this.areaCount = areas.length;
      this.routeCount = routes.length;
      this.userCount = users.length;
      this.featuredAreas = this.getFeaturedAreas(areas);
      this.loadUpcomingAppointments(areas);
    });
  }

  private loadUpcomingAppointments(areas: Area[]): void {
    if (areas.length === 0) {
      this.upcomingAppointments = [];
      this.loading = false;
      return;
    }

    const requests = areas.map(area =>
      this.areasService.getAppointmentsByArea(area.id).pipe(
        map(appointments => appointments.map(appointment => ({
          ...appointment,
          areaId: area.id,
          areaName: area.name,
          areaLocation: area.location
        }))),
        catchError(() => of([] as HomeAppointment[]))
      )
    );

    forkJoin(requests).subscribe({
      next: appointmentGroups => {
        this.upcomingAppointments = appointmentGroups
          .flat()
          .filter(appointment => !!appointment.date)
          .sort((a, b) => new Date(a.date ?? '').getTime() - new Date(b.date ?? '').getTime())
          .slice(0, 3);
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
}
