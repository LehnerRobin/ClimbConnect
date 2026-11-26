import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FeedbackEntry, FeedbackService } from '../../../services/feedback.service';

/**
 * Admin: Feedback der User (#244). Ungelesene stehen oben (Sortierung macht das Backend).
 * Der Admin kann Einträge als gelesen/ungelesen markieren und löschen.
 */
@Component({
  selector: 'app-feedback-list',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './feedback-list.component.html',
  styleUrl: './feedback-list.component.css'
})
export class FeedbackListComponent implements OnInit {

  private feedbackService = inject(FeedbackService);

  entries: FeedbackEntry[] = [];
  loading = true;
  error = '';
  /** Id des Eintrags, an dem gerade etwas gespeichert wird (Knöpfe sperren). */
  busyId: number | null = null;

  get unreadCount(): number {
    return this.entries.filter(e => !e.isRead).length;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.feedbackService.getAll().subscribe({
      next: entries => { this.entries = entries; this.loading = false; },
      error: () => { this.error = 'Feedback konnte nicht geladen werden.'; this.loading = false; }
    });
  }

  toggleRead(entry: FeedbackEntry): void {
    this.busyId = entry.id;
    this.feedbackService.setRead(entry.id, !entry.isRead).subscribe({
      next: () => { entry.isRead = !entry.isRead; this.busyId = null; },
      error: () => { this.error = 'Status konnte nicht gespeichert werden.'; this.busyId = null; }
    });
  }

  remove(entry: FeedbackEntry): void {
    if (!window.confirm('Dieses Feedback endgültig löschen? Ein angehängtes Foto wird mitgelöscht.')) return;
    this.busyId = entry.id;
    this.feedbackService.delete(entry.id).subscribe({
      next: () => { this.entries = this.entries.filter(e => e.id !== entry.id); this.busyId = null; },
      error: () => { this.error = 'Feedback konnte nicht gelöscht werden.'; this.busyId = null; }
    });
  }
}
