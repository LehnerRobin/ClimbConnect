import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { shareReplay } from 'rxjs/operators';
import { environment } from '../environments/environment';

/** Ein Grad in allen drei Skalen, wie ihn GET /api/grades liefert. */
export interface Grade {
  french: string;
  uiaa: string;
  american: string;
}

/**
 * Liefert die Grad-Tabelle aus dem Backend. Es gibt bewusst keine eigene Liste im Frontend,
 * damit Auswahl und Backend-Validierung immer übereinstimmen.
 */
@Injectable({ providedIn: 'root' })
export class GradeService {

  /** Einmal laden und für alle weiteren Aufrufe merken – die Tabelle ändert sich nicht. */
  private readonly grades$: Observable<Grade[]>;

  constructor(http: HttpClient) {
    this.grades$ = http.get<Grade[]>(`${environment.apiUrl}/api/grades`).pipe(shareReplay(1));
  }

  /** Alle Grade, sortiert von leicht nach schwer. */
  getGrades(): Observable<Grade[]> {
    return this.grades$;
  }
}

/** Beschriftung für Auswahllisten, z. B. "6a (VI / 5.10a)". */
export function gradeLabel(grade: Grade): string {
  return `${grade.french} (${grade.uiaa} / ${grade.american})`;
}
