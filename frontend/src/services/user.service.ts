import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

export interface UserStats {
  totalClimbed: number;
  openProjects: number;
  favoriteArea: string | null;
  gradeProgression: { month: string; grade: string }[];
}

export interface UserListItem {
  id: number;
  username: string;
  memberSince: string;
}

export interface PublicProfile {
  id: number;
  username: string;
  memberSince: string;
  recentAscents: {
    routeName: string;
    grade: string;
    area: string;
    date: string;
    status: string;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /// Eigenes Profil abrufen (GET /api/users/me)
  getMe() {
    return this.http.get<any>(`${this.apiUrl}/api/users/me`);
  }

  /// Profil aktualisieren (PUT /api/users/me/profile)
  updateProfile(profile: { bio: string | null; preferredGradeScale: string | null }) {
    return this.http.put<any>(`${this.apiUrl}/api/users/me/profile`, profile);
  }

  /// Statistiken + Grad-Entwicklung (GET /api/users/{id}/stats)
  getStats(userId: number, scale = 'french') {
    return this.http.get<UserStats>(`${this.apiUrl}/api/users/${userId}/stats?scale=${scale}`);
  }

  /// Öffentliches Profil eines anderen Users (GET /api/users/{id}/profile)
  getPublicProfile(userId: number, scale = 'french') {
    return this.http.get<PublicProfile>(`${this.apiUrl}/api/users/${userId}/profile?scale=${scale}`);
  }

  /// Passwort ändern (PUT /api/users/me/password)
  changePassword(currentPassword: string, newPassword: string) {
    return this.http.put(`${this.apiUrl}/api/users/me/password`, { currentPassword, newPassword });
  }

  /// Öffentliche User-Liste abrufen (GET /api/users)
  getUsers() {
    return this.http.get<UserListItem[]>(`${this.apiUrl}/api/users`);
  }
}
