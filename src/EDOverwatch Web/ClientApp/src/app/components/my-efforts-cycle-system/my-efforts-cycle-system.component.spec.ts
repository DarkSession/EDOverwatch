import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyEffortsCycleSystemComponent } from './my-efforts-cycle-system.component';

describe('MyEffortsCycleSystemComponent', () => {
  let component: MyEffortsCycleSystemComponent;
  let fixture: ComponentFixture<MyEffortsCycleSystemComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MyEffortsCycleSystemComponent]
    });
    fixture = TestBed.createComponent(MyEffortsCycleSystemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
