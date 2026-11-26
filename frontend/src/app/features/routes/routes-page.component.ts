import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { AreasService, ClimbingRoute, RouteFilter } from '../../../services/areas.service';
import { Grade, GradeOption, GradeService, buildGradeOptions } from '../../../services/grade.service';
import { UserService } from '../../../services/user.service';
import { PageBandComponent } from '../../shared/page-band.component';
import { gradeClass as gradeColorClass } from '../../shared/design-utils';

/** Ab so vielen Zeichen wird nach Namen gesucht – bei einem Buchstaben wären es fast alle Routen. */
export const MIN_SEARCH_LENGTH = 2;

/** Ergebnis einer Suche: Trefferliste oder Fehler. */
interface SearchResult {
  /** Schlüssel der Suche (siehe filterKey), zu der die Treffer gehören. */
  key: string;
  term: string;
  routes: ClimbingRoute[];
  failed: boolean;
}

/** Leere Suche – kein Name, kein Filter. */
const EMPTY_FILTER: RouteFilter = { search: '', minGrade: null, maxGrade: null, style: null, minLength: null, maxLength: null };

/**
 * Seite „Routen" (#227, #240): Routen über alle Gebiete und Sektoren suchen und filtern –
 * nach Namen, Grad von–bis, Stil und Länge.
 *
 * Ablauf: Jede Änderung (Suchfeld oder Filter) landet in `filterChange$`. Nach einer kurzen
 * Pause (Debounce) wird gesucht. `switchMap` bricht dabei eine noch laufende ältere Suche ab,
 * damit nie ein veraltetes Ergebnis ein neueres überschreibt. Alle Kriterien stehen zusätzlich
 * in der URL (?q=…&minGrade=…), damit Treffer und Filter nach „Zurück" noch da sind.
 */
@Component({
  selector: 'app-routes-page',
  standalone: true,
  imports: [RouterLink, FormsModule, PageBandComponent],
  templateUrl: './routes-page.component.html',
  styleUrl: './routes-page.component.css'
})
export class RoutesPageComponent implements OnInit {

  private areasService = inject(AreasService);
  private gradeService = inject(GradeService);
  private userService = inject(UserService);
  private activatedRoute = inject(ActivatedRoute);
  private router = inject(Router);

  /** Jede Änderung an Suchfeld oder Filtern. */
  private filterChange$ = new Subject<RouteFilter>();

  readonly minLength = MIN_SEARCH_LENGTH;

  /** Aktuelle Eingaben (Suchfeld + Filter). */
  filter: RouteFilter = { ...EMPTY_FILTER };

  /** Schlüssel der Suche, zu der die angezeigten Treffer gehören. */
  searchedKey = '';
  /** Suchbegriff, zu dem die angezeigten Treffer gehören (für die Überschrift). */
  searchedTerm = '';
  routes: ClimbingRoute[] = [];
  loading = false;
  errorMessage = '';

  /** Bevorzugte Gradskala des Users (ohne Login: französisch). */
  gradeScale = 'french';
  /** Grad-Tabelle (leicht → schwer) und die Auswahl in der Skala des Users. */
  private grades: Grade[] = [];
  gradeOptions: GradeOption[] = [];
  /** Stile, die bei Routen vorkommen (aus dem Backend). */
  styles: string[] = [];

  constructor() {
    // Im Konstruktor, damit takeUntilDestroyed() die Suche beim Verlassen der Seite beendet
    this.filterChange$.pipe(
      map(filter => this.normalize(filter)),
      debounceTime(350),
      distinctUntilChanged((a, b) => filterKey(a) === filterKey(b)),
      tap(filter => this.updateUrl(filter)),
      switchMap(filter => this.search(filter)),
      takeUntilDestroyed()
    ).subscribe(result => this.showResult(result));
  }

