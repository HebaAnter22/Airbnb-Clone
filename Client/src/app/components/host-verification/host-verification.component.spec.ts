import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HostVerificationComponent } from './host-verification.component';

describe('HostVerificationComponent', () => {
  let component: HostVerificationComponent;
  let fixture: ComponentFixture<HostVerificationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostVerificationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HostVerificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
