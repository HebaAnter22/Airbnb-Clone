import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MessageUserButtonComponent } from './message-user-button.component';

describe('MessageUserButtonComponent', () => {
  let component: MessageUserButtonComponent;
  let fixture: ComponentFixture<MessageUserButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessageUserButtonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MessageUserButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
