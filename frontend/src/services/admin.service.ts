import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

const httpOptions = {
  headers: new HttpHeaders({
    'Content-Type': 'application/json'
  })
}
@Injectable({
  providedIn: 'root'
})
export class AdminService {

  private apiUrl = "/api";  // will be replaced by environment variable

  constructor(private http: HttpClient) { 
    this.apiUrl = environment.apiUrl;
    console.log("API URL: " + this.apiUrl);
  }

  initDatabase() {
    const url = `${this.apiUrl}/Admin`;
    return this.http.put(url, null);
  }

}
