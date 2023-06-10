import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommanderFcCargoComponent } from './commander-fc-cargo.component';

describe('CommanderFcCargoComponent', () => {
  let component: CommanderFcCargoComponent;
  let fixture: ComponentFixture<CommanderFcCargoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CommanderFcCargoComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CommanderFcCargoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
