import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

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

  /// Statistiken eines Users abrufen (GET /api/users/{id}/stats)
  getStats(userId: number) {
    return this.http.get<any>(`${this.apiUrl}/api/users/${userId}/stats`);
  }
}
