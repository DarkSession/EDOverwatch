import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemStationsComponent } from './system-stations.component';

describe('SystemStationsComponent', () => {
  let component: SystemStationsComponent;
  let fixture: ComponentFixture<SystemStationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemStationsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemStationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
