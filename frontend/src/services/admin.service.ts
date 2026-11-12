import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Area, ClimbingRoute, Sector } from './areas.service';

/** Formulardaten zum Anlegen oder Bearbeiten eines Gebiets. */
export interface AreaSaveRequest {
  name: string;
  location?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  /** Ohne Koordinaten erscheint das Gebiet nicht auf der Karte. */
  latitude?: number | null;
  longitude?: number | null;
}

/** Formulardaten zum Anlegen oder Bearbeiten eines Sektors. */
export interface SectorSaveRequest {
  name: string;
  description?: string | null;
}

/** Formulardaten zum Anlegen oder Bearbeiten einer Route. Der Grad ist immer französisch. */
export interface RouteSaveRequest {
  name: string;
  grade: string;
  style?: string | null;
  lengthMeters?: number | null;
  description?: string | null;
}

/** Ein Safety-Report, wie ihn GET /api/reports für die Admin-Liste liefert. */
export interface AdminReport {
  id: number;
  userId: number;
  areaId?: number | null;
  routeId?: number | null;
  text: string;
  photoUrl?: string | null;
  severity: string;
  status: string;
  createdAtUtc: string;
  user?: { id: number; username: string } | null;
}

/**
 * Schreibende Admin-Aktionen (anlegen, bearbeiten, löschen) und die Report-Liste.
 * Lesen von Gebieten, Sektoren und Routen läuft über den AreasService – hier gibt es
 * dafür bewusst keine eigenen Methoden, damit es nur eine Stelle pro Anfrage gibt.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminService {

  private api = environment.apiUrl + '/api';

  constructor(private http: HttpClient) {}

  // ── Gebiete ────────────────────────────────────────────────────────────────

  createArea(data: AreaSaveRequest): Observable<Area> {
    return this.http.post<Area>(`${this.api}/areas`, data);
  }

  updateArea(id: number, data: AreaSaveRequest): Observable<Area> {
    return this.http.put<Area>(`${this.api}/areas/${id}`, data);
  }

  deleteArea(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/areas/${id}`);
  }

  // ── Sektoren ───────────────────────────────────────────────────────────────

  createSector(areaId: number, data: SectorSaveRequest): Observable<Sector> {
    return this.http.post<Sector>(`${this.api}/areas/${areaId}/sectors`, data);
  }

  updateSector(id: number, data: SectorSaveRequest): Observable<Sector> {
    return this.http.put<Sector>(`${this.api}/sectors/${id}`, data);
  }

  deleteSector(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/sectors/${id}`);
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  createRoute(sectorId: number, data: RouteSaveRequest): Observable<ClimbingRoute> {
    return this.http.post<ClimbingRoute>(`${this.api}/sectors/${sectorId}/routes`, data);
  }

  updateRoute(id: number, data: RouteSaveRequest): Observable<ClimbingRoute> {
    return this.http.put<ClimbingRoute>(`${this.api}/routes/${id}`, data);
  }

  deleteRoute(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/routes/${id}`);
  }

  // ── Reports ────────────────────────────────────────────────────────────────

  getReports(): Observable<AdminReport[]> {
    return this.http.get<AdminReport[]>(`${this.api}/reports`);
  }

  resolveReport(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/reports/${id}/status`, { status: 'Resolved' });
  }
}
