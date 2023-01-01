import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemStateAnalysisComponent } from './system-state-analysis.component';

describe('SystemStateAnalysisComponent', () => {
  let component: SystemStateAnalysisComponent;
  let fixture: ComponentFixture<SystemStateAnalysisComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemStateAnalysisComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemStateAnalysisComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
