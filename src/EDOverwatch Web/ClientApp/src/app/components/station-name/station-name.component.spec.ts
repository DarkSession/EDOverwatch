import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StationNameComponent } from './station-name.component';

describe('StationNameComponent', () => {
  let component: StationNameComponent;
  let fixture: ComponentFixture<StationNameComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ StationNameComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StationNameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
