import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertPredictionComponent } from './alert-prediction.component';

describe('AlertPredictionComponent', () => {
  let component: AlertPredictionComponent;
  let fixture: ComponentFixture<AlertPredictionComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AlertPredictionComponent]
    });
    fixture = TestBed.createComponent(AlertPredictionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
