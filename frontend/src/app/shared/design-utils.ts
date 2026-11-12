/**
 * Kleine Hilfsfunktionen für das neue Design, die mehrere Seiten brauchen.
 */

/** Kletterfotos, die verwendet werden, wenn ein Gebiet kein eigenes Bild hat. */
export const AREA_FALLBACK_PHOTOS = [
  '/assets/home/gebiet-grat.jpg',
  '/assets/home/gebiet-fels-wald.jpg',
  '/assets/home/gebiet-schlucht.jpg',
  '/assets/home/hero-kletterinnen.jpg'
];

/** Wählt für ein Gebiet ohne eigenes Bild immer dasselbe Ersatzfoto (abhängig von der ID). */
export function areaFallbackPhoto(areaId: number): string {
  return AREA_FALLBACK_PHOTOS[Math.abs(areaId) % AREA_FALLBACK_PHOTOS.length];
}

/** Echtes Bild des Gebiets oder – wenn es keins hat – ein Ersatzfoto (immer dasselbe pro Gebiet). */
export function areaImage(area: { id: number; imageUrl?: string | null }): string {
  return area.imageUrl || areaFallbackPhoto(area.id);
}

/**
 * Fehler-Handler für <img (error)="…">: Lädt das Gebietsbild nicht, wird das Ersatzfoto gezeigt.
 * Das data-Attribut merkt sich, dass schon ersetzt wurde. Sonst würde sich der Handler endlos
 * selbst aufrufen, falls auch das Ersatzfoto nicht lädt. (img.onerror = null hilft hier nicht,
 * weil Angular den Handler per addEventListener anhängt.)
 */
export function showAreaFallbackPhoto(event: Event, areaId: number): void {
  const img = event.target as HTMLImageElement;
  if (img.dataset['fallback']) return;
  img.dataset['fallback'] = 'true';
  img.src = areaFallbackPhoto(areaId);
}

/**
 * Liefert die CSS-Klasse für den farbigen Grad-Kreis (cc-grade-4 … cc-grade-8).
 * Funktioniert für alle drei Skalen, die der User einstellen kann:
 * Französisch (6a+), Amerikanisch (5.10b) und UIAA (VI+).
 */
export function gradeClass(grade: string | null | undefined): string {
  const value = (grade ?? '').trim().toUpperCase();
  if (!value) {
    return 'cc-grade-5';
  }

  // Amerikanisch: 5.9, 5.10a, 5.12c …
  const yds = /^5\.(\d+)/.exec(value);
  if (yds) {
    const n = parseInt(yds[1], 10);
    const level = n <= 7 ? 4 : n <= 9 ? 5 : n === 10 ? 6 : n <= 12 ? 7 : 8;
    return `cc-grade-${level}`;
  }

  // Französisch: 4c, 6a+, 7b …
  const french = /^(\d)/.exec(value);
  if (french) {
    const level = Math.min(8, Math.max(4, parseInt(french[1], 10)));
    return `cc-grade-${level}`;
  }

  // UIAA: römische Zahl, z. B. V+, VII-, VIII
  const roman = /^(IX|X|VIII|VII|VI|V|IV|III|II|I)/.exec(value);
  if (roman) {
    const uiaa: Record<string, number> = { I: 4, II: 4, III: 4, IV: 4, V: 5, VI: 5, VII: 6, VIII: 7, IX: 8, X: 8 };
    return `cc-grade-${uiaa[roman[1]]}`;
  }

  return 'cc-grade-6';
}

/** CSS-Klasse für die Status-Plakette einer Begehung (Rotpunkt, Flash, Onsight, Projekt). */
export function ascentStatusClass(status: string | null | undefined): string {
  const value = (status ?? '').toLowerCase();
  if (value.includes('rotpunkt') || value.includes('redpoint')) return 'cc-status cc-status-rotpunkt';
  if (value.includes('flash')) return 'cc-status cc-status-flash';
  if (value.includes('onsight')) return 'cc-status cc-status-onsight';
  return 'cc-status cc-status-projekt';
}
