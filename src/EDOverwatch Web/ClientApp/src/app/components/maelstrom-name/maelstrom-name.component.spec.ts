import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaelstromNameComponent } from './maelstrom-name.component';

describe('MaelstromNameComponent', () => {
  let component: MaelstromNameComponent;
  let fixture: ComponentFixture<MaelstromNameComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MaelstromNameComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MaelstromNameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
