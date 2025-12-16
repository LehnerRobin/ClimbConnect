import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AreasPage } from './areas-page';

describe('AreasPage', () => {
  let component: AreasPage;
  let fixture: ComponentFixture<AreasPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AreasPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AreasPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
