import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpireSitesComponent } from './spire-sites.component';

describe('SpireSitesComponent', () => {
  let component: SpireSitesComponent;
  let fixture: ComponentFixture<SpireSitesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpireSitesComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SpireSitesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
