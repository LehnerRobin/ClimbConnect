import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CalendarDay, monthGrid } from '../../shared/vienna-date';

/** "3. Dezember 2026" – für den Vorlesetext. Einmal anlegen, Intl.DateTimeFormat ist teuer. */
const ariaDateFormatter = new Intl.DateTimeFormat('de-AT', {
  day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC'
});

/** Zahlen für einen Kalendertag. */
export interface DayStats {
  people: number;
  appointments: number;
}

/**
 * Eigener Monatskalender (ohne Bibliothek).
 * Zeigt pro Tag "8 Leute · 2 Termine", färbt Tage mit vielen Leuten stärker ein
 * und meldet Klicks auf einen Tag und das Blättern an die Seite zurück.
 * Die Komponente lädt selbst keine Daten.
 */
@Component({
  selector: 'app-month-calendar',
  standalone: true,
  templateUrl: './month-calendar.component.html',
  styleUrl: './month-calendar.component.css'
})
export class MonthCalendarComponent implements OnChanges {

  @Input({ required: true }) year!: number;
  /** Monat 0–11. */
  @Input({ required: true }) month!: number;
  /** Tages-Schlüssel "YYYY-MM-DD" → Zahlen. Tage ohne Eintrag bleiben leer. */
  @Input() stats: Record<string, DayStats> = {};
  @Input() todayKey = '';
  @Input() selectedKey: string | null = null;

  @Output() previousMonth = new EventEmitter<void>();
  @Output() nextMonth     = new EventEmitter<void>();
  @Output() daySelected   = new EventEmitter<string>();

  readonly weekdays = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

  days: CalendarDay[] = [];

  ngOnChanges(): void {
    this.days = monthGrid(this.year, this.month);
  }

  get monthLabel(): string {
    return new Intl.DateTimeFormat('de-AT', { month: 'long', year: 'numeric', timeZone: 'UTC' })
      .format(new Date(Date.UTC(this.year, this.month, 1)));
  }

  statsFor(key: string): DayStats | null {
    return this.stats[key] ?? null;
  }

  /**
   * Farbstufe nach Anzahl Leute: 0 = keine Termine, 1 = wenige (1–3),
   * 2 = mittel (4–7), 3 = viele (8 und mehr). Die Legende unter dem Kalender erklärt das.
   */
  level(key: string): number {
    const s = this.stats[key];
    if (!s || s.appointments === 0) return 0;
    if (s.people >= 8) return 3;
    if (s.people >= 4) return 2;
    return 1;
  }

  /**
   * Vorlesetext für Screenreader, z. B. "14. November 2026: 8 Leute, 2 Termine".
   * Das Datum kommt aus dem Tages-Schlüssel selbst – so haben auch die grauen
   * Rand-Tage aus Vor- und Folgemonat den richtigen Monat (#296).
   */
  ariaLabel(day: CalendarDay): string {
    const s = this.stats[day.key];
    const [y, m, d] = day.key.split('-').map(Number);
    const base = ariaDateFormatter.format(new Date(Date.UTC(y, m - 1, d)));
    if (!s) return `${base}: keine Termine`;
    return `${base}: ${s.people} ${s.people === 1 ? 'Person' : 'Leute'}, ${s.appointments} ${s.appointments === 1 ? 'Termin' : 'Termine'}`;
  }

  select(day: CalendarDay): void {
    this.daySelected.emit(day.key);
  }
}
