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

/** Wert eines Grades in der gewünschten Skala ("french", "uiaa" oder "american"). */
export function gradeInScale(grade: Grade, scale: string): string {
  if (scale === 'uiaa') return grade.uiaa;
  if (scale === 'american') return grade.american;
  return grade.french;
}

/** Ein Eintrag für eine Grad-Auswahlliste. */
export interface GradeOption {
  /** Was gespeichert wird: immer der französische Grad. */
  value: string;
  /** Was der User sieht: der Grad in seiner Skala. */
  label: string;
}

/**
 * Baut die Auswahlliste in der bevorzugten Skala. Kommt ein Wert in dieser Skala
 * mehrmals vor (z. B. „VII+“ für 6c und 6c+), wird der französische Grad dazugeschrieben.
 */
export function buildGradeOptions(grades: Grade[], scale: string): GradeOption[] {
  // Zuerst zählen, wie oft jeder Wert in der Skala vorkommt
  const counts = new Map<string, number>();
  for (const grade of grades) {
    const value = gradeInScale(grade, scale);
    counts.set(value, (counts.get(value) ?? 0) + 1);
  }

  return grades.map(grade => {
    const value = gradeInScale(grade, scale);
    const isDuplicate = (counts.get(value) ?? 0) > 1;
    return {
      value: grade.french,
      label: isDuplicate ? `${value} (${grade.french})` : value
    };
  });
}
