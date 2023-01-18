import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapHistoricalComponent } from './map-historical.component';

describe('MapHistoricalComponent', () => {
  let component: MapHistoricalComponent;
  let fixture: ComponentFixture<MapHistoricalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MapHistoricalComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapHistoricalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
