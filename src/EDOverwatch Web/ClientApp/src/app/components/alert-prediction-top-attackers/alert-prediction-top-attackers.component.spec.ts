import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertPredictionTopAttackersComponent } from './alert-prediction-top-attackers.component';

describe('AlertPredictionTopAttackersComponent', () => {
  let component: AlertPredictionTopAttackersComponent;
  let fixture: ComponentFixture<AlertPredictionTopAttackersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AlertPredictionTopAttackersComponent]
    });
    fixture = TestBed.createComponent(AlertPredictionTopAttackersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
