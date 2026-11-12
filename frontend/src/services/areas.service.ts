import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../environments/environment';
import { fetchAllPages, PagedResponse } from './paging';

export interface Area {
  id: number;
  name: string;
  location?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  latitude?: number | null;
  longitude?: number | null;
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
  /** Nur bei der Routensuche (GET /api/routes) dabei. */
  areaId?: number | null;
  /** Nur bei der Routensuche (GET /api/routes) dabei. */
  areaName?: string | null;
  name: string;
  grade?: string | null;
  lengthMeters?: number | null;
  style?: string | null;
  description?: string | null;
}

export interface RouteComment {
  id: number;
  userId?: number;
  text: string;
  authorName?: string | null;
  user?: { id?: number; username?: string | null };
  photoUrl?: string | null;
  createdAtUtc?: string | null;
}

export interface AreaComment {
  id: number;
  userId?: number;
  text: string;
  authorName?: string | null;
  user?: { id?: number; username?: string | null };
  authorPhotoUrl?: string | null;
  createdAt?: string | null;
  createdAtUtc?: string | null;
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
  createdByUserId?: number;
  title: string;
  date?: string | null;
  meetingPoint?: string | null;
  description?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
  participantCount?: number;
  /** Ob der eingeloggte User an diesem Termin teilnimmt (kommt von der Terminliste). */
  isSubscribed?: boolean;
  /** Kommt von /upcoming und /me mit, damit das Gebiet nicht extra geladen werden muss. */
  areaName?: string | null;
  areaLocation?: string | null;
  /** Nur bei /me: true, wenn der eingeloggte User den Termin erstellt hat. */
  isCreator?: boolean;
}

/** Teilnehmer eines Termins (aus GET /api/appointments/{id}). */
export interface AppointmentParticipant {
  userId: number;
  username: string;
  comment: string | null;
}

/** Antwort von GET /api/appointments/{id}: Termin mit Teilnehmern und Durchschnittsgrad. */
export interface AppointmentDetail {
  id: number;
  areaId: number;
  createdByUserId: number;
  title: string;
  date: string | null;
  meetingPoint: string | null;
  description: string | null;
  minParticipants: number | null;
  maxParticipants: number | null;
  participantCount: number;
  /** Durchschnitt der besten Grade aller Teilnehmer, in der angefragten Skala. */
  averageGrade: string | null;
  participants: AppointmentParticipant[];
}

/** Antwort von GET /api/routes/{id}/community-grade. */
export interface CommunityGrade {
  /** null, solange noch niemand einen Grad abgegeben hat. */
  communityGrade: string | null;
  voteCount: number;
}

export interface AppointmentCreateRequest {
  title: string;
  date: string;
  meetingPoint?: string | null;
  description?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
}

export interface AppointmentUpdateRequest {
  title: string;
  date: string;
  meetingPoint?: string | null;
  description?: string | null;
  minParticipants?: number | null;
  maxParticipants?: number | null;
}

export interface ProgressEntry {
  id: number;
  userId?: number;
  routeId?: number;
  route?: ClimbingRoute | null;
  status: string;
  climbingStyle: string;
  attempts: number;
  notes?: string | null;
  date: string;
  subjectiveGrade?: string | null;
  subjectiveGradeComment?: string | null;
  createdAtUtc?: string | null;
}

export interface ProgressCreateRequest {
  routeId: number;
  status: string;
  climbingStyle: string;
  attempts: number;
  notes?: string | null;
  date: string;
  subjectiveGrade?: string | null;
  subjectiveGradeComment?: string | null;
}

export interface ProgressUpdateRequest {
  status: string;
  climbingStyle: string;
  attempts: number;
  notes?: string | null;
  date: string;
  subjectiveGrade?: string | null;
  subjectiveGradeComment?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AreasService {

  private apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  // ── Gebiete ────────────────────────────────────────────────────────────────

  /** Alle Gebiete (lädt alle Seiten nach). */
  getAreas(search?: string): Observable<Area[]> {
    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    return fetchAllPages<Area>(this.http, `${this.apiUrl}/areas${params}`);
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

  updateAppointment(id: number, appointment: AppointmentUpdateRequest): Observable<Appointment> {
    return this.http.put<Appointment>(`${this.apiUrl}/appointments/${id}`, appointment);
  }

  deleteAppointment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/appointments/${id}`);
  }

  /** Nächste Termine über alle Gebiete in einer einzigen Anfrage. */
  getUpcomingAppointments(limit = 10): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.apiUrl}/appointments/upcoming?limit=${limit}`);
  }

