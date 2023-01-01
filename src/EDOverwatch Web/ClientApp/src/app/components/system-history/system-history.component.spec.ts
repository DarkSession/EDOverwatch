import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemHistoryComponent } from './system-history.component';

describe('SystemHistoryComponent', () => {
  let component: SystemHistoryComponent;
  let fixture: ComponentFixture<SystemHistoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemHistoryComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
