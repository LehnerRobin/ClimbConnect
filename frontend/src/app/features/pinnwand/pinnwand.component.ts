import { Component, Input, OnChanges, OnDestroy, OnInit, SimpleChanges, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PinnwandService } from '../../../services/pinnwand.service';
import { AuthService } from '../../../services/auth.service';
import { PinnwandPost } from '../../../models/pinnwand.model';
import { ReportDialogComponent } from './report-dialog.component';

/**
 * Pinnwand eines Termins (#245).
 * - Lesen, Schreiben und Melden: nur Teilnehmer (bzw. Ersteller) des Termins, Lesen auch Admin
 * - Aktualisiert sich alle 15 Sekunden selbst – aber nur, solange die Seite sichtbar ist
 *   (kein Live-Chat, keine Zusatztechnik wie WebSockets)
 * - Melden-Knopf nur bei Beiträgen anderer, und nur einmal pro Beitrag
 */
@Component({
  selector: 'app-pinnwand',
  standalone: true,
  imports: [DatePipe, FormsModule, ReportDialogComponent],
  templateUrl: './pinnwand.component.html',
  styleUrl: './pinnwand.component.css'
})
export class PinnwandComponent implements OnInit, OnChanges, OnDestroy {

  private pinnwandService = inject(PinnwandService);
  private authService     = inject(AuthService);

  @Input({ required: true }) appointmentId!: number;

  /** Teilnehmer oder Ersteller des Termins – nur dann darf geschrieben und gemeldet werden. */
  @Input() isParticipant = false;

  readonly maxLength = 1000;

  /** So oft wird nach neuen Beiträgen gefragt (Millisekunden). */
  static readonly REFRESH_MS = 15_000;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;
  private readonly onVisibilityChange = () => {
    // Beim Zurückkommen auf den Tab sofort aktualisieren
    if (document.visibilityState === 'visible') this.refresh();
  };

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

  ngOnInit(): void {
    this.refreshTimer = setInterval(() => {
      if (document.visibilityState === 'visible') this.refresh();
    }, PinnwandComponent.REFRESH_MS);
    document.addEventListener('visibilitychange', this.onVisibilityChange);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['appointmentId'] || changes['isParticipant']) {
      this.load();
    }
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
    if (this.reportInfoTimer) clearTimeout(this.reportInfoTimer);
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
  }

  /**
   * Stilles Nachladen im Hintergrund: keine Lade-Anzeige, und ein kurzer Netzwerkfehler
   * überschreibt die schon angezeigten Beiträge nicht.
   */
  refresh(): void {
    if (this.loading || this.loadError) return;
    this.pinnwandService.getPosts(this.appointmentId).subscribe({
      next: posts => this.posts = posts,
      error: () => { /* beim nächsten Durchlauf erneut versuchen */ }
    });
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

  /** Löschen-Knopf: eigene Beiträge, der Admin zusätzlich alle (#223 – unpassende Beiträge entfernen). */
  canDelete(post: PinnwandPost): boolean {
    return this.isOwn(post) || this.authService.hasRole('admin');
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
    const question = this.isOwn(post)
      ? 'Beitrag wirklich löschen?'
      : `Beitrag von ${post.authorUsername} als Admin löschen?`;
    if (!window.confirm(question)) return;
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
