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
}

export interface Sector {
  id: number;
  name: string;
  description?: string | null;
}

export interface ClimbingRoute {
  id: number;
  name: string;
  grade?: string | null;
  length?: number | null;
  style?: string | null;
}

export interface Appointment {
  id: number;
  title: string;
  date?: string | null;
  participantCount?: number;
}

export interface AreaComment {
  id: number;
  text: string;
  authorName?: string | null;
  authorPhotoUrl?: string | null;
  createdAt?: string | null;
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

  getAreas(): Observable<Area[]> {
    return this.http.get<Area[]>(`${this.apiUrl}/areas`);
  }

  getAreaById(id: number): Observable<Area> {
    return this.http.get<Area>(`${this.apiUrl}/areas/${id}`);
  }

  getSectorsByArea(areaId: number): Observable<Sector[]> {
    return this.http.get<Sector[]>(`${this.apiUrl}/areas/${areaId}/sectors`);
  }

  getRoutesBySector(sectorId: number, scale: string): Observable<ClimbingRoute[]> {
    return this.http.get<ClimbingRoute[]>(`${this.apiUrl}/sectors/${sectorId}/routes?scale=${scale}`);
  }

  getAppointmentsByArea(areaId: number): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.apiUrl}/areas/${areaId}/appointments`);
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

}