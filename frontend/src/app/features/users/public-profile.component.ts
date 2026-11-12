import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserService, PublicProfile } from '../../../services/user.service';
import { PageBandComponent } from '../../shared/page-band.component';
import { ascentStatusClass, gradeClass as gradeColorClass } from '../../shared/design-utils';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, PageBandComponent],
  templateUrl: './public-profile.component.html',
  styleUrl: './public-profile.component.css'
})
export class PublicProfileComponent implements OnInit {

  profile?: PublicProfile;
  loading = true;
  errorMessage = '';

  constructor(
    private activatedRoute: ActivatedRoute,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    const id = Number(this.activatedRoute.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Ungültige User-ID.';
      this.loading = false;
      return;
    }

    // Gradskala kommt zentral aus dem UserService (ohne Login: französisch)
    this.userService.getPreferredScale().subscribe(scale => this.loadProfile(id, scale));
  }

  private loadProfile(id: number, scale: string): void {
    this.userService.getPublicProfile(id, scale).subscribe({
      next: (data) => {
        this.profile = data;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Profil konnte nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Rotpunkt: 'Rotpunkt',
      Flash:    'Flash',
      Onsight:  'Onsight',
      Projekt:  'Projekt',
      Toprope:  'Toprope'
    };
    return map[status] ?? status;
  }

  /** Farbige Status-Plakette (Rotpunkt, Flash, Onsight, Projekt) aus dem neuen Design. */
  statusClass(status: string): string {
    return ascentStatusClass(status);
  }

  /** Klassen für den farbigen Grad-Kreis. */
  gradeClass(grade: string | null | undefined): string {
    return 'cc-grade ' + gradeColorClass(grade);
  }
}
