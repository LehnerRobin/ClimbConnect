import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

/**
 * "2/4" – angemeldete Leute / Maximum. Ohne Maximum ("Open End"): "2/∞".
 * Als eigene Funktion, damit sie auch ohne Komponente (z. B. in Tests) nutzbar ist.
 */
export function dudesLabel(count: number | null | undefined, max: number | null | undefined): string {
  return `${count ?? 0}/${max ? max : '∞'}`;
}

/** Standard-Minimum wie im Backend (Appointment.DefaultMinParticipants). */
export const DEFAULT_MIN_DUDES = 2;

/**
 * true, solange weniger Leute angemeldet sind als das Minimum (#254) –
 * dann braucht der Termin noch Leute ("Send Dudes!").
 */
export function needsDudes(count: number | null | undefined, min: number | null | undefined): boolean {
  return (count ?? 0) < (min ?? DEFAULT_MIN_DUDES);
}

/** true, wenn ein Maximum gesetzt ist und es erreicht wurde. */
export function isFullyBooked(count: number | null | undefined, max: number | null | undefined): boolean {
  return !!max && (count ?? 0) >= max;
}

/**
 * Einheitliche Teilnehmer-Anzeige für Termine (#228), überall gleich:
 * "2/4 Dudes", dazu "Du bist dabei", "ausgebucht" und – solange das Minimum
 * nicht erreicht ist – groß "Send Dudes!" (#254).
 *
 * Schrift und Farbe kommen vom umgebenden Element, damit die Anzeige auf dem dunklen
 * Foto-Kopfbereich genauso funktioniert wie auf hellen Ticket-Karten.
 *
 * Verwendung:
 *   <app-dudes [count]="a.participantCount" [min]="a.minParticipants" [max]="a.maxParticipants"
 *              [subscribed]="a.isSubscribed" />
 */
@Component({
  selector: 'app-dudes',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="dudes">
      <strong class="dudes-count tnum">{{ label }}</strong> Dudes
      @if (sendDudes) {
        <span class="dudes-tag dudes-send" title="Hier fehlen noch Leute – komm mit!">Send Dudes!</span>
      }
      @if (subscribed) {
        <span class="dudes-tag dudes-in">Du bist dabei</span>
      }
      @if (full) {
        <span class="dudes-tag dudes-full">ausgebucht</span>
      }
    </span>
  `,
  styles: [`
    :host { display: inline-flex; }
    .dudes { display: inline-flex; flex-wrap: wrap; align-items: center; gap: 6px; }
    .dudes-count { font-weight: 800; }
    .dudes-tag {
      padding: 2px 8px;
      border-radius: 999px;
      font-size: 0.85em;
      font-weight: 800;
      letter-spacing: 0.02em;
      text-transform: none;
      white-space: nowrap;
    }
    .dudes-in   { background: var(--cc-teal); color: #fff; }
    .dudes-full { background: var(--cc-ink);  color: var(--cc-paper); }
    /* Bewusst auffällig: der Termin braucht noch Leute */
    .dudes-send {
      background: var(--cc-rust);
      color: #fff;
      font-size: 1em;
      padding: 3px 10px;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      box-shadow: 0 0 0 2px rgba(168, 67, 27, 0.25);
    }
  `]
})
export class DudesComponent {

  /** Anzahl angemeldeter Leute. */
  @Input() count: number | null | undefined = 0;

  /** Minimum; leer = Standard 2. */
  @Input() min: number | null | undefined = null;

  /** Vergangene Termine brauchen keine Leute mehr – dann kein "Send Dudes!". */
  @Input() past = false;

  /** Maximum; leer = "Open End". */
  @Input() max: number | null | undefined = null;

  /** Ist der eingeloggte User angemeldet? Leer/false = kein Kennzeichen. */
  @Input() subscribed: boolean | null | undefined = false;

  get label(): string {
    return dudesLabel(this.count, this.max);
  }

  get sendDudes(): boolean {
    return !this.past && needsDudes(this.count, this.min);
  }

  get full(): boolean {
    return isFullyBooked(this.count, this.max);
  }
}
