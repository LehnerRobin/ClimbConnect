import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface Area {
  id: number;
  name: string;
  location?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  todayVisitors?: number;
  todayAppointments?: number;
  createdAtUtc?: string;
}


export interface Sector {
  id: number;
  name: string;
  description?: string | null;
}

export interface ClimbingRoute {
  id: number;
  sectorId: number;
  sectorName?: string | null;
  name: string;
  grade?: string | null;
  lengthMeters?: number | null;
  style?: string | null;
  description?: string | null;
}

export interface RouteComment {
  id: number;
  text: string;
  authorName?: string | null;
  photoUrl?: string | null;
  createdAtUtc?: string | null;
}

export interface AreaComment {
  id: number;
  text: string;
  authorName?: string | null;
  authorPhotoUrl?: string | null;
  createdAt?: string | null;
}

export interface SafetyReport {
  id: number;
  text: string;
  severity: string;
  status: string;
  photoUrl?: string | null;
  createdAtUtc?: string | null;
  authorName?: string | null;
}

export interface Appointment {
  id: number;
  areaId?: number;
  title: string;
  date?: string | null;
  meetingPoint?: string | null;
  description?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
  participantCount?: number;
}

export interface AppointmentCreateRequest {
  title: string;
  date: string;
  meetingPoint?: string | null;
  description?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class AreasService {

  private apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  // ── Gebiete ────────────────────────────────────────────────────────────────

  getAreas(search?: string): Observable<Area[]> {
    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http.get<Area[]>(`${this.apiUrl}/areas${params}`);
  }

  getAreaById(id: number): Observable<Area> {
    return this.http.get<Area>(`${this.apiUrl}/areas/${id}`);
  }

  getSectorsByArea(areaId: number): Observable<Sector[]> {
    return this.http.get<Sector[]>(`${this.apiUrl}/areas/${areaId}/sectors`);
  }

  getAppointmentsByArea(areaId: number, all = false): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.apiUrl}/areas/${areaId}/appointments?all=${all}`);
  }

  getCommentsByArea(areaId: number): Observable<AreaComment[]> {
    return this.http.get<AreaComment[]>(`${this.apiUrl}/areas/${areaId}/comments`);
  }

  createAppointment(areaId: number, appointment: AppointmentCreateRequest): Observable<Appointment> {
    return this.http.post<Appointment>(
      `${this.apiUrl}/areas/${areaId}/appointments`,
      appointment
    );
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  getRoutesBySector(sectorId: number, scale: string): Observable<ClimbingRoute[]> {
    return this.http.get<ClimbingRoute[]>(`${this.apiUrl}/sectors/${sectorId}/routes?scale=${scale}`);
  }

  searchRoutes(search: string, scale = 'french'): Observable<ClimbingRoute[]> {
    return this.http.get<ClimbingRoute[]>(
      `${this.apiUrl}/routes?search=${encodeURIComponent(search)}&scale=${scale}`
    );
  }

  getRoutes(search = '', scale = 'french'): Observable<ClimbingRoute[]> {
    const searchParam = search ? `search=${encodeURIComponent(search)}&` : '';
    return this.http.get<ClimbingRoute[]>(`${this.apiUrl}/routes?${searchParam}scale=${scale}`);
  }

  getRouteById(id: number, scale = 'french'): Observable<ClimbingRoute> {
    return this.http.get<ClimbingRoute>(`${this.apiUrl}/routes/${id}?scale=${scale}`);
  }

  getCommunityGrade(routeId: number, scale = 'french'): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/routes/${routeId}/community-grade?scale=${scale}`);
  }

  getRouteComments(routeId: number): Observable<RouteComment[]> {
    return this.http.get<RouteComment[]>(`${this.apiUrl}/routes/${routeId}/comments`);
  }

  addRouteComment(routeId: number, text: string, photoUrl?: string | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/routes/${routeId}/comments`, { text, photoUrl });
  }

  getRouteReports(routeId: number): Observable<SafetyReport[]> {
    return this.http.get<SafetyReport[]>(`${this.apiUrl}/routes/${routeId}/reports`);
  }

  // ── Safety Reports ──────────────────────────────────────────────────────────

  createReport(data: { routeId?: number; areaId?: number; text: string; severity: string; photoUrl?: string | null }): Observable<any> {
    return this.http.post(`${this.apiUrl}/reports`, data);
  }

  // ── Upload ─────────────────────────────────────────────────────────────────

  uploadImage(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/upload`, form);
  }

  // ── Appointments Subscribe ──────────────────────────────────────────────────

  subscribeToAppointment(id: number, comment?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/appointments/${id}/subscribe`, { comment: comment ?? null });
  }

  unsubscribeFromAppointment(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/appointments/${id}/subscribe`);
  }

  getAppointmentById(id: number, scale = 'french'): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/appointments/${id}?scale=${scale}`);
  }

  // ── Progress ───────────────────────────────────────────────────────────────

  getMyProgress(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/progress/me`);
  }

  createProgress(data: {
    routeId: number;
    status: string;
    climbingStyle: string;
    attempts: number;
    notes?: string | null;
    date: string;
    subjectiveGrade?: string | null;
    subjectiveGradeComment?: string | null;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/progress`, data);
  }
}
