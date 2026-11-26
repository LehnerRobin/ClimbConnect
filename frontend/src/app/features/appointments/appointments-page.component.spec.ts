import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { AppointmentsPageComponent } from './appointments-page.component';
import { Appointment, AreasService } from '../../../services/areas.service';

describe('AppointmentsPageComponent', () => {
  /** Jede Anfrage bekommt ein eigenes Subject – so bestimmt der Test, wann welche Antwort ankommt. */
  let requests: Subject<Appointment[]>[];

  function create(): AppointmentsPageComponent {
    requests = [];
    const service = {
      getAreas: () => of([]),
      getAppointmentsInRange: () => {
        const s = new Subject<Appointment[]>();
        requests.push(s);
        return s.asObservable();
      }
    };
    TestBed.configureTestingModule({
      imports: [AppointmentsPageComponent],
      providers: [provideRouter([]), { provide: AreasService, useValue: service }]
    });
    return TestBed.createComponent(AppointmentsPageComponent).componentInstance;
  }

  /**
   * Dezember 2026 – das Raster reicht von Mo 30.11. bis So 3.1.2027.
   * Zeiten in UTC; in Wien ist es eine Stunde später (Winterzeit).
   */
  const december: Appointment[] = [
    { id: 1, areaId: 1, title: 'A', date: '2026-12-03T08:00:00Z', participantCount: 3 },
    { id: 2, areaId: 2, title: 'B', date: '2026-12-03T15:00:00Z', participantCount: 5 },
    { id: 3, areaId: 1, title: 'C', date: '2026-12-10T22:30:00Z', participantCount: 2 }, // 23:30 in Wien → 10.12.
    { id: 4, areaId: 1, title: 'D', date: '2026-11-30T10:00:00Z', participantCount: 4 }, // Rand-Tag Vormonat
    { id: 5, areaId: 1, title: 'E', date: '2026-12-31T23:30:00Z', participantCount: 1 }, // in Wien schon 1.1.2027
    { id: 6, areaId: 1, title: 'F', date: '2027-01-05T10:00:00Z', participantCount: 9 }  // außerhalb des Rasters
  ];

  function loadDecember(): AppointmentsPageComponent {
    const page = create();
    page.year = 2026;
    page.month = 11;
    page.loadMonth();
    requests[0].next(december);
    return page;
  }

  const titles = (page: AppointmentsPageComponent) =>
    page.groups.map(g => `${g.key}: ${g.appointments.map(a => a.title).join(',')}`);

  it('zählt pro Wiener Tag die Leute und Termine – auch Rand-Tage, aber nichts außerhalb des Rasters', () => {
    const page = loadDecember();
    expect(page.dayStats).toEqual({
      '2026-12-03': { people: 8, appointments: 2 },
      '2026-12-10': { people: 2, appointments: 1 },
      '2026-11-30': { people: 4, appointments: 1 },
      '2027-01-01': { people: 1, appointments: 1 }
    });
  });

  it('listet ohne ausgewählten Tag nur die Tage des angezeigten Monats, nach Zeit sortiert', () => {
    const page = loadDecember();
    expect(titles(page)).toEqual(['2026-12-03: A,B', '2026-12-10: C']);
    expect(page.loading).toBeFalse();
  });

  it('filtert Kalender und Liste nach Gebiet', () => {
    const page = loadDecember();
    page.areaFilter = 2;
    expect(page.dayStats).toEqual({ '2026-12-03': { people: 5, appointments: 1 } });
    expect(titles(page)).toEqual(['2026-12-03: B']);
  });

  it('zeigt nach Klick auf einen Tag nur diesen Tag, ein zweiter Klick hebt das auf', () => {
    const page = loadDecember();
    page.selectDay('2026-11-30');
    expect(titles(page)).toEqual(['2026-11-30: D']);

    page.selectDay('2026-11-30');
    expect(page.selectedKey).toBeNull();
    expect(titles(page)).toEqual(['2026-12-03: A,B', '2026-12-10: C']);
  });

  it('setzt beim Blättern die Tagesauswahl zurück', () => {
    const page = loadDecember();
    page.selectDay('2026-12-03');
    page.nextMonth();
    expect(page.selectedKey).toBeNull();
    expect(page.year).toBe(2027);
    expect(page.month).toBe(0);
  });

  it('zeigt eine Fehlermeldung, wenn das Laden fehlschlägt', () => {
    const page = create();
    page.loadMonth();
    requests[0].error(new Error('offline'));
    expect(page.errorMessage).toBe('Termine konnten nicht geladen werden.');
    expect(page.loading).toBeFalse();
  });

  it('zeigt beim schnellen Blättern den zuletzt gewählten Monat, auch wenn eine alte Antwort später kommt (#295)', () => {
    const page = create();
    page.year = 2026;
    page.month = 11; // Dezember
    page.loadMonth();   // Anfrage 0: Dezember
    page.nextMonth();   // Anfrage 1: Jänner 2027

    // Jänner antwortet zuerst …
    requests[1].next([{ id: 2, title: 'Jänner', date: '2027-01-20T10:00:00Z' }]);
    // … die alte Dezember-Antwort kommt zu spät.
    requests[0].next([{ id: 1, title: 'Dezember', date: '2026-12-15T10:00:00Z' }]);

    expect(page.groups.flatMap(g => g.appointments.map(a => a.title))).toEqual(['Jänner']);
    expect(page.loading).toBeFalse();
  });
});
