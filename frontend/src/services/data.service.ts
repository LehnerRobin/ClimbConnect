import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MDemo, MDemoOverview } from '../models/mdemo.model';
import { environment } from '../environments/environment';

const httpOptions = {
  headers: new HttpHeaders({
    'Content-Type': 'application/json'
  })
}
@Injectable({
  providedIn: 'root'
})
export class DataService {

  private apiUrl = "/api";  // will be replaced by environment variable

  constructor(private http: HttpClient) { 
    this.apiUrl = environment.apiUrl;
    console.log("API URL: " + this.apiUrl);
  }

  getMDemos() {
    return this.http.get<MDemoOverview[]>(`${this.apiUrl}/demo`);
  }
  
  getMDemo(id: number) {
    return this.http.get<MDemo>(`${this.apiUrl}/demo/${id}`);
  }

  updateMDemo(mdemo: MDemo) {
    const url = `${this.apiUrl}/demo/${mdemo.id}`;
    return this.http.put(url, mdemo);
  }

  addMDemo(name: string, age: number, minPlayers: number|null, maxPlayers: number|null): Observable<MDemo> {

    const url = `${this.apiUrl}/demo`;
    let createMDemoDto: MDemo = {
      id: 0,
      name: name,
      fDemoId: 1
    };

    return this.http.post<MDemo>(url, createMDemoDto);
  }  

  deleteMDemo(id: number) {
    const url = `${this.apiUrl}/demo/${id}`;
    return this.http.delete(url, { responseType: 'text' });
  }
}
