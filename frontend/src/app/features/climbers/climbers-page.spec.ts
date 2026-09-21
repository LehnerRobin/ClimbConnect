import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClimbersPage } from './climbers-page';

describe('ClimbersPage', () => {
  let component: ClimbersPage;
  let fixture: ComponentFixture<ClimbersPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClimbersPage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClimbersPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
