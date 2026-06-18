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

@Injectable({
  providedIn: 'root'
})
export class AreasService {

  private apiUrl = `${environment.apiUrl}/api/areas`;

  constructor(private http: HttpClient) {}

  getAreas(): Observable<Area[]> {
    return this.http.get<Area[]>(this.apiUrl);
  }

  getAreaById(id: number): Observable<Area> {
    return this.http.get<Area>(`${this.apiUrl}/${id}`);
  }
}
