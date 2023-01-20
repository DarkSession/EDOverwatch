import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemContributionSummaryComponent } from './system-contribution-summary.component';

describe('SystemContributionSummaryComponent', () => {
  let component: SystemContributionSummaryComponent;
  let fixture: ComponentFixture<SystemContributionSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemContributionSummaryComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemContributionSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
