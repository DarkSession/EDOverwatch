import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsumerApiComponent } from './consumer-api.component';

describe('ConsumerApiComponent', () => {
  let component: ConsumerApiComponent;
  let fixture: ComponentFixture<ConsumerApiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ConsumerApiComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConsumerApiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
