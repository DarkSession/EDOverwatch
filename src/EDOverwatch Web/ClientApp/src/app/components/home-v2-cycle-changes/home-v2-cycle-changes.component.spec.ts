import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeV2CycleChangesComponent } from './home-v2-cycle-changes.component';

describe('HomeV2CycleChangesComponent', () => {
  let component: HomeV2CycleChangesComponent;
  let fixture: ComponentFixture<HomeV2CycleChangesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [HomeV2CycleChangesComponent]
    });
    fixture = TestBed.createComponent(HomeV2CycleChangesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
