import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { RoutesPageComponent } from './routes-page.component';
import { UserService } from '../../../services/user.service';

describe('RoutesPageComponent', () => {
  let fixture: ComponentFixture<RoutesPageComponent>;
  let component: RoutesPageComponent;
  let httpMock: HttpTestingController;

  /** Anfrage an die Routensuche abfangen. */
  const expectSearch = () =>
    httpMock.expectOne(req => req.url.includes('/api/routes') && req.url.includes('search='));

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutesPageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // Bevorzugte Skala des Users: UIAA
        { provide: UserService, useValue: { getPreferredScale: () => of('uiaa') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RoutesPageComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('zeigt ohne Suchbegriff einen Hinweis und fragt nichts an', () => {
    expect(fixture.nativeElement.textContent).toContain('Welche Route suchst du?');
    httpMock.expectNone(req => req.url.includes('/api/routes'));
  });

  it('sucht erst ab 2 Zeichen', fakeAsync(() => {
    component.onSearchInput('F');
    tick(400);
    httpMock.expectNone(req => req.url.includes('/api/routes'));
  }));

  it('zeigt Treffer mit Grad, Gebiet und Sektor und verlinkt zur Route', fakeAsync(() => {
    component.onSearchInput('Finger');
    tick(400);

    const req = expectSearch();
    expect(req.request.url).toContain('search=Finger');
    expect(req.request.url).toContain('scale=uiaa');
    req.flush({
      total: 1, page: 1, pageSize: 100,
      items: [{ id: 7, sectorId: 1, sectorName: 'Hauptwand', areaId: 1, areaName: 'Sandwand', name: 'Fingerkante', grade: 'VII+' }]
    });
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Fingerkante');
    expect(text).toContain('VII+');
    expect(text).toContain('Sandwand · Hauptwand');

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a.result-row');
    expect(link.getAttribute('href')).toBe('/routes/7');
  }));

  it('zeigt einen Hinweis, wenn nichts gefunden wurde', fakeAsync(() => {
    component.onSearchInput('Gibtsnicht');
    tick(400);

    expectSearch().flush({ total: 0, page: 1, pageSize: 100, items: [] });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Keine Routen gefunden');
  }));
});
