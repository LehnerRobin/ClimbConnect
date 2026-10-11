import { HttpClient } from '@angular/common/http';
import { EMPTY, Observable } from 'rxjs';
import { expand, map, reduce } from 'rxjs/operators';

/** Antwort der paginierten Listen-Endpoints (/api/areas, /api/routes, /api/progress/me). */
export interface PagedResponse<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

/** Größte Seitengröße, die das Backend erlaubt. */
const MAX_PAGE_SIZE = 100;

/**
 * Lädt alle Seiten eines paginierten Endpoints nacheinander und liefert die Einträge als eine Liste.
 * Die Endpoints liefern standardmäßig nur 20 Einträge pro Seite; ohne Nachladen fehlen sonst Einträge.
 */
export function fetchAllPages<T>(http: HttpClient, url: string): Observable<T[]> {
  const separator = url.includes('?') ? '&' : '?';
  const loadPage = (page: number) =>
    http.get<PagedResponse<T>>(`${url}${separator}page=${page}&pageSize=${MAX_PAGE_SIZE}`);

  return loadPage(1).pipe(
    // Nächste Seite laden, solange noch nicht alle Einträge da sind
    expand(r => r.page * r.pageSize < r.total ? loadPage(r.page + 1) : EMPTY),
    map(r => r.items),
    reduce((all, items) => all.concat(items), [] as T[])
  );
}
