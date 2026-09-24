import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AreasService, ProgressUpdateRequest } from '../../../services/areas.service';

@Component({
  selector: 'app-progress-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './progress-form.component.html',
  styleUrl: './progress-form.component.css'
})
export class ProgressFormComponent implements OnInit {

  routeId = 0;
  routeName = '';
  progressId = 0;
  isEditMode = false;
  loading = false;

  form = {
    status:                 'Rotpunkt',
    climbingStyle:          'Vorstieg',
    attempts:               1,
    date:                   new Date().toISOString().split('T')[0],
    notes:                  '',
    subjectiveGrade:        '',
    subjectiveGradeComment: ''
  };

  statuses        = ['Rotpunkt', 'Flash', 'Onsight', 'Projekt', 'Toprope'];
  climbingStyles  = ['Vorstieg', 'Toprope'];

  frenchGrades = [
    '3a','3b','3c','4a','4b','4c',
    '5a','5b','5c',
    '6a','6a+','6b','6b+','6c','6c+',
    '7a','7a+','7b','7b+','7c','7c+',
    '8a','8a+','8b','8b+','8c','8c+',
    '9a','9a+','9b','9b+','9c'
  ];

  submitting = false;
  errorMessage = '';

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private areasService: AreasService
  ) {}

  ngOnInit(): void {
    this.progressId = Number(this.activatedRoute.snapshot.paramMap.get('id'));
    this.isEditMode = this.progressId > 0;

    if (this.isEditMode) {
      this.loadProgress();
      return;
    }

    this.routeId = Number(this.activatedRoute.snapshot.queryParamMap.get('routeId'));
    this.routeName = this.activatedRoute.snapshot.queryParamMap.get('routeName') ?? '';
  }

  private loadProgress(): void {
    this.loading = true;
    this.errorMessage = '';

    this.areasService.getProgressById(this.progressId).subscribe({
      next: (progress) => {
        this.routeId = progress.routeId ?? progress.route?.id ?? 0;
        this.routeName = progress.route?.name ?? 'Route';

        this.form = {
          status:                 progress.status ?? 'Rotpunkt',
          climbingStyle:          progress.climbingStyle ?? 'Vorstieg',
          attempts:               progress.attempts ?? 1,
          date:                   this.toInputDate(progress.date),
          notes:                  progress.notes ?? '',
          subjectiveGrade:        progress.subjectiveGrade ?? '',
          subjectiveGradeComment: progress.subjectiveGradeComment ?? ''
        };

        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.error ?? 'Begehung konnte nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  submit(): void {
    if (!this.isEditMode && !this.routeId) {
      this.errorMessage = 'Ungültige Routen-ID.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';

    const payload = this.buildProgressPayload();

    if (this.isEditMode) {
      this.areasService.updateProgress(this.progressId, payload).subscribe({
        next: () => this.router.navigate(['/profile']),
        error: (err) => {
          this.errorMessage = err?.error?.error ?? 'Begehung konnte nicht gespeichert werden.';
          this.submitting = false;
        }
      });
      return;
    }

    this.areasService.createProgress({
      routeId: this.routeId,
      ...payload
    }).subscribe({
      next: () => {
        if (this.routeId) {
          this.router.navigate(['/routes', this.routeId]);
        } else {
          this.router.navigate(['/profile']);
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.error ?? 'Begehung konnte nicht gespeichert werden.';
        this.submitting = false;
      }
    });
  }

  cancel(): void {
    if (this.isEditMode) {
      this.router.navigate(['/profile']);
      return;
    }

    if (this.routeId) {
      this.router.navigate(['/routes', this.routeId]);
    } else {
      this.router.navigate(['/profile']);
    }
  }

  private buildProgressPayload(): ProgressUpdateRequest {
    return {
      status:                 this.form.status,
      climbingStyle:          this.form.climbingStyle,
      attempts:               Number(this.form.attempts) || 1,
      date:                   this.form.date,
      notes:                  this.form.notes.trim() || null,
      subjectiveGrade:        this.form.subjectiveGrade.trim() || null,
      subjectiveGradeComment: this.form.subjectiveGradeComment.trim() || null
    };
  }

  private toInputDate(value?: string | null): string {
    if (!value) {
      return new Date().toISOString().split('T')[0];
    }

    return value.includes('T') ? value.split('T')[0] : value;
  }
}
