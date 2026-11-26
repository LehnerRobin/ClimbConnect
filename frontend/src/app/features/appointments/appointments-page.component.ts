import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { Appointment, Area, AreasService } from '../../../services/areas.service';
import { PageBandComponent } from '../../shared/page-band.component';
import { DudesComponent } from '../../shared/dudes.component';
import { DayStats, MonthCalendarComponent } from './month-calendar.component';
import { monthGrid, viennaCurrentMonth, viennaDayKey, viennaTime, viennaTodayKey } from '../../shared/vienna-date';

/** Termine eines Tages für die Liste. */
interface DayGroup {
  key: string;
  label: string;
  appointments: Appointment[];
}

/**
 * Terminübersicht (#230): Liste aller Termine eines Monats und daneben ein großer
 * Monatskalender mit "X Leute · Y Termine" pro Tag. Klick auf einen Tag filtert die Liste,
 * Klick auf einen Termin öffnet die Termin-Seite (#229).
 */
@Component({
  selector: 'app-appointments-page',
  standalone: true,
  imports: [FormsModule, RouterLink, PageBandComponent, MonthCalendarComponent, DudesComponent],
  templateUrl: './appointments-page.component.html',
  styleUrl: './appointments-page.component.css'
})
export class AppointmentsPageComponent implements OnInit, OnDestroy {

  private areasService = inject(AreasService);

  year  = viennaCurrentMonth().year;
  month = viennaCurrentMonth().month;
  readonly todayKey = viennaTodayKey();

  /** Alle Termine im sichtbaren Kalenderraster (inkl. Rand-Tage der Nachbarmonate). */
  private appointments: Appointment[] = [];

  areas: Area[] = [];
  /** null = alle Gebiete. */
  areaFilter: number | null = null;

  selectedKey: string | null = null;

  loading = true;
  errorMessage = '';

  readonly viennaTime = viennaTime;

  /**
   * Die gerade laufende Monats-Anfrage. Wird beim nächsten Laden abgebrochen,
   * damit eine verspätete Antwort für einen alten Monat nichts überschreibt (#295).
   */
  private loadSub?: Subscription;

  ngOnInit(): void {
    this.areasService.getAreas().subscribe({
      next: areas => this.areas = [...areas].sort((a, b) => a.name.localeCompare(b.name, 'de')),
      error: () => this.areas = []
    });
    this.loadMonth();
  }

  /** Beim Verlassen der Seite eine noch laufende Anfrage abbrechen. */
  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
  }

  // ── Laden ─────────────────────────────────────────────────────────────────

  /**
   * Lädt die Termine des angezeigten Monats. Eine noch laufende Anfrage für einen
   * anderen Monat wird vorher abgebrochen – so gewinnt immer der zuletzt gewählte Monat.
   */
  loadMonth(): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    // Zeitraum = sichtbares Raster. Einen Tag Puffer auf beiden Seiten, weil Wien
    // gegenüber UTC verschoben ist; genau zugeordnet wird danach per Wiener Tages-Schlüssel.
    const grid = monthGrid(this.year, this.month);
    const from = this.utcFromKey(grid[0].key, -1);
    const to   = this.utcFromKey(grid[grid.length - 1].key, 2);
    const visibleKeys = new Set(grid.map(d => d.key));

    this.loadSub = this.areasService.getAppointmentsInRange(from, to).subscribe({
      next: appointments => {
        this.appointments = appointments
          .filter(a => a.date && visibleKeys.has(viennaDayKey(a.date)))
          .sort((a, b) => (a.date ?? '').localeCompare(b.date ?? ''));
        this.loading = false;
      },
      error: () => {
        this.appointments = [];
        this.errorMessage = 'Termine konnten nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  // ── Kalender-Aktionen ─────────────────────────────────────────────────────

  previousMonth(): void {
    if (this.month === 0) { this.month = 11; this.year--; } else { this.month--; }
    this.selectedKey = null;
    this.loadMonth();
  }

  nextMonth(): void {
    if (this.month === 11) { this.month = 0; this.year++; } else { this.month++; }
    this.selectedKey = null;
    this.loadMonth();
  }

  goToToday(): void {
    const now = viennaCurrentMonth();
    const changed = now.year !== this.year || now.month !== this.month;
    this.year = now.year;
    this.month = now.month;
    this.selectedKey = this.todayKey;
    if (changed) this.loadMonth();
  }

  /** Klick auf einen Tag: Liste auf diesen Tag einschränken, zweiter Klick hebt das wieder auf. */
  selectDay(key: string): void {
    this.selectedKey = this.selectedKey === key ? null : key;
  }

  showWholeMonth(): void {
    this.selectedKey = null;
  }

  // ── Daten für Kalender und Liste ──────────────────────────────────────────

  /** Termine nach Gebietsfilter. */
  private get filtered(): Appointment[] {
    return this.areaFilter === null
      ? this.appointments
      : this.appointments.filter(a => a.areaId === this.areaFilter);
  }

  /** Pro Tag: Summe der Teilnehmer aller Termine und Anzahl Termine. */
  get dayStats(): Record<string, DayStats> {
    const stats: Record<string, DayStats> = {};
    for (const a of this.filtered) {
      const key = viennaDayKey(a.date!);
      const s = stats[key] ?? (stats[key] = { people: 0, appointments: 0 });
      s.people += a.participantCount ?? 0;
      s.appointments += 1;
    }
    return stats;
  }

  /** Liste: ausgewählter Tag, sonst alle Tage des angezeigten Monats. */
  get groups(): DayGroup[] {
    const monthPrefix = `${this.year}-${String(this.month + 1).padStart(2, '0')}-`;
    const byDay = new Map<string, Appointment[]>();

    for (const a of this.filtered) {
      const key = viennaDayKey(a.date!);
      if (this.selectedKey ? key !== this.selectedKey : !key.startsWith(monthPrefix)) continue;
      const list = byDay.get(key) ?? [];
      list.push(a);
      byDay.set(key, list);
    }

    return [...byDay.entries()].map(([key, appointments]) => ({
      key,
      label: this.dayLabel(key),
      appointments
    }));
  }

  get selectedLabel(): string {
    return this.selectedKey ? this.dayLabel(this.selectedKey) : '';
  }

  get monthName(): string {
    return new Intl.DateTimeFormat('de-AT', { month: 'long', timeZone: 'UTC' })
      .format(new Date(Date.UTC(this.year, this.month, 1)));
  }

  isPast(a: Appointment): boolean {
    return !!a.date && new Date(a.date).getTime() < Date.now();
  }

  // ── Hilfen ────────────────────────────────────────────────────────────────

  /** "Donnerstag, 26. November" für einen Tages-Schlüssel. */
  private dayLabel(key: string): string {
    const [y, m, d] = key.split('-').map(Number);
    return new Intl.DateTimeFormat('de-AT', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' })
      .format(new Date(Date.UTC(y, m - 1, d)));
  }

  /** UTC-Mitternacht eines Tages-Schlüssels, um `shiftDays` Tage verschoben. */
  private utcFromKey(key: string, shiftDays: number): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(Date.UTC(y, m - 1, d + shiftDays));
  }
}