  ngOnInit(): void {
    this.areasService.getRouteStyles().subscribe({
      next: styles => this.styles = styles,
      error: () => this.styles = []
    });

    // Zuerst Skala und Grad-Tabelle laden, damit alle Treffer gleich im richtigen Grad kommen
    this.userService.getPreferredScale().subscribe(scale => {
      this.gradeScale = scale;
      this.gradeService.getGrades().subscribe({
        next: grades => {
          this.grades = grades;
          this.gradeOptions = buildGradeOptions(grades, scale);
        },
        error: () => this.gradeOptions = []
      });

      // Suche und Filter aus der URL übernehmen (z. B. nach „Zurück" von einer Route)
      this.filter = readFilter(this.activatedRoute.snapshot.queryParamMap);
      if (this.isActive) this.filterChange$.next(this.filter);
    });
  }

  // ── Eingaben ──────────────────────────────────────────────────────────────

  /** Wird bei jeder Eingabe im Suchfeld aufgerufen. */
  onSearchInput(value: string): void {
    this.filter = { ...this.filter, search: value };
    this.filterChange$.next(this.filter);
  }

  /** Wird bei jeder Änderung eines Filters aufgerufen. */
  onFilterChange(): void {
    this.filter = { ...this.filter };
    this.filterChange$.next(this.filter);
  }

  /** Nach einem Fehler dieselbe Suche noch einmal ausführen. */
  retry(): void {
    this.search(this.normalize(this.filter)).subscribe(result => this.showResult(result));
  }

  /** Suchfeld leeren, Filter bleiben. */
  clearSearch(): void {
    this.onSearchInput('');
  }

  /** Filter zurücksetzen, der Suchbegriff bleibt. */
  resetFilters(): void {
    this.filter = { ...EMPTY_FILTER, search: this.filter.search };
    this.filterChange$.next(this.filter);
  }

  /** Alles zurücksetzen – Suchbegriff und Filter. */
  resetAll(): void {
    this.filter = { ...EMPTY_FILTER };
    this.filterChange$.next(this.filter);
  }

  // ── Zustand für das Template ──────────────────────────────────────────────

  /** true, wenn der Begriff lang genug für eine Namenssuche ist. */
  get hasSearchTerm(): boolean {
    return this.filter.search.trim().length >= MIN_SEARCH_LENGTH;
  }

  /** true, wenn mindestens ein Filter gesetzt ist. */
  get hasFilter(): boolean {
    const f = this.filter;
    return !!f.minGrade || !!f.maxGrade || !!f.style || f.minLength !== null || f.maxLength !== null;
  }

  /** Gesucht wird ab 2 Zeichen oder sobald ein Filter gesetzt ist. */
  get isActive(): boolean {
    return (this.hasSearchTerm || this.hasFilter) && !this.filterError;
  }

  /** Widersprüchliche Filter – dann wird nicht gesucht, sondern ein Hinweis gezeigt. */
  get filterError(): string {
    const f = this.filter;
    if (f.minGrade && f.maxGrade && this.rank(f.minGrade) > this.rank(f.maxGrade)) {
      return 'Der Grad „von" ist schwerer als „bis".';
    }
    if ((f.minLength ?? 0) < 0 || (f.maxLength ?? 0) < 0) {
      return 'Die Länge darf nicht negativ sein.';
    }
    if (f.minLength !== null && f.maxLength !== null && f.minLength > f.maxLength) {
      return 'Die Länge „von" ist größer als „bis".';
    }
    return '';
  }

  /**
   * true, solange zu den aktuellen Eingaben noch keine Antwort da ist
   * (Debounce-Pause oder laufende Anfrage). Verhindert ein kurzes „Keine Routen gefunden".
   */
  get pending(): boolean {
    return this.isActive && (this.loading || this.searchedKey !== filterKey(this.normalize(this.filter)));
  }

  /** Bei der Grad-Auswahl „bis" nur Grade anbieten, die nicht leichter als „von" sind. */
  isBelowMinGrade(option: GradeOption): boolean {
    return !!this.filter.minGrade && this.rank(option.value) < this.rank(this.filter.minGrade);
  }

