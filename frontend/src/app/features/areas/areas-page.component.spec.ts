import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AreasPageComponent } from './areas-page.component';
import { Area, AreasService } from '../../../services/areas.service';

describe('AreasPageComponent – nächste 7 Tage (#239)', () => {
  function create(): AreasPageComponent {
    TestBed.configureTestingModule({
      imports: [AreasPageComponent],
      providers: [provideRouter([]), { provide: AreasService, useValue: { getAreas: () => of([]) } }]
    });
    return TestBed.createComponent(AreasPageComponent).componentInstance;
  }

  const area = (nextWeekVisitors?: number): Area => ({ id: 1, name: 'Sandwand', nextWeekVisitors });

  it('beschriftet den Chip mit Einzahl, Mehrzahl und „noch niemand"', () => {
    const page = create();
    expect(page.nextWeekLabel(area(12))).toBe('Nächste 7 Tage: 12 Leute');
    expect(page.nextWeekLabel(area(1))).toBe('Nächste 7 Tage: 1 Person');
    expect(page.nextWeekLabel(area(0))).toBe('Nächste 7 Tage: noch niemand');
    expect(page.nextWeekLabel(area(undefined))).toBe('Nächste 7 Tage: noch niemand');
  });

  it('summiert die Leute der nächsten 7 Tage über alle Gebiete', () => {
    const page = create();
    page.areas = [area(3), area(undefined), { ...area(5), id: 2 }];
    expect(page.totalVisitorsNextWeek).toBe(8);
  });
});
