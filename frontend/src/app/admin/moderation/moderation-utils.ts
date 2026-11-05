/** Ganze Tage bis zu einem Zeitpunkt (mindestens 0) – für "wird in X Tagen gelöscht". */
export function daysUntil(isoDate: string): number {
  const ms = new Date(isoDate).getTime() - Date.now();
  return Math.max(0, Math.ceil(ms / (24 * 60 * 60 * 1000)));
}