  /** Eigene Termine (erstellt oder beigetreten). Mit all=true auch vergangene. */
  getMyAppointments(all = false): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.apiUrl}/appointments/me?all=${all}`);
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  getRoutesBySector(sectorId: number, scale: string): Observable<ClimbingRoute[]> {
    return this.http.get<ClimbingRoute[]>(`${this.apiUrl}/sectors/${sectorId}/routes?scale=${scale}`);
  }

  /** Alle Routen, deren Name die Suche enthält (lädt alle Seiten nach). */
  searchRoutes(search: string, scale = 'french'): Observable<ClimbingRoute[]> {
    return fetchAllPages<ClimbingRoute>(
      this.http, `${this.apiUrl}/routes?search=${encodeURIComponent(search)}&scale=${scale}`
    );
  }

  /** Gesamtanzahl der Routen – fragt nur eine Seite mit einem Eintrag ab und liest `total`. */
  getRouteCount(): Observable<number> {
    return this.http.get<PagedResponse<ClimbingRoute>>(`${this.apiUrl}/routes?pageSize=1`).pipe(
      map(r => r.total)
    );
  }

  getRouteById(id: number, scale = 'french'): Observable<ClimbingRoute> {
    return this.http.get<ClimbingRoute>(`${this.apiUrl}/routes/${id}?scale=${scale}`);
  }

  getCommunityGrade(routeId: number, scale = 'french'): Observable<CommunityGrade> {
    return this.http.get<CommunityGrade>(`${this.apiUrl}/routes/${routeId}/community-grade?scale=${scale}`);
  }

  getRouteComments(routeId: number): Observable<RouteComment[]> {
    return this.http.get<RouteComment[]>(`${this.apiUrl}/routes/${routeId}/comments`);
  }

  addAreaComment(areaId: number, text: string, photoUrl?: string | null): Observable<AreaComment> {
    return this.http.post<AreaComment>(`${this.apiUrl}/areas/${areaId}/comments`, { text, photoUrl });
  }

  addRouteComment(routeId: number, text: string, photoUrl?: string | null): Observable<RouteComment> {
    return this.http.post<RouteComment>(`${this.apiUrl}/routes/${routeId}/comments`, { text, photoUrl });
  }

  deleteComment(commentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/comments/${commentId}`);
  }

  getRouteReports(routeId: number): Observable<SafetyReport[]> {
    return this.http.get<SafetyReport[]>(`${this.apiUrl}/routes/${routeId}/reports`);
  }

  // ── Safety Reports ──────────────────────────────────────────────────────────

  createReport(data: { routeId?: number; areaId?: number; text: string; severity: string; photoUrl?: string | null }): Observable<SafetyReport> {
    return this.http.post<SafetyReport>(`${this.apiUrl}/reports`, data);
  }

  // ── Upload ─────────────────────────────────────────────────────────────────

  uploadImage(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/upload`, form);
  }

  // ── Appointments Subscribe ──────────────────────────────────────────────────

  subscribeToAppointment(id: number, comment?: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/appointments/${id}/subscribe`, { comment: comment ?? null });
  }

  unsubscribeFromAppointment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/appointments/${id}/subscribe`);
  }

  getAppointmentById(id: number, scale = 'french'): Observable<AppointmentDetail> {
    return this.http.get<AppointmentDetail>(`${this.apiUrl}/appointments/${id}?scale=${scale}`);
  }

  // ── Progress ───────────────────────────────────────────────────────────────

  /** Alle eigenen Begehungen (lädt alle Seiten nach). */
  getMyProgress(): Observable<ProgressEntry[]> {
    return fetchAllPages<ProgressEntry>(this.http, `${this.apiUrl}/progress/me`);
  }

  getProgressById(id: number): Observable<ProgressEntry> {
    return this.http.get<ProgressEntry>(`${this.apiUrl}/progress/${id}`);
  }

  createProgress(data: ProgressCreateRequest): Observable<ProgressEntry> {
    return this.http.post<ProgressEntry>(`${this.apiUrl}/progress`, data);
  }

  updateProgress(id: number, data: ProgressUpdateRequest): Observable<ProgressEntry> {
    return this.http.put<ProgressEntry>(`${this.apiUrl}/progress/${id}`, data);
  }

  deleteProgress(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/progress/${id}`);
  }
}
