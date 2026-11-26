/**
 * Datums-Hilfen für den Kalender – immer nach Wiener Ortszeit.
 * Termine sind im Backend in UTC gespeichert (siehe #198). Für den Kalender zählt aber,
 * an welchem Tag ein Termin in Wien stattfindet, auch wenn der Browser in einer anderen
 * Zeitzone läuft.
 */

export const VIENNA_TZ = 'Europe/Vienna';

/** Formatter einmal anlegen – Intl.DateTimeFormat ist teuer in der Erstellung. */
const keyFormatter = new Intl.DateTimeFormat('en-CA', {
  timeZone: VIENNA_TZ, year: 'numeric', month: '2-digit', day: '2-digit'
});
const timeFormatter = new Intl.DateTimeFormat('de-AT', {
  timeZone: VIENNA_TZ, hour: '2-digit', minute: '2-digit'
});

/** Tages-Schlüssel "YYYY-MM-DD" eines Zeitpunkts nach Wiener Ortszeit. */
export function viennaDayKey(value: string | Date): string {
  return keyFormatter.format(typeof value === 'string' ? new Date(value) : value);
}

/** Heutiger Tag in Wien als "YYYY-MM-DD". */
export function viennaTodayKey(): string {
  return viennaDayKey(new Date());
}

/** Uhrzeit "HH:mm" nach Wiener Ortszeit. */
export function viennaTime(value: string): string {
  return timeFormatter.format(new Date(value));
}

/** Schlüssel "YYYY-MM-DD" aus Jahr, Monat (0–11) und Tag. */
export function dayKey(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

export interface CalendarDay {
  key: string;
  day: number;
  /** false = Tag aus dem Vor- oder Folgemonat (grau dargestellt). */
  inMonth: boolean;
}

/**
 * Alle Tage, die das Monatsraster zeigt: volle Wochen von Montag bis Sonntag.
 * Reine Kalenderrechnung mit Date.UTC – unabhängig von der Zeitzone des Browsers.
 */
export function monthGrid(year: number, month: number): CalendarDay[] {
  const first = new Date(Date.UTC(year, month, 1));
  // getUTCDay: 0 = Sonntag … 6 = Samstag → Abstand zum Montag davor
  const offset = (first.getUTCDay() + 6) % 7;
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const weeks = Math.ceil((offset + daysInMonth) / 7);

  const days: CalendarDay[] = [];
  for (let i = 0; i < weeks * 7; i++) {
    const d = new Date(Date.UTC(year, month, 1 - offset + i));
    days.push({
      key: dayKey(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()),
      day: d.getUTCDate(),
      inMonth: d.getUTCMonth() === month
    });
  }
  return days;
}

/** Jahr und Monat (0–11) des heutigen Tages in Wien. */
export function viennaCurrentMonth(): { year: number; month: number } {
  const [y, m] = viennaTodayKey().split('-').map(Number);
  return { year: y, month: m - 1 };
}
