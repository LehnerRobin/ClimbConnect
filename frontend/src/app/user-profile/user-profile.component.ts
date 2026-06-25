import { Component, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { UserService } from '../../services/user.service';
import { Area, Appointment, AreasService } from '../../services/areas.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

interface MyAppointment extends Appointment {
  areaId: number;
  areaName: string;
  areaLocation?: string | null;
}

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit, AfterViewInit {

  @ViewChild('gradeChart') gradeChartRef!: ElementRef<HTMLCanvasElement>;

  user = {
    id: 0,
    username: '',
    email: '',
    bio: '',
    preferredGradeScale: 'french',
    memberSince: ''
  };

  stats = {
    totalAscents: 0,
    openProjects: 0,
    favoriteArea: '-'
  };

  gradeProgression: { month: string; grade: string }[] = [];
  chartReady = false;
  private chart: Chart | null = null;

  myProgress: any[] = [];
  myAppointments: MyAppointment[] = [];
  loadingAppointments = false;
  appointmentError = '';
  deletingAppointmentId: number | null = null;

  saveSuccess = false;
  saveError = '';

  constructor(
    private userService: UserService,
    private areasService: AreasService
  ) {}

  ngOnInit(): void {
    this.userService.getMe().subscribe({
      next: (data) => {
        this.user.id                  = data.id;
        this.user.username            = data.username;
        this.user.email               = data.email;
        this.user.bio                 = data.bio ?? '';
        this.user.preferredGradeScale = data.preferredGradeScale ?? 'french';
        this.user.memberSince         = data.memberSince;

        this.loadStats();
        this.loadMyProgress();
        this.loadMyAppointments();
      },
      error: () => {}
    });
  }

  ngAfterViewInit(): void {
    this.chartReady = true;
    this.buildChart();
  }

  private loadStats(): void {
    this.userService.getStats(this.user.id, this.user.preferredGradeScale ?? 'french').subscribe({
      next: (s) => {
        this.stats.totalAscents = s.totalClimbed ?? 0;
        this.stats.openProjects = s.openProjects ?? 0;
        this.stats.favoriteArea = s.favoriteArea ?? '-';
        this.gradeProgression   = s.gradeProgression ?? [];
        this.buildChart();
      },
      error: () => {}
    });
  }

  private loadMyProgress(): void {
    this.areasService.getMyProgress().subscribe({
      next: (p) => { this.myProgress = p; },
      error: () => {}
    });
  }

  private loadMyAppointments(): void {
    this.loadingAppointments = true;
    this.appointmentError = '';

    this.areasService.getAreas().pipe(
      switchMap((areas: Area[]) => {
        if (areas.length === 0) {
          return of([] as MyAppointment[]);
        }

        const requests = areas.map((area: Area) =>
          this.areasService.getAppointmentsByArea(area.id, true).pipe(
            map((appointments: Appointment[]) => appointments.map((appointment: Appointment): MyAppointment => ({
              ...appointment,
              areaId: appointment.areaId ?? area.id,
              areaName: area.name,
              areaLocation: area.location ?? null
            }))),
            catchError(() => of([] as MyAppointment[]))
          )
        );

        return forkJoin(requests).pipe(
          map((groups: MyAppointment[][]) => groups.flat())
        );
      }),
      map((appointments: MyAppointment[]) => appointments
        .filter((appointment: MyAppointment) => appointment.createdByUserId === this.user.id)
        .sort((a: MyAppointment, b: MyAppointment) => this.getAppointmentTime(a) - this.getAppointmentTime(b))
      )
    ).subscribe({
      next: (appointments) => {
        this.myAppointments = appointments;
        this.loadingAppointments = false;
      },
      error: () => {
        this.appointmentError = 'Eigene Termine konnten nicht geladen werden.';
        this.loadingAppointments = false;
      }
    });
  }

  deleteOwnAppointment(appointment: MyAppointment): void {
    const confirmed = window.confirm(`Termin \"${appointment.title}\" wirklich löschen?`);
    if (!confirmed) {
      return;
    }

    this.appointmentError = '';
    this.deletingAppointmentId = appointment.id;

    this.areasService.deleteAppointment(appointment.id).subscribe({
      next: () => {
        this.myAppointments = this.myAppointments.filter(item => item.id !== appointment.id);
        this.deletingAppointmentId = null;
      },
      error: (error) => {
        console.error('Appointment delete error:', error);
        this.appointmentError = error.status === 401 || error.status === 403
          ? 'Du darfst nur eigene Termine löschen.'
          : 'Termin konnte nicht gelöscht werden.';
        this.deletingAppointmentId = null;
      }
    });
  }

  private getAppointmentTime(appointment: Appointment): number {
    if (!appointment.date) {
      return 0;
    }

    const time = new Date(appointment.date).getTime();
    return Number.isFinite(time) ? time : 0;
  }

  private buildChart(): void {
    if (!this.chartReady || this.gradeProgression.length === 0) return;
    if (!this.gradeChartRef?.nativeElement) return;

    if (this.chart) {
      this.chart.destroy();
    }

    this.chart = new Chart(this.gradeChartRef.nativeElement, {
      type: 'line',
      data: {
        labels: this.gradeProgression.map(p => p.month),
        datasets: [{
          label: 'Höchster Grad',
          data: this.gradeProgression.map((_, i) => i + 1),
          borderColor: '#2563eb',
          backgroundColor: 'rgba(37,99,235,0.1)',
          tension: 0.3,
          fill: true,
          pointRadius: 5
        }]
      },
      options: {
        plugins: {
          tooltip: {
            callbacks: {
              label: (ctx) => this.gradeProgression[ctx.dataIndex]?.grade ?? ''
            }
          },
          legend: { display: false }
        },
        scales: {
          y: {
            ticks: {
              callback: (_, i) => this.gradeProgression[i]?.grade ?? ''
            }
          }
        }
      }
    });
  }

  saveProfile(): void {
    this.saveSuccess = false;
    this.saveError   = '';

    this.userService.updateProfile({
      bio: this.user.bio || null,
      preferredGradeScale: this.user.preferredGradeScale || null
    }).subscribe({
      next: (data) => {
        this.user.bio               = data.bio ?? '';
        this.user.preferredGradeScale = data.preferredGradeScale ?? 'french';
        this.saveSuccess = true;
      },
      error: (err) => {
        this.saveError = err?.error?.error ?? 'Profil konnte nicht gespeichert werden.';
      }
    });
  }
}
