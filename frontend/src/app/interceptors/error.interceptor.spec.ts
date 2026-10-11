import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { NotificationService } from '../../services/notification.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let notifications: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting()
      ]
    });
    http          = TestBed.inject(HttpClient);
    httpMock      = TestBed.inject(HttpTestingController);
    notifications = TestBed.inject(NotificationService);
  });

  afterEach(() => httpMock.verify());

  it('zeigt bei einem Serverfehler (500) eine Meldung an', () => {
    http.get('/api/areas').subscribe({ error: () => {} });
    httpMock.expectOne('/api/areas').flush('kaputt', { status: 500, statusText: 'Server Error' });

    expect(notifications.message()).toContain('Server');
  });

  it('zeigt ohne Verbindung (Status 0) eine Meldung an', () => {
    http.get('/api/areas').subscribe({ error: () => {} });
    httpMock.expectOne('/api/areas').error(new ProgressEvent('error'));

    expect(notifications.message()).toContain('Verbindung');
  });

  it('überlässt Validierungsfehler (400) der Komponente', () => {
    let status = 0;
    http.post('/api/progress', {}).subscribe({ error: e => status = e.status });
    httpMock.expectOne('/api/progress').flush({ error: 'ungültig' }, { status: 400, statusText: 'Bad Request' });

    expect(status).toBe(400);
    expect(notifications.message()).toBeNull();
  });
});
