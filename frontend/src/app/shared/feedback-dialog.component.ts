import { Component, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FeedbackService } from '../../services/feedback.service';
import { AreasService } from '../../services/areas.service';

/**
 * Feedback-Dialog (#244) – liegt einmal in app.html und wird über den FeedbackService
 * geöffnet (Menü, Gebiets- und Routenseite). Text, optional Foto, optional Bezug zur
 * aktuellen Seite. Das Feedback sieht nur das Team.
 */
@Component({
  selector: 'app-feedback-dialog',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './feedback-dialog.component.html',
  styleUrl: './feedback-dialog.component.css'
})
export class FeedbackDialogComponent {

  readonly feedback = inject(FeedbackService);
  private areasService = inject(AreasService);

  @ViewChild('textField') textField?: ElementRef<HTMLTextAreaElement>;

  readonly maxLength = 2000;

  text = '';
  /** Bezug zur aktuellen Seite mitschicken? Standard ja, wenn es einen gibt. */
  withContext = true;
  photoFile: File | null = null;
  photoPreview: string | null = null;
  sending = false;
  error = '';
  sent = false;

  constructor() {
    // Bei jedem Öffnen Formular zurücksetzen und ins Textfeld springen
    effect(() => {
      if (this.feedback.open()) {
        this.reset();
        setTimeout(() => this.textField?.nativeElement.focus());
      }
    });
  }

  get hasContext(): boolean {
    const c = this.feedback.context();
    return !!(c.areaId || c.routeId);
  }

  get contextLabel(): string {
    const c = this.feedback.context();
    if (c.routeId) return c.label ? `die Route „${c.label}"` : 'diese Route';
    return c.label ? `das Gebiet „${c.label}"` : 'dieses Gebiet';
  }

  get canSubmit(): boolean {
    return !this.sending && this.text.trim().length > 0 && this.text.length <= this.maxLength;
  }

  @HostListener('document:keydown.escape')
  close(): void {
    if (this.sending) return;
    this.feedback.close();
  }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      this.error = 'Das Foto darf maximal 5 MB groß sein.';
      return;
    }
    this.error = '';
    this.photoFile = file;
    const reader = new FileReader();
    reader.onload = () => { this.photoPreview = reader.result as string; };
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoFile = null;
    this.photoPreview = null;
  }

  /** Erst das Foto hochladen (falls vorhanden), dann das Feedback senden. */
  submit(): void {
    if (!this.canSubmit) return;
    this.sending = true;
    this.error = '';

    if (this.photoFile) {
      this.areasService.uploadImage(this.photoFile).subscribe({
        next: ({ url }) => this.send(url),
        error: (err: HttpErrorResponse) => this.fail(err, 'Das Foto konnte nicht hochgeladen werden.')
      });
    } else {
      this.send(null);
    }
  }

  private send(photoUrl: string | null): void {
    const c = this.feedback.context();
    this.feedback.send({
      text: this.text.trim(),
      photoUrl,
      areaId:  this.withContext ? c.areaId  ?? null : null,
      routeId: this.withContext ? c.routeId ?? null : null
    }).subscribe({
      next: () => {
        this.sending = false;
        this.sent = true;
      },
      error: (err: HttpErrorResponse) => this.fail(err, 'Das Feedback konnte nicht gesendet werden.')
    });
  }

  private fail(err: HttpErrorResponse, fallback: string): void {
    this.sending = false;
    this.error = err?.error?.error ?? fallback;
  }

  private reset(): void {
    this.text = '';
    this.withContext = true;
    this.photoFile = null;
    this.photoPreview = null;
    this.sending = false;
    this.error = '';
    this.sent = false;
  }
}
