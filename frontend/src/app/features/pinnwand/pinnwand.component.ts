import { Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PinnwandService } from '../../../services/pinnwand.service';
import { AuthService } from '../../../services/auth.service';
import { PinnwandPost } from '../../../models/pinnwand.model';
import { ReportDialogComponent } from './report-dialog.component';

/**
 * Pinnwand eines Termins.
 * - Lesen: alle eingeloggten User
 * - Schreiben und Melden: nur Teilnehmer (bzw. Ersteller) des Termins
 * - Melden-Knopf nur bei Beiträgen anderer, und nur einmal pro Beitrag
 */
@Component({
  selector: 'app-pinnwand',
  standalone: true,
  imports: [DatePipe, FormsModule, ReportDialogComponent],
  templateUrl: './pinnwand.component.html',
  styleUrl: './pinnwand.component.css'
})
export class PinnwandComponent implements OnChanges {

  private pinnwandService = inject(PinnwandService);
  private authService     = inject(AuthService);

  @Input({ required: true }) appointmentId!: number;

  /** Teilnehmer oder Ersteller des Termins – nur dann darf geschrieben und gemeldet werden. */
  @Input() isParticipant = false;

  readonly maxLength = 1000;

  posts: PinnwandPost[] = [];
  loading = false;
  loadError = '';

  newText = '';
  sending = false;
  sendError = '';

  deletingId: number | null = null;

  /** Beitrag, für den gerade der Melde-Dialog offen ist. */
  reportingPost: PinnwandPost | null = null;
  /** Kurze Bestätigung nach dem Melden. */
  reportInfo = '';
  private reportInfoTimer: ReturnType<typeof setTimeout> | null = null;

  get currentUserId(): number | null {
    return this.authService.getUserId();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['appointmentId'] || changes['isParticipant']) {
      this.load();
    }
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.pinnwandService.getPosts(this.appointmentId).subscribe({
      next: posts => {
        this.posts = posts;
        this.loading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.loadError = err.status === 403
          ? 'Die Pinnwand ist nur für Teilnehmer sichtbar.'
          : 'Pinnwand konnte nicht geladen werden.';
      }
    });
  }

  isOwn(post: PinnwandPost): boolean {
    return this.currentUserId !== null && post.authorId === this.currentUserId;
  }

  /** Melden-Knopf: nur Teilnehmer, nur fremde Beiträge. */
  canReport(post: PinnwandPost): boolean {
    return this.isParticipant && !this.isOwn(post);
  }

  submit(): void {
    const text = this.newText.trim();
    if (!text || this.sending || text.length > this.maxLength) return;

    this.sending = true;
    this.sendError = '';
    this.pinnwandService.createPost(this.appointmentId, text).subscribe({
      next: post => {
        this.posts = [...this.posts, post];
        this.newText = '';
        this.sending = false;
      },
      error: (err: HttpErrorResponse) => {
        this.sending = false;
        this.sendError = err.status === 403
          ? 'Nur Teilnehmer können auf der Pinnwand schreiben.'
          : err.error?.error ?? 'Beitrag konnte nicht gespeichert werden.';
      }
    });
  }

  /** Strg/Cmd + Enter sendet den Beitrag. */
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.submit();
    }
  }

  deletePost(post: PinnwandPost): void {
    if (!window.confirm('Beitrag wirklich löschen?')) return;
    this.deletingId = post.id;
    this.pinnwandService.deletePost(post.id).subscribe({
      next: () => {
        this.posts = this.posts.filter(p => p.id !== post.id);
        this.deletingId = null;
      },
      error: () => {
        this.deletingId = null;
        this.sendError = 'Beitrag konnte nicht gelöscht werden.';
      }
    });
  }

  openReport(post: PinnwandPost): void {
    this.reportingPost = post;
  }

  closeReport(): void {
    this.reportingPost = null;
  }

  onReported(): void {
    if (this.reportingPost) {
      this.reportingPost.reportedByMe = true;
    }
    this.reportingPost = null;
    this.showReportInfo('Danke! Deine Meldung wurde an das Admin-Team weitergeleitet.');
  }

  private showReportInfo(text: string): void {
    this.reportInfo = text;
    if (this.reportInfoTimer) clearTimeout(this.reportInfoTimer);
    this.reportInfoTimer = setTimeout(() => this.reportInfo = '', 5000);
  }
}
