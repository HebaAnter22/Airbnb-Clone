// src/app/components/chat/chat-input/chat-input.component.ts
import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-chat-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-input.component.html',
  styleUrls: ['./chat-input.component.css']
})
export class ChatInputComponent {
  @Input() disabled: boolean = true;
  @Output() messageSent = new EventEmitter<string>();
  
  messageContent: string = '';
  
  constructor() {}
  
  sendMessage(): void {
    if (!this.messageContent.trim() || this.disabled) {
      return;
    }
    
    this.messageSent.emit(this.messageContent);
    this.messageContent = '';
  }
  
}