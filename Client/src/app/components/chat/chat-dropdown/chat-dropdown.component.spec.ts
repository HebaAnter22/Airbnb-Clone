import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatDropdownComponent } from './chat-dropdown.component';

describe('ChatDropdownComponent', () => {
  let component: ChatDropdownComponent;
  let fixture: ComponentFixture<ChatDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatDropdownComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChatDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
