import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

/** Worauf sich das Feedback bezieht – leer = allgemeines Feedback zur App. */
export interface FeedbackContext {
  areaId?: number;
  routeId?: number;
  /** Name für die Anzeige im Dialog, z. B. "Traunstein". */
  label?: string;
}

/** Feedback-Eintrag, wie ihn der Admin sieht (GET /api/feedback). */
export interface FeedbackEntry {
  id: number;
  userId: number;
  username: string;
  text: string;
  photoUrl: string | null;
  areaId: number | null;
  areaName: string | null;
  routeId: number | null;
  routeName: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

/**
 * Feedback an das Team (#244): Senden für User, Verwalten für den Admin – und der
 * Zustand des Feedback-Dialogs, damit ihn jede Seite und das Menü öffnen können.
 */
@Injectable({ providedIn: 'root' })
export class FeedbackService {

  private http = inject(HttpClient);
  private api  = `${environment.apiUrl}/api/feedback`;

  /** Ist der Dialog offen? */
  readonly open = signal(false);
  /** Bezug des aktuell geöffneten Dialogs. */
  readonly context = signal<FeedbackContext>({});

  /** Öffnet den Dialog, optional mit Bezug zu Gebiet oder Route. */
  show(context: FeedbackContext = {}): void {
    this.context.set(context);
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
  }

  send(body: { text: string; photoUrl: string | null; areaId: number | null; routeId: number | null }): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.api, body);
  }

  // ── Admin ──────────────────────────────────────────────────────────────────

  getAll(): Observable<FeedbackEntry[]> {
    return this.http.get<FeedbackEntry[]>(this.api);
  }

  setRead(id: number, isRead: boolean): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}/read`, { isRead });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }
}
