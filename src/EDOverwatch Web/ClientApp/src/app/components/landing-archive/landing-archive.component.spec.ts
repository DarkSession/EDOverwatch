import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LandingArchiveComponent } from './landing-archive.component';

describe('LandingArchiveComponent', () => {
  let component: LandingArchiveComponent;
  let fixture: ComponentFixture<LandingArchiveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingArchiveComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LandingArchiveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
