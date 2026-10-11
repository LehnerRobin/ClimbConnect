import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AreasService, ProgressUpdateRequest } from '../../../services/areas.service';
import { Grade, GradeService, gradeLabel } from '../../../services/grade.service';

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

  // Muss zu ProgressConst.Statuses im Backend passen; Toprope ist eine Begehungsart, kein Status
  statuses        = ['Rotpunkt', 'Flash', 'Onsight', 'Projekt'];
  climbingStyles  = ['Vorstieg', 'Toprope'];
  // Muss zu ProgressConst.FirstTryStatuses im Backend passen
  private readonly firstTryStatuses = ['Flash', 'Onsight'];

  /** True bei Flash/Onsight: Dann ist die Begehung im ersten Versuch gelungen, also genau 1 Versuch. */
  get isFirstTry(): boolean {
    return this.firstTryStatuses.includes(this.form.status);
  }

  /** Wird bei jeder Änderung des Status aufgerufen und setzt bei Flash/Onsight die Versuche auf 1. */
  onStatusChange(status: string): void {
    this.form.status = status;
    if (this.isFirstTry) {
      this.form.attempts = 1;
    }
  }

  /** Grad-Tabelle aus dem Backend (GET /api/grades). */
  grades: Grade[] = [];
  readonly gradeLabel = gradeLabel;

  submitting = false;
  errorMessage = '';

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private areasService: AreasService,
    private gradeService: GradeService
  ) {}

  ngOnInit(): void {
    this.gradeService.getGrades().subscribe({
      next: grades => this.grades = grades,
      error: () => this.errorMessage = 'Grad-Liste konnte nicht geladen werden.'
    });

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

        // Ältere Einträge könnten „Flash mit 3 Versuchen“ enthalten, das korrigieren wir beim Laden
        if (this.isFirstTry) {
          this.form.attempts = 1;
        }

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
      attempts:               this.isFirstTry ? 1 : (Number(this.form.attempts) || 1),
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
