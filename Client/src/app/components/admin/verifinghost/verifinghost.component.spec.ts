import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerifinghostComponent } from './verifinghost.component';

describe('VerifinghostComponent', () => {
  let component: VerifinghostComponent;
  let fixture: ComponentFixture<VerifinghostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerifinghostComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VerifinghostComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
