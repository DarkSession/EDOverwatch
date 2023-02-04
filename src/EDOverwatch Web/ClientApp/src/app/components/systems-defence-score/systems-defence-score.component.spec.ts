import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemsDefenceScoreComponent } from './systems-defence-score.component';

describe('SystemsDefenceScoreComponent', () => {
  let component: SystemsDefenceScoreComponent;
  let fixture: ComponentFixture<SystemsDefenceScoreComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemsDefenceScoreComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemsDefenceScoreComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
