import { TestBed } from '@angular/core/testing';
import { MonthCalendarComponent } from './month-calendar.component';

describe('MonthCalendarComponent', () => {
  function create(): MonthCalendarComponent {
    TestBed.configureTestingModule({ imports: [MonthCalendarComponent] });
    const cal = TestBed.createComponent(MonthCalendarComponent).componentInstance;
    cal.year = 2026;
    cal.month = 11; // Dezember
    cal.ngOnChanges();
    return cal;
  }

  describe('level (Farbstufe)', () => {
    it('stuft nach Anzahl Leute ein: 0 = nichts, 1 = 1–3, 2 = 4–7, 3 = 8+', () => {
      const cal = create();
      cal.stats = {
        '2026-12-01': { people: 3, appointments: 1 },
        '2026-12-02': { people: 4, appointments: 1 },
        '2026-12-03': { people: 7, appointments: 2 },
        '2026-12-04': { people: 8, appointments: 2 },
        '2026-12-05': { people: 0, appointments: 0 }
      };
      expect(cal.level('2026-12-01')).toBe(1);
      expect(cal.level('2026-12-02')).toBe(2);
      expect(cal.level('2026-12-03')).toBe(2);
      expect(cal.level('2026-12-04')).toBe(3);
      expect(cal.level('2026-12-05')).toBe(0);
      expect(cal.level('2026-12-06')).toBe(0); // kein Eintrag
    });
  });

  describe('Anzeige', () => {
    it('zeigt Monat, Zahlen am Tag und meldet Klicks mit dem Tages-Schlüssel', () => {
      TestBed.configureTestingModule({ imports: [MonthCalendarComponent] });
      const fixture = TestBed.createComponent(MonthCalendarComponent);
      fixture.componentRef.setInput('year', 2026);
      fixture.componentRef.setInput('month', 11);
      fixture.componentRef.setInput('stats', { '2026-12-03': { people: 8, appointments: 2 } });
      fixture.detectChanges();

      const el: HTMLElement = fixture.nativeElement;
      expect(el.querySelector('.month-label')?.textContent).toContain('Dezember 2026');

      const day3 = el.querySelector<HTMLButtonElement>('[aria-label^="3. Dezember 2026"]')!;
      expect(day3.textContent).toContain('8 Leute');
      expect(day3.textContent).toContain('2 Termine');
      expect(day3.classList).toContain('level-3');

      let selected = '';
      fixture.componentInstance.daySelected.subscribe(key => selected = key);
      day3.click();
      expect(selected).toBe('2026-12-03');
    });
  });

  describe('ariaLabel', () => {
    it('liest Tage des Monats mit Datum und Zahlen vor', () => {
      const cal = create();
      cal.stats = { '2026-12-03': { people: 8, appointments: 2 } };
      expect(cal.ariaLabel({ key: '2026-12-03', day: 3, inMonth: true }))
        .toBe('3. Dezember 2026: 8 Leute, 2 Termine');
    });

    it('nennt bei Rand-Tagen den richtigen Monat (#296)', () => {
      const cal = create();
      expect(cal.ariaLabel({ key: '2026-11-30', day: 30, inMonth: false }))
        .toBe('30. November 2026: keine Termine');
    });

    it('verwendet Einzahl bei 1 Person und 1 Termin', () => {
      const cal = create();
      cal.stats = { '2026-12-05': { people: 1, appointments: 1 } };
      expect(cal.ariaLabel({ key: '2026-12-05', day: 5, inMonth: true }))
        .toBe('5. Dezember 2026: 1 Person, 1 Termin');
    });
  });
});
