// src/app/components/chat/chat-input/chat-input.component.ts
import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';
import { ChatService } from '../../../services/chat.service';

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
  @Input() conversationId: number | null = null;
  messageContent: string = '';
  private typingTimeout: any;
  private destroy$ = new Subject<void>();
  private typingSubject = new Subject<boolean>();
  constructor(
    private chatService: ChatService
  ) {
    this.typingSubject.pipe(
      distinctUntilChanged(),
      debounceTime(1000),
      takeUntil(this.destroy$)
    ).subscribe(isTyping => {
      if (this.conversationId) {
        this.chatService.notifyTyping(this.conversationId, isTyping);
      }
    });
  }

  onInputChange(): void {
    if (this.messageContent.trim().length > 0) {
      this.typingSubject.next(true);

      // Clear previous timeout
      if (this.typingTimeout) clearTimeout(this.typingTimeout);

      // Set new timeout (3 seconds of inactivity)
      this.typingTimeout = setTimeout(() => {
        this.typingSubject.next(false);
      }, 3000);
    } else {
      this.typingSubject.next(false);
    }
  }
  sendMessage(): void {
    if (!this.messageContent.trim() || this.disabled) {
      return;
    }
    this.typingSubject.next(false);

    this.messageSent.emit(this.messageContent);
    this.messageContent = '';

    this.messageSent.emit(this.messageContent);
    this.messageContent = '';
  }
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    // Ensure we send a final "not typing" notification
    if (this.conversationId) {
      this.chatService.notifyTyping(this.conversationId, false);
    }
  }

}