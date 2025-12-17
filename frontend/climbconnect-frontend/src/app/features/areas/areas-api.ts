import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Area {
  id: number;
  name: string;
}

export interface AreaDetail extends Area {
  description?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AreasApiService {
  constructor(private http: HttpClient) {}

  getAreas(): Observable<Area[]> {
    return this.http.get<Area[]>('/api/areas');
  }

  getArea(id: number): Observable<AreaDetail> {
    return this.http.get<AreaDetail>(`/api/areas/${id}`);
  }
}
