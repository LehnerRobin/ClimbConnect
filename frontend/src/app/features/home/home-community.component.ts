import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Community-Abschnitt der Startseite: Überschrift „Gemeinsam am Fels" mit Link zur
 * Kletterer-Suche und drei Polaroid-Fotos, die an einem Kletterseil hängen.
 *
 * Reine Anzeige-Komponente ohne Daten – ausgelagert, damit die Styles der
 * Startseite übersichtlich (und unter dem CSS-Budget von 20 kB) bleiben.
 */
@Component({
  selector: 'app-home-community',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home-community.component.html',
  styleUrl: './home-community.component.css'
})
export class HomeCommunityComponent {}
