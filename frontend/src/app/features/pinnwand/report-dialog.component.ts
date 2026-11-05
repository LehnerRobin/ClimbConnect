import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PinnwandService } from '../../../services/pinnwand.service';
import { PinnwandPost, REPORT_REASONS, ReportReason } from '../../../models/pinnwand.model';

/**
 * Dialog zum Melden eines Pinnwand-Beitrags.
 * Zeigt den Beitrag als Zitat, lässt einen Grund wählen und optional einen Kommentar schreiben.
 * Bei "Sonstiges" ist der Kommentar Pflicht.
 */
@Component({
  selector: 'app-report-dialog',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './report-dialog.component.html',
  styleUrl: './report-dialog.component.css'
})
export class ReportDialogComponent implements AfterViewInit {

  private pinnwandService = inject(PinnwandService);

  @Input({ required: true }) post!: PinnwandPost;

  /** Wird nach erfolgreichem Melden ausgelöst. */
  @Output() reported = new EventEmitter<void>();
  @Output() closed   = new EventEmitter<void>();

  @ViewChild('firstOption') firstOption?: ElementRef<HTMLInputElement>;

  readonly reasons = REPORT_REASONS;
  readonly maxCommentLength = 500;

  reason: ReportReason | null = null;
  comment = '';
  sending = false;
  error = '';

  ngAfterViewInit(): void {
    // Fokus in den Dialog setzen, damit Tastatur-Nutzer direkt einen Grund wählen können
    setTimeout(() => this.firstOption?.nativeElement.focus());
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.sending) this.closed.emit();
  }

  get commentRequired(): boolean {
    return this.reason === 'Sonstiges';
  }

  get canSubmit(): boolean {
    if (!this.reason || this.sending) return false;
    if (this.commentRequired && !this.comment.trim()) return false;
    return this.comment.length <= this.maxCommentLength;
  }

  submit(): void {
    if (!this.canSubmit || !this.reason) return;
    this.sending = true;
    this.error = '';

    this.pinnwandService.reportPost(this.post.id, {
      reason:  this.reason,
      comment: this.comment.trim() || null
    }).subscribe({
      next: () => {
        this.sending = false;
        this.reported.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.sending = false;
        this.error = this.messageFor(err);
        // Schon gemeldet → für die Oberfläche gilt der Beitrag trotzdem als gemeldet
        if (err.status === 409) this.reported.emit();
      }
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    const serverText = err.error?.error as string | undefined;
    switch (err.status) {
      case 403: return serverText ?? 'Nur Teilnehmer dieses Termins können Beiträge melden.';
      case 409: return serverText ?? 'Du hast diesen Beitrag bereits gemeldet.';
      case 404: return 'Der Beitrag existiert nicht mehr.';
      case 400: return serverText ?? 'Die Meldung ist unvollständig.';
      case 0:   return 'Keine Verbindung zum Server.';
      default:  return 'Die Meldung konnte nicht gesendet werden. Bitte versuche es später noch einmal.';
    }
  }
}
