import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExperimentalSettingsComponent } from './experimental-settings.component';

describe('ExperimentalSettingsComponent', () => {
  let component: ExperimentalSettingsComponent;
  let fixture: ComponentFixture<ExperimentalSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ExperimentalSettingsComponent]
    });
    fixture = TestBed.createComponent(ExperimentalSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
