import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ContributeDataComponent } from './contribute-data.component';

describe('ContributeDataComponent', () => {
  let component: ContributeDataComponent;
  let fixture: ComponentFixture<ContributeDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ContributeDataComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ContributeDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
