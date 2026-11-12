import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { AdminComponent } from './admin.component';
import { Area } from '../../services/areas.service';

/**
 * Tests für das Gebiets-Formular der Admin-Seite.
 * Die Methoden werden direkt aufgerufen (ohne Template), damit nur die Logik geprüft wird.
 */
describe('AdminComponent – Gebiete', () => {
  let component: AdminComponent;
  let httpMock: HttpTestingController;

  const sandwand: Area = { id: 3, name: 'Sandwand', location: 'Steyr', latitude: 47.8634, longitude: 13.8392 };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    component = TestBed.createComponent(AdminComponent).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('behält beim Bearbeiten die Koordinaten des Gebiets', () => {
    component.startEditArea(sandwand);
    component.saveArea();

    const req = httpMock.expectOne(r => r.method === 'PUT' && r.url.endsWith('/api/areas/3'));
    expect(req.request.body.latitude).toBe(47.8634);
    expect(req.request.body.longitude).toBe(13.8392);
    req.flush(sandwand);

    // Nach dem Speichern wird die Liste neu geladen – diese Anfrage hier nur abräumen
    httpMock.match(r => r.method === 'GET');
    expect(component.successMsg).toBe('Gebiet aktualisiert.');
  });

  it('schickt neue Koordinaten beim Anlegen mit', () => {
    component.startCreateArea();
    component.areaForm.name = 'Neues Gebiet';
    component.areaForm.latitude = 48.1;
    component.areaForm.longitude = 14.2;
    component.saveArea();

    const req = httpMock.expectOne(r => r.method === 'POST' && r.url.endsWith('/api/areas'));
    expect(req.request.body.latitude).toBe(48.1);
    expect(req.request.body.longitude).toBe(14.2);
  });

  it('verlangt beide Koordinaten oder keine', () => {
    component.startCreateArea();
    component.areaForm.name = 'Neues Gebiet';
    component.areaForm.latitude = 48.1;
    component.saveArea();

    expect(component.errorMsg).toContain('beide');
    httpMock.expectNone(r => r.method === 'POST');
  });

  it('lehnt Koordinaten außerhalb des gültigen Bereichs ab', () => {
    component.startCreateArea();
    component.areaForm.name = 'Neues Gebiet';
    component.areaForm.latitude = 95;
    component.areaForm.longitude = 14.2;
    component.saveArea();

    expect(component.errorMsg).toContain('Breitengrad');
    httpMock.expectNone(r => r.method === 'POST');
  });
});
