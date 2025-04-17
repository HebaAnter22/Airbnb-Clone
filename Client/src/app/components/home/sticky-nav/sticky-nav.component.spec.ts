import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StickyNavComponent } from './sticky-nav.component';

describe('StickyNavComponent', () => {
  let component: StickyNavComponent;
  let fixture: ComponentFixture<StickyNavComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StickyNavComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StickyNavComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
