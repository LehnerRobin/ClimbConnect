import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  private api = environment.apiUrl + '/api';

  constructor(private http: HttpClient) {}

  // ── Gebiete ────────────────────────────────────────────────────────────────

  getAreas(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/areas`);
  }

  createArea(data: { name: string; location?: string | null; description?: string | null; imageUrl?: string | null }): Observable<any> {
    return this.http.post(`${this.api}/areas`, data);
  }

  updateArea(id: number, data: { name: string; location?: string | null; description?: string | null; imageUrl?: string | null }): Observable<any> {
    return this.http.put(`${this.api}/areas/${id}`, data);
  }

  deleteArea(id: number): Observable<any> {
    return this.http.delete(`${this.api}/areas/${id}`);
  }

  // ── Sektoren ───────────────────────────────────────────────────────────────

  getSectorsByArea(areaId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/areas/${areaId}/sectors`);
  }

  createSector(areaId: number, data: { name: string; description?: string | null }): Observable<any> {
    return this.http.post(`${this.api}/areas/${areaId}/sectors`, data);
  }

  updateSector(id: number, data: { name: string; description?: string | null }): Observable<any> {
    return this.http.put(`${this.api}/sectors/${id}`, data);
  }

  deleteSector(id: number): Observable<any> {
    return this.http.delete(`${this.api}/sectors/${id}`);
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  getRoutesBySector(sectorId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/sectors/${sectorId}/routes`);
  }

  createRoute(sectorId: number, data: { name: string; grade: string; style?: string | null; lengthMeters?: number | null; description?: string | null }): Observable<any> {
    return this.http.post(`${this.api}/sectors/${sectorId}/routes`, data);
  }

  updateRoute(id: number, data: { name: string; grade: string; style?: string | null; lengthMeters?: number | null; description?: string | null }): Observable<any> {
    return this.http.put(`${this.api}/routes/${id}`, data);
  }

  deleteRoute(id: number): Observable<any> {
    return this.http.delete(`${this.api}/routes/${id}`);
  }

  // ── Datenbank zurücksetzen (Gefahrenzone) ──────────────────────────────────

  initDatabase(): Observable<any> {
    return this.http.put(`${this.api}/Admin`, null);
  }
}
