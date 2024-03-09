import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemProgressDetailsComponent } from './system-progress-details.component';

describe('SystemProgressDetailsComponent', () => {
  let component: SystemProgressDetailsComponent;
  let fixture: ComponentFixture<SystemProgressDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SystemProgressDetailsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SystemProgressDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
