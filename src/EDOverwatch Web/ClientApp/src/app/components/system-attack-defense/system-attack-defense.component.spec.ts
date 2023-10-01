import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemAttackDefenseComponent } from './system-attack-defense.component';

describe('SystemAttackDefenseComponent', () => {
  let component: SystemAttackDefenseComponent;
  let fixture: ComponentFixture<SystemAttackDefenseComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [SystemAttackDefenseComponent]
    });
    fixture = TestBed.createComponent(SystemAttackDefenseComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
