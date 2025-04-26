// src/app/components/chat/chat-messages/chat-messages.component.ts
import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Message, Conversation, ChatService } from '../../../services/chat.service';
import { ReverseArrayPipe } from '../../../pipes/reverse.pipe';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-chat-messages',
  standalone: true,
  imports: [CommonModule, ReverseArrayPipe],
  templateUrl: './chat-messages.component.html',
  styleUrls: ['./chat-messages.component.css']
})
export class ChatMessagesComponent implements OnChanges, AfterViewChecked {
  @Input() messages: Message[] = [];
  @Input() conversation: Conversation | null = null;
  @Input() currentUserId: number = 0;
  isTyping = false;
  typingUserName = '';
  private destroy$ = new Subject<void>();
  @ViewChild('messageContainer') private messageContainer!: ElementRef;

  constructor(private chatService: ChatService) {
    this.chatService.typingUsers$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(typingUsers => {
      if (this.conversation) {
        const typingUserId = typingUsers[this.conversation.id];
        this.isTyping = !!typingUserId;
        console.log('Typing user ID:', typingUserId);
        if (typingUserId) {
          this.typingUserName = typingUserId === this.conversation.user1Id
            ? this.conversation.user1.firstName
            : this.conversation.user2.firstName;
        }
      } else {
        this.isTyping = false;
      }
    });
  }
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['messages'] || changes['conversation']) {
      // Scroll to bottom on next tick
      setTimeout(() => this.scrollToBottom(), 0);
    }
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  scrollToBottom(): void {
    try {
      if (this.messageContainer) {
        this.messageContainer.nativeElement.scrollTop =
          this.messageContainer.nativeElement.scrollHeight;
      }
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }

  // In src/app/components/chat/chat-messages/chat-messages.component.ts
  shouldShowDateDivider(currentMessage: Message, previousMessage?: Message): boolean {
    if (!previousMessage) return true;

    const currentDate = new Date(currentMessage.sentAt).toDateString();
    const previousDate = new Date(previousMessage.sentAt).toDateString();

    return currentDate !== previousDate;
  }
  getMessageDate(message: Message): string {
    const date = new Date(message.sentAt);
    const now = new Date();

    // If today, return time
    if (date.toDateString() === now.toDateString()) {
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    // If this week, return day name and time
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 3600 * 24));
    if (diffDays < 7) {
      return `${date.toLocaleDateString([], { weekday: 'short' })} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    }

    // Otherwise return date and time
    return `${date.toLocaleDateString()} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  }

  getMessageStatus(message: Message): string {
    if (message.senderId !== this.currentUserId) {
      return '';
    }

    return message.readAt ? 'Read' : 'Sent';
  }

  shouldShowDate(index: number): boolean {
    if (index === 0) {
      return true;
    }

    const currentDate = new Date(this.messages[index].sentAt).toDateString();
    const previousDate = new Date(this.messages[index - 1].sentAt).toDateString();

    return currentDate !== previousDate;
  }

  getDateDivider(date: Date): string {
    const messageDate = new Date(date);
    const today = new Date();
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);

    if (messageDate.toDateString() === today.toDateString()) {
      return 'Today';
    }

    if (messageDate.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    }

    return messageDate.toLocaleDateString([], { weekday: 'long', month: 'long', day: 'numeric' });
  }
}