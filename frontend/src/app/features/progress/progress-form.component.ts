import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AreasService } from '../../../services/areas.service';

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
    this.routeId = Number(this.activatedRoute.snapshot.queryParamMap.get('routeId'));
    this.routeName = this.activatedRoute.snapshot.queryParamMap.get('routeName') ?? '';
  }

  submit(): void {
    if (!this.routeId) {
      this.errorMessage = 'Ungültige Routen-ID.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';

    this.areasService.createProgress({
      routeId:                this.routeId,
      status:                 this.form.status,
      climbingStyle:          this.form.climbingStyle,
      attempts:               this.form.attempts,
      date:                   this.form.date,
      notes:                  this.form.notes.trim() || null,
      subjectiveGrade:        this.form.subjectiveGrade.trim() || null,
      subjectiveGradeComment: this.form.subjectiveGradeComment.trim() || null
    }).subscribe({
      next: () => {
        // Zurück zur Route oder zum Profil
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
    if (this.routeId) {
      this.router.navigate(['/routes', this.routeId]);
    } else {
      this.router.navigate(['/profile']);
    }
  }
}
