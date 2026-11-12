import { Component, Input, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ModerationService } from '../../../services/moderation.service';
import { CasePost, ModerationCaseDetail, REPORT_REASONS, reportReasonLabel } from '../../../models/pinnwand.model';
import { daysUntil } from './moderation-utils';
import { PageBandComponent } from '../../shared/page-band.component';

/**
 * Admin: Details eines Falls.
 * Zeigt den kompletten Pinnwand-Verlauf des Termins (als Kopie), hebt gemeldete Beiträge hervor
 * und listet zu jedem gemeldeten Beitrag, wer ihn warum gemeldet hat.
 */
@Component({
  selector: 'app-case-detail',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, PageBandComponent],
  templateUrl: './case-detail.component.html',
  styleUrl: './moderation.css'
})
export class CaseDetailComponent implements OnInit {

  private moderationService = inject(ModerationService);
  private router            = inject(Router);

  /** Kommt über withComponentInputBinding direkt aus der URL (/admin/faelle/:id). */
  @Input() id!: string;

  caseData: ModerationCaseDetail | null = null;
  loading = true;
  error = '';

  onlyReported = false;
  resolving = false;
  resolveError = '';

  readonly reasonLabel = reportReasonLabel;
  readonly daysUntil = daysUntil;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const caseId = Number(this.id);
    if (!caseId) {
      this.error = 'Ungültige Fall-ID.';
      this.loading = false;
      return;
    }
    this.loading = true;
    this.error = '';
    this.moderationService.getCase(caseId).subscribe({
      next: data => {
        this.caseData = {
          ...data,
          posts: [...data.posts].sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc))
        };
        this.loading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.error = err.status === 404
          ? 'Dieser Fall existiert nicht mehr (erledigt oder automatisch gelöscht).'
          : 'Fall konnte nicht geladen werden.';
      }
    });
  }

  get visiblePosts(): CasePost[] {
    const posts = this.caseData?.posts ?? [];
    return this.onlyReported ? posts.filter(p => p.reports.length > 0) : posts;
  }

  /** Anzahl Meldungen pro Grund – als kleine Übersicht oben. */
  get reasonSummary(): { label: string; count: number }[] {
    const counts = new Map<string, number>();
    for (const post of this.caseData?.posts ?? []) {
      for (const r of post.reports) {
        counts.set(r.reason, (counts.get(r.reason) ?? 0) + 1);
      }
    }
    return REPORT_REASONS
      .filter(r => counts.has(r.value))
      .map(r => ({ label: r.label, count: counts.get(r.value)! }));
  }

  /** Verfasser, deren Beiträge gemeldet wurden – hilft, wiederholte Fälle zu erkennen. */
  get reportedAuthors(): string[] {
    const names = new Set<string>();
    for (const post of this.caseData?.posts ?? []) {
      if (post.reports.length > 0) names.add(post.authorUsername);
    }
    return [...names];
  }

  resolve(): void {
    if (!this.caseData || this.resolving) return;
    const ok = window.confirm(
      `Fall #${this.caseData.id} erledigen?\n\n` +
      'Der Fall wird mit allen gespeicherten Beiträgen und Meldungen endgültig gelöscht.'
    );
    if (!ok) return;

    this.resolving = true;
    this.resolveError = '';
    this.moderationService.resolveCase(this.caseData.id).subscribe({
      next: () => {
        this.router.navigate(['/admin'], { queryParams: { tab: 'moderation' } });
      },
      error: () => {
        this.resolving = false;
        this.resolveError = 'Fall konnte nicht erledigt werden.';
      }
    });
  }
}
