import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemsHistoricalCycleComponent } from './systems-historical-cycle.component';

describe('SystemsHistoricalCycleComponent', () => {
  let component: SystemsHistoricalCycleComponent;
  let fixture: ComponentFixture<SystemsHistoricalCycleComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [SystemsHistoricalCycleComponent]
    });
    fixture = TestBed.createComponent(SystemsHistoricalCycleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
