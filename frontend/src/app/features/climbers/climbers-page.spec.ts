import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { ClimbersPage } from './climbers-page';

describe('ClimbersPage', () => {
  let component: ClimbersPage;
  let fixture: ComponentFixture<ClimbersPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClimbersPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
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
