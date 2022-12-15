import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaelstromComponent } from './maelstrom.component';

describe('MaelstromComponent', () => {
  let component: MaelstromComponent;
  let fixture: ComponentFixture<MaelstromComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MaelstromComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MaelstromComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
