import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ThargoidLevelComponent } from './thargoid-level.component';

describe('ThargoidLevelComponent', () => {
  let component: ThargoidLevelComponent;
  let fixture: ComponentFixture<ThargoidLevelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ThargoidLevelComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ThargoidLevelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
