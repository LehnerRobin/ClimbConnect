import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FeedbackDialogComponent } from './feedback-dialog.component';
import { FeedbackService } from '../../services/feedback.service';
import { environment } from '../../environments/environment';

/** Feedback-Dialog (#244): richtiger Bezug, Reset beim Öffnen, Bestätigung nach dem Senden. */
describe('FeedbackDialogComponent', () => {
  let http: HttpTestingController;
  let service: FeedbackService;
  let dialog: FeedbackDialogComponent;
  let fixture: ComponentFixture<FeedbackDialogComponent>;
  const url = `${environment.apiUrl}/api/feedback`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FeedbackDialogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    http    = TestBed.inject(HttpTestingController);
    service = TestBed.inject(FeedbackService);
    fixture = TestBed.createComponent(FeedbackDialogComponent);
    dialog = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('schickt den Bezug zum Gebiet mit', () => {
    service.show({ areaId: 5, label: 'Traunstein' });
    fixture.detectChanges();
    dialog.text = '  Neuer Zustieg  ';

    dialog.submit();

    const req = http.expectOne(url);
    expect(req.request.body).toEqual({ text: 'Neuer Zustieg', photoUrl: null, areaId: 5, routeId: null });
    req.flush({ id: 1 });
    expect(dialog.sent).toBeTrue();
  });

  it('lässt den Bezug weg, wenn der Haken entfernt wird', () => {
    service.show({ routeId: 7 });
    fixture.detectChanges();
    dialog.text = 'Allgemeiner Hinweis';
    dialog.withContext = false;

    dialog.submit();

    const req = http.expectOne(url);
    expect(req.request.body.areaId).toBeNull();
    expect(req.request.body.routeId).toBeNull();
    req.flush({ id: 2 });
  });

  it('sendet nichts ohne Text', () => {
    service.show();
    fixture.detectChanges();
    dialog.text = '   ';

    expect(dialog.canSubmit).toBeFalse();
    dialog.submit();
    http.expectNone(url);
  });

  it('setzt das Formular beim erneuten Öffnen zurück', () => {
    service.show();
    fixture.detectChanges();
    dialog.text = 'Alter Text';
    dialog.sent = true;

    service.close();
    fixture.detectChanges();
    service.show();
    fixture.detectChanges();

    expect(dialog.text).toBe('');
    expect(dialog.sent).toBeFalse();
  });

  it('beschreibt den Bezug verständlich', () => {
    service.show({ areaId: 1, label: 'Traunstein' });
    expect(dialog.contextLabel).toBe('das Gebiet „Traunstein"');
    service.show({ routeId: 2 });
    expect(dialog.contextLabel).toBe('diese Route');
  });
});
