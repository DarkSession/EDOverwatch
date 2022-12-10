import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyEffortsComponent } from './my-efforts.component';

describe('MyEffortsComponent', () => {
  let component: MyEffortsComponent;
  let fixture: ComponentFixture<MyEffortsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MyEffortsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MyEffortsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
