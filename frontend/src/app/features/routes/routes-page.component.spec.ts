import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { RoutesPageComponent } from './routes-page.component';
import { UserService } from '../../../services/user.service';
import { Grade, GradeService } from '../../../services/grade.service';

/** Ausschnitt der Grad-Tabelle, leicht → schwer. */
const GRADES: Grade[] = [
  { french: '6a', uiaa: 'VI+', american: '5.10a' },
  { french: '6a+', uiaa: 'VII-', american: '5.10b' },
  { french: '6b', uiaa: 'VII', american: '5.10c' },
  { french: '6c', uiaa: 'VII+', american: '5.11a' }
];

describe('RoutesPageComponent', () => {
  let fixture: ComponentFixture<RoutesPageComponent>;
  let component: RoutesPageComponent;
  let httpMock: HttpTestingController;

  /** Anfrage an die Routensuche abfangen (nicht die Stil-Liste). */
  const expectSearch = () =>
    httpMock.expectOne(req => req.url.includes('/api/routes?'));

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutesPageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // Bevorzugte Skala des Users: UIAA
        { provide: UserService, useValue: { getPreferredScale: () => of('uiaa') } },
        { provide: GradeService, useValue: { getGrades: () => of(GRADES) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RoutesPageComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    // Beim Start lädt die Seite die Stile für die Stil-Auswahl
    httpMock.expectOne(req => req.url.endsWith('/api/routes/styles')).flush(['Toprope', 'Vorstieg']);
  });

  afterEach(() => httpMock.verify());

  it('zeigt ohne Suchbegriff einen Hinweis und fragt nichts an', () => {
    expect(fixture.nativeElement.textContent).toContain('Welche Route suchst du?');
    httpMock.expectNone(req => req.url.includes('/api/routes?'));
  });

  it('bietet Grade in der Skala des Users und die Stile aus dem Backend an', () => {
    expect(component.gradeOptions.map(o => o.label)).toEqual(['VI+', 'VII-', 'VII', 'VII+']);
    expect(component.gradeOptions[0].value).toBe('6a'); // gesendet wird französisch
    expect(component.styles).toEqual(['Toprope', 'Vorstieg']);
  });

  it('sucht nur mit Filtern, auch ohne Namen (#240)', fakeAsync(() => {
    component.filter.style = 'Toprope';
    component.filter.minGrade = '6a';
    component.filter.maxGrade = '6b';
    component.onFilterChange();
    tick(400);

    const req = expectSearch();
    expect(req.request.url).toContain('style=Toprope');
    expect(req.request.url).toContain('minGrade=6a');
    expect(req.request.url).toContain('maxGrade=6b');
    expect(req.request.url).toContain('scale=uiaa');
    expect(req.request.url).not.toContain('search=');
    req.flush({ total: 1, page: 1, pageSize: 100, items: [{ id: 3, sectorId: 1, name: 'Linke Platte', grade: 'VI+', style: 'Toprope' }] });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Gefilterte');
    expect(fixture.nativeElement.textContent).toContain('Linke Platte');
  }));

  it('kombiniert Namen und Länge; ein einzelnes Zeichen zählt nicht als Name', fakeAsync(() => {
    component.filter.minLength = 18;
    component.onSearchInput('F');
    tick(400);

    let req = expectSearch();
    expect(req.request.url).toContain('minLength=18');
    expect(req.request.url).not.toContain('search=');
    req.flush({ total: 0, page: 1, pageSize: 100, items: [] });

    component.onSearchInput('Finger');
    tick(400);
    req = expectSearch();
    expect(req.request.url).toContain('search=Finger');
    expect(req.request.url).toContain('minLength=18');
    req.flush({ total: 0, page: 1, pageSize: 100, items: [] });
  }));

  it('sucht bei widersprüchlichen Filtern nicht und zeigt einen Hinweis', fakeAsync(() => {
    component.filter.minLength = 30;
    component.filter.maxLength = 10;
    component.onFilterChange();
    tick(400);
    fixture.detectChanges();

    httpMock.expectNone(req => req.url.includes('/api/routes?'));
    expect(fixture.nativeElement.textContent).toContain('Die Länge „von" ist größer als „bis".');
  }));

  it('sperrt bei „bis" alle Grade, die leichter als „von" sind', () => {
    component.filter.minGrade = '6a+';
    const disabled = component.gradeOptions.filter(o => component.isBelowMinGrade(o)).map(o => o.value);
    expect(disabled).toEqual(['6a']);
  });

  it('schreibt Suche und Filter in die URL, damit sie beim Zurücknavigieren bleiben', fakeAsync(() => {
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    component.filter.style = 'Vorstieg';
    component.onSearchInput('Kante');
    tick(400);
    expectSearch().flush({ total: 0, page: 1, pageSize: 100, items: [] });

    const extras = navigate.calls.mostRecent().args[1]!;
    expect(extras.queryParams).toEqual(jasmine.objectContaining({ q: 'Kante', style: 'Vorstieg', minGrade: null }));
    expect(extras.replaceUrl).toBeTrue();
  }));

  it('setzt mit „Suche zurücksetzen" Namen und Filter zurück', () => {
    component.filter = { search: 'Kante', minGrade: '6a', maxGrade: null, style: 'Vorstieg', minLength: 10, maxLength: null };
    component.resetAll();
    expect(component.hasFilter).toBeFalse();
    expect(component.filter.search).toBe('');
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

describe('RoutesPageComponent – Filter aus der URL (#240)', () => {
  it('übernimmt Suche und Filter aus der URL und sucht sofort damit', fakeAsync(() => {
    TestBed.configureTestingModule({
      imports: [RoutesPageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: UserService, useValue: { getPreferredScale: () => of('french') } },
        { provide: GradeService, useValue: { getGrades: () => of(GRADES) } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(
          { q: 'Platte', minGrade: '6a', style: 'Toprope', maxLength: '20' }) } } }
      ]
    });
    spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    const httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(RoutesPageComponent);
    fixture.detectChanges();
    httpMock.expectOne(req => req.url.endsWith('/api/routes/styles')).flush([]);

    const page = fixture.componentInstance;
    expect(page.filter).toEqual({ search: 'Platte', minGrade: '6a', maxGrade: null, style: 'Toprope', minLength: null, maxLength: 20 });

    tick(400);
    const req = httpMock.expectOne(r => r.url.includes('/api/routes?'));
    expect(req.request.url).toContain('search=Platte');
    expect(req.request.url).toContain('minGrade=6a');
    expect(req.request.url).toContain('style=Toprope');
    expect(req.request.url).toContain('maxLength=20');
    req.flush({ total: 0, page: 1, pageSize: 100, items: [] });
    httpMock.verify();
  }));
});
