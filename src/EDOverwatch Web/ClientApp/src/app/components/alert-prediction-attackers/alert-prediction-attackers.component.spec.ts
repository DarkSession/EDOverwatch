import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertPredictionAttackersComponent } from './alert-prediction-attackers.component';

describe('AlertPredictionAttackersComponent', () => {
  let component: AlertPredictionAttackersComponent;
  let fixture: ComponentFixture<AlertPredictionAttackersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AlertPredictionAttackersComponent]
    });
    fixture = TestBed.createComponent(AlertPredictionAttackersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
