import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OperationSearchComponent } from './operation-search.component';

describe('OperationSearchComponent', () => {
  let component: OperationSearchComponent;
  let fixture: ComponentFixture<OperationSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ OperationSearchComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OperationSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
