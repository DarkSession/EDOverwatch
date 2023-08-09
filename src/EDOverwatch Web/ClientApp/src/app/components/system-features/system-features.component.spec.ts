import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemFeaturesComponent } from './system-features.component';

describe('SystemFeaturesComponent', () => {
  let component: SystemFeaturesComponent;
  let fixture: ComponentFixture<SystemFeaturesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [SystemFeaturesComponent]
    });
    fixture = TestBed.createComponent(SystemFeaturesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
