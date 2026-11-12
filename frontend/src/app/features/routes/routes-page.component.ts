import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { AreasService, ClimbingRoute } from '../../../services/areas.service';
import { UserService } from '../../../services/user.service';
import { PageBandComponent } from '../../shared/page-band.component';
import { gradeClass as gradeColorClass } from '../../shared/design-utils';

/** Ab so vielen Zeichen wird gesucht – bei einem Buchstaben wären es fast alle Routen. */
export const MIN_SEARCH_LENGTH = 2;

/** Ergebnis einer Suche: Trefferliste oder Fehler. */
interface SearchResult {
  term: string;
  routes: ClimbingRoute[];
  failed: boolean;
}

/**
 * Seite „Routen" (#227): Routen über alle Gebiete und Sektoren nach Namen suchen.
 *
 * Ablauf: Jede Eingabe landet in `searchInput$`. Nach einer kurzen Pause (Debounce) wird
 * gesucht. `switchMap` bricht dabei eine noch laufende ältere Suche ab, damit nie ein
 * veraltetes Ergebnis ein neueres überschreibt. Der Suchbegriff steht zusätzlich in der
 * URL (?q=…), damit die Treffer nach „Zurück" von der Routendetailseite noch da sind.
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
  private userService = inject(UserService);
  private activatedRoute = inject(ActivatedRoute);
  private router = inject(Router);

  /** Jede Eingabe ins Suchfeld. */
  private searchInput$ = new Subject<string>();

  readonly minLength = MIN_SEARCH_LENGTH;

  searchTerm = '';
  /** Begriff, zu dem die angezeigten Treffer gehören. */
  searchedTerm = '';
  routes: ClimbingRoute[] = [];
  loading = false;
  errorMessage = '';

  /** Bevorzugte Gradskala des Users (ohne Login: französisch). */
  gradeScale = 'french';

  constructor() {
    // Im Konstruktor, damit takeUntilDestroyed() die Suche beim Verlassen der Seite beendet
    this.searchInput$.pipe(
      map(term => term.trim()),
      debounceTime(350),
      distinctUntilChanged(),
      tap(term => this.updateUrl(term)),
      switchMap(term => this.search(term)),
      takeUntilDestroyed()
    ).subscribe(result => this.showResult(result));
  }

  ngOnInit(): void {
    // Zuerst die Skala laden, damit alle Treffer gleich im richtigen Grad kommen
    this.userService.getPreferredScale().subscribe(scale => {
      this.gradeScale = scale;

      const q = this.activatedRoute.snapshot.queryParamMap.get('q') ?? '';
      if (q) {
        this.searchTerm = q;
        this.searchInput$.next(q);
      }
    });
  }

  /** Wird bei jeder Eingabe im Suchfeld aufgerufen. */
  onSearchInput(value: string): void {
    this.searchTerm = value;
    this.searchInput$.next(value);
  }

  /** Nach einem Fehler dieselbe Suche noch einmal ausführen. */
  retry(): void {
    this.search(this.searchTerm.trim()).subscribe(result => this.showResult(result));
  }

  /** Suchfeld leeren und Treffer entfernen. */
  clearSearch(): void {
    this.searchTerm = '';
    this.searchInput$.next('');
  }

  /** true, wenn der Begriff lang genug für eine Suche ist. */
  get hasSearchTerm(): boolean {
    return this.searchTerm.trim().length >= MIN_SEARCH_LENGTH;
  }

  /**
   * true, solange zum eingegebenen Begriff noch keine Antwort da ist
   * (Debounce-Pause oder laufende Anfrage). Verhindert ein kurzes „Keine Routen gefunden".
   */
  get pending(): boolean {
    return this.hasSearchTerm && (this.loading || this.searchedTerm !== this.searchTerm.trim());
  }

  /** Farbe des Grad-Kreises (gleiche Farben wie auf der Gebietsseite). */
  gradeClass(grade: string | null | undefined): string {
    return gradeColorClass(grade);
  }

  /** "Sandwand · Hauptwand" – Teile, die fehlen, werden weggelassen. */
  location(route: ClimbingRoute): string {
    return [route.areaName, route.sectorName].filter(Boolean).join(' · ');
  }

  /** Führt eine Suche aus. Fehler werden abgefangen, damit die Suche danach weiter funktioniert. */
  private search(term: string): Observable<SearchResult> {
    if (term.length < MIN_SEARCH_LENGTH) {
      this.loading = false;
      return of({ term: '', routes: [], failed: false });
    }

    this.loading = true;
    return this.areasService.searchRoutes(term, this.gradeScale).pipe(
      map(routes => ({ term, routes, failed: false })),
      catchError(() => of({ term, routes: [], failed: true }))
    );
  }

  private showResult(result: SearchResult): void {
    this.loading = false;
    this.searchedTerm = result.term;
    this.routes = result.routes;
    this.errorMessage = result.failed ? 'Routen konnten nicht geladen werden.' : '';
  }

  /** Schreibt den Suchbegriff in die URL, ohne einen neuen Verlaufseintrag anzulegen. */
  private updateUrl(term: string): void {
    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams: { q: term || null },
      replaceUrl: true
    });
  }
}
