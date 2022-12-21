import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemContributionsComponent } from './system-contributions.component';

describe('SystemContributionsComponent', () => {
  let component: SystemContributionsComponent;
  let fixture: ComponentFixture<SystemContributionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SystemContributionsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SystemContributionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