  /** Farbe des Grad-Kreises (gleiche Farben wie auf der Gebietsseite). */
  gradeClass(grade: string | null | undefined): string {
    return gradeColorClass(grade);
  }

  /** "Sandwand · Hauptwand" – Teile, die fehlen, werden weggelassen. */
  location(route: ClimbingRoute): string {
    return [route.areaName, route.sectorName].filter(Boolean).join(' · ');
  }

  // ── Suche ─────────────────────────────────────────────────────────────────

  /** Führt eine Suche aus. Fehler werden abgefangen, damit die Suche danach weiter funktioniert. */
  private search(filter: RouteFilter): Observable<SearchResult> {
    const key = filterKey(filter);
    if (!this.isActiveFilter(filter)) {
      this.loading = false;
      return of({ key, term: '', routes: [], failed: false });
    }

    this.loading = true;
    return this.areasService.searchRoutes(filter, this.gradeScale).pipe(
      map(routes => ({ key, term: filter.search, routes, failed: false })),
      catchError(() => of({ key, term: filter.search, routes: [], failed: true }))
    );
  }

  private showResult(result: SearchResult): void {
    this.loading = false;
    this.searchedKey = result.key;
    this.searchedTerm = result.term;
    this.routes = result.routes;
    this.errorMessage = result.failed ? 'Routen konnten nicht geladen werden.' : '';
  }

  /**
   * Bringt die Eingaben in die Form, die gesucht wird: Suchbegriff getrimmt und unter
   * 2 Zeichen ignoriert, leere Felder als null, Länge als ganze Zahl.
   */
  private normalize(f: RouteFilter): RouteFilter {
    const term = f.search.trim();
    return {
      search: term.length >= MIN_SEARCH_LENGTH ? term : '',
      minGrade: f.minGrade || null,
      maxGrade: f.maxGrade || null,
      style: f.style || null,
      minLength: toLength(f.minLength),
      maxLength: toLength(f.maxLength)
    };
  }

  /** Wie isActive, aber für einen bereits normalisierten Filter (ohne Template-Zustand). */
  private isActiveFilter(f: RouteFilter): boolean {
    const any = !!f.search || !!f.minGrade || !!f.maxGrade || !!f.style || f.minLength !== null || f.maxLength !== null;
    return any && !this.filterError;
  }

  /** Rang eines französischen Grads in der Grad-Tabelle (−1 = unbekannt). */
  private rank(french: string): number {
    return this.grades.findIndex(g => g.french === french);
  }

  /** Schreibt Suche und Filter in die URL, ohne einen neuen Verlaufseintrag anzulegen. */
  private updateUrl(f: RouteFilter): void {
    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams: {
        q: f.search || null,
        minGrade: f.minGrade,
        maxGrade: f.maxGrade,
        style: f.style,
        minLength: f.minLength,
        maxLength: f.maxLength
      },
      replaceUrl: true
    });
  }
}

/** Eindeutiger Text für einen Filter – zum Vergleichen, ob sich etwas geändert hat. */
function filterKey(f: RouteFilter): string {
  return JSON.stringify([f.search, f.minGrade, f.maxGrade, f.style, f.minLength, f.maxLength]);
}

/** Leeres Feld → null, sonst ganze Meter. */
function toLength(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const n = Math.round(Number(value));
  return Number.isFinite(n) ? n : null;
}

/** Suche und Filter aus den Query-Parametern der URL lesen. */
function readFilter(params: ParamMap): RouteFilter {
  return {
    search: params.get('q') ?? '',
    minGrade: params.get('minGrade'),
    maxGrade: params.get('maxGrade'),
    style: params.get('style'),
    minLength: toLength(params.get('minLength')),
    maxLength: toLength(params.get('maxLength'))
  };
}
