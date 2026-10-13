import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, timeout, TimeoutError } from 'rxjs';
import { NotificationService } from '../../services/notification.service';

/** Nach so vielen Millisekunden ohne Antwort gilt eine Anfrage als gescheitert. */
const REQUEST_TIMEOUT_MS = 20_000;

/** Bild-Uploads dürfen bei langsamer Verbindung länger dauern. */
const UPLOAD_TIMEOUT_MS = 120_000;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router        = inject(Router);
  const notifications = inject(NotificationService);
  const limit         = req.url.endsWith('/api/upload') ? UPLOAD_TIMEOUT_MS : REQUEST_TIMEOUT_MS;

  return next(req).pipe(
    timeout(limit),
    catchError((error: HttpErrorResponse | TimeoutError) => {
      // Keine Antwort in der Zeit → wie ein Netzwerkfehler behandeln (Status 0)
      if (error instanceof TimeoutError) {
        notifications.showError('Der Server antwortet gerade nicht. Bitte versuche es gleich noch einmal.');
        return throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Timeout', url: req.url }));
      }

      if (error.status === 401) {
        localStorage.removeItem('token');
        router.navigate(['/login']);
      } else if (error.status === 403) {
        router.navigate(['/forbidden']);
      } else if (error.status === 0) {
        notifications.showError('Keine Verbindung zum Server. Bitte prüfe deine Internetverbindung.');
      } else if (error.status === 502 || error.status === 503 || error.status === 504) {
        // Kommt von nginx, wenn das Backend gerade nicht läuft (z. B. während eines Deployments)
        notifications.showError('Der Server ist gerade nicht erreichbar. Bitte versuche es gleich noch einmal.');
      } else if (error.status >= 500) {
        notifications.showError('Auf dem Server ist ein Fehler aufgetreten. Bitte versuche es später noch einmal.');
      }
      // 400/404/409 zeigen die Komponenten selbst passend an
      return throwError(() => error);
    })
  );
};
