import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { AdminAppointmentsComponent } from './admin-appointments.component';
import { AdminAppointment, AdminService } from '../../../services/admin.service';
import { AreasService } from '../../../services/areas.service';

describe('AdminAppointmentsComponent (#223)', () => {
  const appointments: AdminAppointment[] = [
    { id: 1, title: 'Morgen-Session', date: '2030-08-01T16:00:00Z', areaId: 2, areaName: 'Sandwand',
      createdByUserId: 10, createdByUsername: 'zoe',  participantCount: 2 },
    { id: 2, title: 'Abend-Session',  date: '2030-08-02T16:00:00Z', areaId: 1, areaName: 'Adlitzgräben',
      createdByUserId: 11, createdByUsername: 'anna', participantCount: 3 },
    { id: 3, title: 'Wochenende',     date: '2030-08-03T08:00:00Z', areaId: 2, areaName: 'Sandwand',
      createdByUserId: 11, createdByUsername: 'anna', participantCount: 1 }
  ];

  let loadCalls: boolean[];
  let deleteResult: () => Observable<void>;
  let deletedIds: number[];

  function create(): AdminAppointmentsComponent {
    loadCalls = [];
    deletedIds = [];
    deleteResult = () => of(undefined);

    TestBed.configureTestingModule({
      imports: [AdminAppointmentsComponent],
      providers: [
        provideRouter([]),
        { provide: AdminService, useValue: {
          getAdminAppointments: (all = false) => { loadCalls.push(all); return of([...appointments]); }
        } },
        { provide: AreasService, useValue: {
          deleteAppointment: (id: number) => { deletedIds.push(id); return deleteResult(); }
        } }
      ]
    });
    const fixture = TestBed.createComponent(AdminAppointmentsComponent);
    fixture.detectChanges(); // ngOnInit → laden
    return fixture.componentInstance;
  }

  it('lädt beim Start nur kommende Termine', () => {
    const page = create();
    expect(loadCalls).toEqual([false]);
    expect(page.appointments.length).toBe(3);
    expect(page.loading).toBeFalse();
  });

  it('lädt mit „Auch vergangene Termine" neu, diesmal mit all=true', () => {
    const page = create();
    page.showPast = true;
    page.load();
    expect(loadCalls).toEqual([false, true]);
  });

  it('bietet jedes Gebiet und jeden Ersteller nur einmal an, alphabetisch', () => {
    const page = create();
    expect(page.areaOptions).toEqual([{ id: 1, name: 'Adlitzgräben' }, { id: 2, name: 'Sandwand' }]);
    expect(page.creatorOptions).toEqual([{ id: 11, name: 'anna' }, { id: 10, name: 'zoe' }]);
  });

  it('filtert nach Gebiet, nach Ersteller und nach beidem', () => {
    const page = create();
    const ids = () => page.filtered.map(a => a.id);

    page.areaFilter = 2;
    expect(ids()).toEqual([1, 3]);

    page.areaFilter = null;
    page.creatorFilter = 11;
    expect(ids()).toEqual([2, 3]);

    page.areaFilter = 2;
    expect(ids()).toEqual([3]);
    expect(page.hasFilter).toBeTrue();

    page.resetFilters();
    expect(ids()).toEqual([1, 2, 3]);
    expect(page.hasFilter).toBeFalse();
  });

  it('löscht nach Bestätigung und nimmt den Termin aus der Liste', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(true);

    page.remove(page.appointments[1]);

    expect(deletedIds).toEqual([2]);
    expect(page.appointments.map(a => a.id)).toEqual([1, 3]);
    expect(page.busyId).toBeNull();
  });

  it('löscht nichts, wenn der Admin abbricht', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(false);

    page.remove(page.appointments[0]);

    expect(deletedIds).toEqual([]);
    expect(page.appointments.length).toBe(3);
  });

  it('zeigt eine Fehlermeldung, wenn das Löschen fehlschlägt, und behält den Termin', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(true);
    deleteResult = () => throwError(() => new Error('500'));

    page.remove(page.appointments[0]);

    expect(page.error).toBe('Termin konnte nicht gelöscht werden.');
    expect(page.appointments.length).toBe(3);
  });

  it('zeigt Datum und Uhrzeit nach Wiener Ortszeit', () => {
    const page = create();
    expect(page.formatDate('2030-08-01T16:00:00Z')).toBe('01.08.2030, 18:00'); // Sommerzeit: +2
    expect(page.formatDate(null)).toBe('–');
  });
});
