import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaelstromsComponent } from './maelstroms.component';

describe('MaelstromsComponent', () => {
  let component: MaelstromsComponent;
  let fixture: ComponentFixture<MaelstromsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MaelstromsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MaelstromsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
