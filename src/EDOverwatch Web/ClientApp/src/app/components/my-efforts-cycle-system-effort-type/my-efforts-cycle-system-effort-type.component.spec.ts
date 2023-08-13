import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyEffortsCycleSystemEffortTypeComponent } from './my-efforts-cycle-system-effort-type.component';

describe('MyEffortsCycleSystemEffortTypeComponent', () => {
  let component: MyEffortsCycleSystemEffortTypeComponent;
  let fixture: ComponentFixture<MyEffortsCycleSystemEffortTypeComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MyEffortsCycleSystemEffortTypeComponent]
    });
    fixture = TestBed.createComponent(MyEffortsCycleSystemEffortTypeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
