import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type Area = {
  id: number;
  title: string;
};

export type AreaDetail = {
  id: number;
  title: string;
  description?: string;
};

@Injectable({
  providedIn: 'root',
})
export class AreasApiService {
  private baseUrl = '/api/areas';

  constructor(private http: HttpClient) {}

  getAreas(): Observable<Area[]> {
    return this.http.get<Area[]>(this.baseUrl);
  }

  getArea(id: number): Observable<AreaDetail> {
    return this.http.get<AreaDetail>(`${this.baseUrl}/${id}`);
  }
}
