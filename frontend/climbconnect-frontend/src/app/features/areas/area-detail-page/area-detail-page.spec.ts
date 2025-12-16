import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AreaDetailPage } from './area-detail-page';

describe('AreaDetailPage', () => {
  let component: AreaDetailPage;
  let fixture: ComponentFixture<AreaDetailPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AreaDetailPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AreaDetailPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
