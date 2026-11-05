import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { PinnwandPost, ReportCreateRequest } from '../models/pinnwand.model';
import { SKIP_FORBIDDEN_REDIRECT } from '../app/interceptors/error.interceptor';

/** Pinnwand eines Termins: Beiträge lesen, schreiben, löschen und melden. */
@Injectable({ providedIn: 'root' })
export class PinnwandService {

  private http = inject(HttpClient);
  private api  = `${environment.apiUrl}/api`;

  /** 403 soll hier nicht auf /forbidden umleiten – die Komponente zeigt die Meldung selbst an. */
  private readonly context = new HttpContext().set(SKIP_FORBIDDEN_REDIRECT, true);

  getPosts(appointmentId: number): Observable<PinnwandPost[]> {
    return this.http.get<PinnwandPost[]>(`${this.api}/appointments/${appointmentId}/posts`, { context: this.context });
  }

  createPost(appointmentId: number, text: string): Observable<PinnwandPost> {
    return this.http.post<PinnwandPost>(`${this.api}/appointments/${appointmentId}/posts`, { text }, { context: this.context });
  }

  deletePost(postId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/posts/${postId}`, { context: this.context });
  }

  /** Meldet einen Beitrag. Beim ersten Bericht zu einem Termin legt das Backend einen Fall an. */
  reportPost(postId: number, request: ReportCreateRequest): Observable<{ caseId: number }> {
    return this.http.post<{ caseId: number }>(`${this.api}/posts/${postId}/reports`, request, { context: this.context });
  }
}
