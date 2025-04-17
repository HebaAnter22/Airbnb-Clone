import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PropertyGallaryComponent } from './property-gallery.component';

describe('PropertyGallaryComponent', () => {
  let component: PropertyGallaryComponent;
  let fixture: ComponentFixture<PropertyGallaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertyGallaryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PropertyGallaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
