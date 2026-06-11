import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private apiUrl = 'http://localhost:8080/api/users';

  constructor(private http: HttpClient) {}

 getProfile() {
  return this.http.get('/api/users/me');
}

updateProfile(profile: any) {
  return this.http.put('/api/users/me/profile', profile);
}
  }
