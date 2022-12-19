import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemStarportStatusComponent } from './system-starport-status.component';

describe('SystemStarportStatusComponent', () => {
  let component: SystemStarportStatusComponent;
  let fixture: ComponentFixture<SystemStarportStatusComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemStarportStatusComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemStarportStatusComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
