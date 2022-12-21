import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemOperationsComponent } from './system-operations.component';

describe('SystemOperationsComponent', () => {
  let component: SystemOperationsComponent;
  let fixture: ComponentFixture<SystemOperationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemOperationsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemOperationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
