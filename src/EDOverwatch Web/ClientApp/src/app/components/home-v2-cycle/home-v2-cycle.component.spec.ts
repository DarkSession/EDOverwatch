import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeV2CycleComponent } from './home-v2-cycle.component';

describe('HomeV2CycleComponent', () => {
  let component: HomeV2CycleComponent;
  let fixture: ComponentFixture<HomeV2CycleComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [HomeV2CycleComponent]
    });
    fixture = TestBed.createComponent(HomeV2CycleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
