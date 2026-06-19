import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {

  user = {
    id: 0,
    username: '',
    email: '',
    bio: '',
    preferredGradeScale: 'french',
    memberSince: ''
  };

  stats = {
    totalAscents: 0,
    openProjects: 0,
    favoriteArea: '-'
  };

  saveSuccess = false;
  saveError = '';

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.userService.getMe().subscribe({
      next: (data) => {
        this.user.id                = data.id;
        this.user.username          = data.username;
        this.user.email             = data.email;
        this.user.bio               = data.bio ?? '';
        this.user.preferredGradeScale = data.preferredGradeScale ?? 'french';
        this.user.memberSince       = data.memberSince;

        // Statistiken laden sobald wir die userId kennen
        this.userService.getStats(data.id).subscribe({
          next: (s) => {
            this.stats.totalAscents = s.totalAscents ?? 0;
            this.stats.openProjects = s.openProjects ?? 0;
            this.stats.favoriteArea = s.favoriteArea ?? '-';
          },
          error: () => { /* Statistiken optional */ }
        });
      },
      error: () => { /* Nicht eingeloggt — Guard leitet weiter */ }
    });
  }

  saveProfile(): void {
    this.saveSuccess = false;
    this.saveError   = '';

    this.userService.updateProfile({
      bio: this.user.bio || null,
      preferredGradeScale: this.user.preferredGradeScale || null
    }).subscribe({
      next: (data) => {
        this.user.bio               = data.bio ?? '';
        this.user.preferredGradeScale = data.preferredGradeScale ?? 'french';
        this.saveSuccess = true;
      },
      error: (err) => {
        this.saveError = err?.error?.error ?? 'Profil konnte nicht gespeichert werden.';
      }
    });
  }
}
