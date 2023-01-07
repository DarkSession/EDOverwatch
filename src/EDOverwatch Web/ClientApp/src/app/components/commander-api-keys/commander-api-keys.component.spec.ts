import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommanderApiKeysComponent } from './commander-api-keys.component';

describe('CommanderApiKeysComponent', () => {
  let component: CommanderApiKeysComponent;
  let fixture: ComponentFixture<CommanderApiKeysComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CommanderApiKeysComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CommanderApiKeysComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
