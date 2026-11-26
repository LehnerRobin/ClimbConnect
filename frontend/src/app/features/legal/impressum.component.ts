import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageBandComponent } from '../../shared/page-band.component';
import { LEGAL } from './legal-info';

/**
 * Impressum (#231): Offenlegung für eine kleine, nicht-kommerzielle Website nach § 25 Abs. 5 MedienG.
 * Name und Wohnort reichen, solange es keine Werbung und keine Bezahlfunktion gibt.
 */
@Component({
  selector: 'app-impressum',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageBandComponent],
  templateUrl: './impressum.component.html',
  styleUrl: './legal.css'
})
export class ImpressumComponent {
  readonly legal = LEGAL;
}
