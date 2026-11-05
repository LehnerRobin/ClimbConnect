import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ModerationCaseDetail, ModerationCaseSummary } from '../models/pinnwand.model';

/** Admin: gemeldete Pinnwand-Fälle ansehen und erledigen. */
@Injectable({ providedIn: 'root' })
export class ModerationService {

  private http = inject(HttpClient);
  private api  = `${environment.apiUrl}/api/moderation/cases`;

  getCases(): Observable<ModerationCaseSummary[]> {
    return this.http.get<ModerationCaseSummary[]>(this.api);
  }

  getCase(id: number): Observable<ModerationCaseDetail> {
    return this.http.get<ModerationCaseDetail>(`${this.api}/${id}`);
  }

  /** Fall erledigen = Fall samt kopierter Beiträge und Meldungen löschen. */
  resolveCase(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }
}
