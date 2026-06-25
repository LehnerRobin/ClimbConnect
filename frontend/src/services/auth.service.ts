import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../environments/environment';

interface JwtPayload {
  exp?: number;
  role?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  sub?: string;
  email?: string;
  username?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly tokenKey = 'token';
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(response => this.saveToken(response))
    );
  }

  register(user: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, user).pipe(
      tap(response => this.saveToken(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    const payload = this.decodeToken(token);
    if (!payload?.exp) {
      return false;
    }

    return Date.now() < payload.exp * 1000;
  }

  hasRole(role: string): boolean {
    return this.getRole() === role.toLowerCase();
  }

  getRole(): string | null {
    if (!this.isAuthenticated()) {
      return null;
    }

    const token = this.getToken();
    if (!token) {
      return null;
    }

    const payload = this.decodeToken(token);
    const role = payload?.role
      ?? payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    return role?.toLowerCase() ?? null;
  }

  getUsername(): string | null {
    if (!this.isAuthenticated()) {
      return null;
    }

    const token = this.getToken();
    if (!token) {
      return null;
    }

    const payload = this.decodeToken(token);
    return payload?.username ?? payload?.email ?? null;
  }

  private saveToken(response: any): void {
    if (response?.token) {
      localStorage.setItem(this.tokenKey, response.token);
    }
  }

  private decodeToken(token: string): JwtPayload | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = atob(normalized);
      return JSON.parse(decoded) as JwtPayload;
    } catch {
      return null;
    }
  }
}
