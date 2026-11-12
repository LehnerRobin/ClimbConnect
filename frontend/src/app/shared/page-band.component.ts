import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

/**
 * Foto-Kopfbereich für alle Unterseiten im neuen Design:
 * Hintergrundfoto mit dunklem Verlauf, darüber der Seiteninhalt (Titel, Buttons …)
 * und unten der „Papier-Riss" als Übergang in den hellen Seitenteil.
 *
 * Verwendung:
 *   <app-page-band image="/assets/home/gebiet-grat.jpg">
 *     <p class="cc-eyebrow">…</p>
 *     <h1 class="cc-h1">…</h1>
 *   </app-page-band>
 */
@Component({
  selector: 'app-page-band',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-band.component.html',
  styleUrl: './page-band.component.css'
})
export class PageBandComponent {

  /** Hintergrundfoto (Pfad oder URL). */
  @Input() image = '/assets/home/gebiet-grat.jpg';

  /** Bildausschnitt, z. B. "center 30%" oder "66% center". */
  @Input() position = 'center';

  /** "normal" für Unterseiten, "tall" für Seiten, die fast nur aus dem Kopfbereich bestehen (Login, Fehler). */
  @Input() size: 'normal' | 'tall' = 'normal';
}
