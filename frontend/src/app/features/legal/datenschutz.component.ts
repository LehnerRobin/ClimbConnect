import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageBandComponent } from '../../shared/page-band.component';
import { LEGAL } from './legal-info';

/**
 * Datenschutzerklärung (#231). Beschreibt, was die App tatsächlich speichert – bei neuen
 * Funktionen, die personenbezogene Daten verarbeiten, hier ergänzen und das Datum in
 * legal-info.ts anpassen.
 */
@Component({
  selector: 'app-datenschutz',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageBandComponent],
  templateUrl: './datenschutz.component.html',
  styleUrl: './legal.css'
})
export class DatenschutzComponent {
  readonly legal = LEGAL;
}
