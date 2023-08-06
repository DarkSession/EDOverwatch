import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertPredictionOverviewComponent } from './alert-prediction-overview.component';

describe('AlertPredictionOverviewComponent', () => {
  let component: AlertPredictionOverviewComponent;
  let fixture: ComponentFixture<AlertPredictionOverviewComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AlertPredictionOverviewComponent]
    });
    fixture = TestBed.createComponent(AlertPredictionOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
