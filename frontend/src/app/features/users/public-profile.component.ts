import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserService, PublicProfile } from '../../../services/user.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './public-profile.component.html',
  styleUrl: './public-profile.component.css'
})
export class PublicProfileComponent implements OnInit {

  profile?: PublicProfile;
  loading = true;
  errorMessage = '';

  constructor(
    private activatedRoute: ActivatedRoute,
    private userService: UserService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const id = Number(this.activatedRoute.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Ungültige User-ID.';
      this.loading = false;
      return;
    }

    if (this.authService.isAuthenticated()) {
      this.userService.getMe().subscribe({
        next: (data) => this.loadProfile(id, data.preferredGradeScale ?? 'french'),
        error: () => this.loadProfile(id, 'french')
      });
    } else {
      this.loadProfile(id, 'french');
    }
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

  /** Farbklasse passend zur Begehungsart (gesendet / Projekt / Toprope). */
  statusClass(status: string): string {
    switch (status) {
      case 'Rotpunkt':
      case 'Flash':
      case 'Onsight':
        return 'status-sent';
      case 'Projekt':
        return 'status-project';
      default:
        return 'status-toprope';
    }
  }
}
