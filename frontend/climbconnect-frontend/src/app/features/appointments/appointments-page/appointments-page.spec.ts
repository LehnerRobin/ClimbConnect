import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentsPage } from './appointments-page';

describe('AppointmentsPage', () => {
  let component: AppointmentsPage;
  let fixture: ComponentFixture<AppointmentsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentsPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppointmentsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
