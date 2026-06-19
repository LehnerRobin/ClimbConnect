import { Component, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UserService } from '../../services/user.service';
import { AreasService } from '../../services/areas.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

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

        this.userService.getStats(data.id, data.preferredGradeScale ?? 'french').subscribe({
          next: (s) => {
            this.stats.totalAscents = s.totalClimbed ?? 0;
            this.stats.openProjects = s.openProjects ?? 0;
            this.stats.favoriteArea = s.favoriteArea ?? '-';
            this.gradeProgression   = s.gradeProgression ?? [];
            this.buildChart();
          },
          error: () => {}
        });

        this.areasService.getMyProgress().subscribe({
          next: (p) => { this.myProgress = p; },
          error: () => {}
        });
      },
      error: () => {}
    });
  }

  ngAfterViewInit(): void {
    this.chartReady = true;
    this.buildChart();
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
