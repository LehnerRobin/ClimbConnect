import { dayKey, monthGrid, viennaCurrentMonth, viennaDayKey, viennaTime } from './vienna-date';

describe('vienna-date', () => {

  describe('viennaDayKey', () => {
    it('ordnet einen Termin kurz nach Mitternacht in Wien dem nächsten Tag zu (Sommerzeit, UTC+2)', () => {
      // 22:30 UTC = 00:30 in Wien am 15. Juli
      expect(viennaDayKey('2026-07-14T22:30:00Z')).toBe('2026-07-15');
    });

    it('ordnet einen Termin kurz nach Mitternacht in Wien dem nächsten Tag zu (Winterzeit, UTC+1)', () => {
      // 23:30 UTC = 00:30 in Wien am 15. Jänner
      expect(viennaDayKey('2026-01-14T23:30:00Z')).toBe('2026-01-15');
      // 22:30 UTC = 23:30 in Wien – noch derselbe Tag
      expect(viennaDayKey('2026-01-14T22:30:00Z')).toBe('2026-01-14');
    });

    it('rechnet rund um die Umstellung auf Sommerzeit (29. März 2026) richtig', () => {
      expect(viennaDayKey('2026-03-28T23:30:00Z')).toBe('2026-03-29'); // 00:30 MEZ
      expect(viennaDayKey('2026-03-29T21:30:00Z')).toBe('2026-03-29'); // 23:30 MESZ
      expect(viennaDayKey('2026-03-29T22:30:00Z')).toBe('2026-03-30'); // 00:30 MESZ
    });

    it('rechnet rund um die Umstellung auf Winterzeit (25. Oktober 2026) richtig', () => {
      expect(viennaDayKey('2026-10-24T22:30:00Z')).toBe('2026-10-25'); // 00:30 MESZ
      expect(viennaDayKey('2026-10-25T22:30:00Z')).toBe('2026-10-25'); // 23:30 MEZ
      expect(viennaDayKey('2026-10-25T23:30:00Z')).toBe('2026-10-26'); // 00:30 MEZ
    });

    it('nimmt auch ein Date-Objekt', () => {
      expect(viennaDayKey(new Date('2026-07-14T22:30:00Z'))).toBe('2026-07-15');
    });
  });

  describe('viennaTime', () => {
    it('zeigt die Uhrzeit in Wiener Ortszeit', () => {
      expect(viennaTime('2026-07-14T16:00:00Z')).toBe('18:00'); // Sommer: +2
      expect(viennaTime('2026-12-01T16:00:00Z')).toBe('17:00'); // Winter: +1
    });
  });

  describe('dayKey', () => {
    it('baut YYYY-MM-DD mit führenden Nullen, Monat 0–11', () => {
      expect(dayKey(2026, 0, 5)).toBe('2026-01-05');
      expect(dayKey(2026, 11, 31)).toBe('2026-12-31');
    });
  });

  describe('monthGrid', () => {
    it('zeigt Dezember 2026 von Montag 30.11. bis Sonntag 3.1.2027', () => {
      const grid = monthGrid(2026, 11);
      expect(grid.length).toBe(35);
      expect(grid[0]).toEqual({ key: '2026-11-30', day: 30, inMonth: false });
      expect(grid[1]).toEqual({ key: '2026-12-01', day: 1, inMonth: true });
      expect(grid[grid.length - 1]).toEqual({ key: '2027-01-03', day: 3, inMonth: false });
      expect(grid.filter(d => d.inMonth).length).toBe(31);
    });

    it('zeigt Februar 2026 (beginnt an einem Sonntag) von Montag 26.1. bis Sonntag 1.3.', () => {
      const grid = monthGrid(2026, 1);
      expect(grid[0].key).toBe('2026-01-26');
      expect(grid[6]).toEqual({ key: '2026-02-01', day: 1, inMonth: true });
      expect(grid[grid.length - 1].key).toBe('2026-03-01');
      expect(grid.filter(d => d.inMonth).length).toBe(28);
    });

    it('besteht in jedem Monat aus vollen Wochen von Montag bis Sonntag, ohne Lücken', () => {
      for (let month = 0; month < 12; month++) {
        const grid = monthGrid(2026, month);
        expect(grid.length % 7).withContext(`Monat ${month}`).toBe(0);

        const weekday = (key: string) => new Date(`${key}T00:00:00Z`).getUTCDay();
        expect(weekday(grid[0].key)).withContext(`Monat ${month}`).toBe(1);               // Montag
        expect(weekday(grid[grid.length - 1].key)).withContext(`Monat ${month}`).toBe(0); // Sonntag

        // Jeder Tag folgt genau einen Tag auf den vorherigen
        for (let i = 1; i < grid.length; i++) {
          const diff = Date.parse(`${grid[i].key}T00:00:00Z`) - Date.parse(`${grid[i - 1].key}T00:00:00Z`);
          expect(diff).toBe(24 * 60 * 60 * 1000);
        }
      }
    });
  });

  describe('viennaCurrentMonth', () => {
    afterEach(() => jasmine.clock().uninstall());

    it('nimmt den Monat nach Wiener Zeit – Silvester 23:30 UTC ist in Wien schon Jänner', () => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date('2026-12-31T23:30:00Z'));
      expect(viennaCurrentMonth()).toEqual({ year: 2027, month: 0 });
    });
  });
});
