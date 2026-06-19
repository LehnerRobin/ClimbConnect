import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserService, PublicProfile } from '../../../services/user.service';

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
    private userService: UserService
  ) {}

  ngOnInit(): void {
    const id = Number(this.activatedRoute.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Ungültige User-ID.';
      this.loading = false;
      return;
    }

    this.userService.getPublicProfile(id, 'french').subscribe({
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
}
