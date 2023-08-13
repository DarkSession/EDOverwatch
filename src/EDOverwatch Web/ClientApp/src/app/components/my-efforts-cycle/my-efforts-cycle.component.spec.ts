import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyEffortsCycleComponent } from './my-efforts-cycle.component';

describe('MyEffortsCycleComponent', () => {
  let component: MyEffortsCycleComponent;
  let fixture: ComponentFixture<MyEffortsCycleComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MyEffortsCycleComponent]
    });
    fixture = TestBed.createComponent(MyEffortsCycleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
