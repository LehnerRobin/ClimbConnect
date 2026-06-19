import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AreasService, ClimbingRoute, RouteComment, SafetyReport } from '../../../services/areas.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-route-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './route-detail.component.html',
  styleUrl: './route-detail.component.css'
})
export class RouteDetailComponent implements OnInit {

  route?: ClimbingRoute;
  communityGrade: string | null = null;
  comments: RouteComment[] = [];
  reports: SafetyReport[] = [];

  loading = true;
  errorMessage = '';

  // Kommentar-Formular
  newCommentText = '';
  commentPhotoFile: File | null = null;
  commentPhotoPreview: string | null = null;
  commentSending = false;
  commentError = '';
  commentSuccess = false;

  // Report-Formular
  showReportForm = false;
  reportText = '';
  reportSeverity = 'Low';
  reportPhotoFile: File | null = null;
  reportSending = false;
  reportError = '';
  reportSuccess = false;

  constructor(
    private activatedRoute: ActivatedRoute,
    private areasService: AreasService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const id = Number(this.activatedRoute.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Ungültige Routen-ID.';
      this.loading = false;
      return;
    }

    this.areasService.getRouteById(id, 'french').subscribe({
      next: (r) => {
        this.route = r;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Route konnte nicht geladen werden.';
        this.loading = false;
      }
    });

    this.areasService.getCommunityGrade(id, 'french').subscribe({
      next: (data) => { this.communityGrade = data?.communityGrade ?? null; },
      error: () => {}
    });

    this.areasService.getRouteComments(id).subscribe({
      next: (c) => { this.comments = c; },
      error: () => {}
    });

    this.areasService.getRouteReports(id).subscribe({
      next: (r) => { this.reports = r; },
      error: () => {}
    });
  }

  get isLoggedIn(): boolean {
    return this.authService.isAuthenticated();
  }

  get routeId(): number {
    return Number(this.activatedRoute.snapshot.paramMap.get('id'));
  }

  severityClass(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'high':   return 'severity-high';
      case 'medium': return 'severity-medium';
      default:       return 'severity-low';
    }
  }

  // ── Foto-Upload für Kommentar ───────────────────────────────────────────────

  onCommentPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      this.commentError = 'Bild darf maximal 5 MB groß sein.';
      return;
    }
    this.commentPhotoFile = file;
    const reader = new FileReader();
    reader.onload = () => { this.commentPhotoPreview = reader.result as string; };
    reader.readAsDataURL(file);
  }

  submitComment(): void {
    if (!this.newCommentText.trim()) return;
    this.commentSending = true;
    this.commentError = '';
    this.commentSuccess = false;

    const doPost = (photoUrl: string | null) => {
      this.areasService.addRouteComment(this.routeId, this.newCommentText.trim(), photoUrl).subscribe({
        next: (comment) => {
          this.comments = [comment, ...this.comments];
          this.newCommentText = '';
          this.commentPhotoFile = null;
          this.commentPhotoPreview = null;
          this.commentSending = false;
          this.commentSuccess = true;
        },
        error: () => {
          this.commentError = 'Kommentar konnte nicht gespeichert werden.';
          this.commentSending = false;
        }
      });
    };

    if (this.commentPhotoFile) {
      this.areasService.uploadImage(this.commentPhotoFile).subscribe({
        next: (res) => doPost(res.url),
        error: () => {
          this.commentError = 'Foto-Upload fehlgeschlagen.';
          this.commentSending = false;
        }
      });
    } else {
      doPost(null);
    }
  }

  // ── Safety Report ───────────────────────────────────────────────────────────

  onReportPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      this.reportError = 'Bild darf maximal 5 MB groß sein.';
      return;
    }
    this.reportPhotoFile = file;
  }

  submitReport(): void {
    if (!this.reportText.trim()) return;
    this.reportSending = true;
    this.reportError = '';
    this.reportSuccess = false;

    const doPost = (photoUrl: string | null) => {
      this.areasService.createReport({
        routeId: this.routeId,
        text: this.reportText.trim(),
        severity: this.reportSeverity,
        photoUrl
      }).subscribe({
        next: (report) => {
          this.reports = [report, ...this.reports];
          this.reportText = '';
          this.reportSeverity = 'Low';
          this.reportPhotoFile = null;
          this.reportSending = false;
          this.reportSuccess = true;
          this.showReportForm = false;
        },
        error: () => {
          this.reportError = 'Report konnte nicht gespeichert werden.';
          this.reportSending = false;
        }
      });
    };

    if (this.reportPhotoFile) {
      this.areasService.uploadImage(this.reportPhotoFile).subscribe({
        next: (res) => doPost(res.url),
        error: () => {
          this.reportError = 'Foto-Upload fehlgeschlagen.';
          this.reportSending = false;
        }
      });
    } else {
      doPost(null);
    }
  }
}
