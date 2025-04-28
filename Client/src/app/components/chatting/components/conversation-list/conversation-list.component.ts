import { Component, OnInit, OnDestroy } from '@angular/core';

import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { Conversation, Message } from '../../../../models/messaging.model';
import { ChatSignalRService } from '../../../../services/chatSignal.service';
import { AuthService } from '../../../../services/auth.service';
import { DatePipe, NgForOf, NgIf } from '@angular/common';

@Component({
  selector: 'app-conversation-list',
  templateUrl: './conversation-list.component.html',
  imports: [NgIf, NgForOf, DatePipe],
  styleUrls: ['./conversation-list.component.scss']
})
export class ConversationListComponent implements OnInit, OnDestroy {
  conversations: Conversation[] = [];
  loading = true;
  currentUserId: number;
  private subscriptions: Subscription[] = [];

  constructor(
    private chatService: ChatSignalRService,
    private authService: AuthService,
    private router: Router
  ) {
    this.currentUserId = authService.getCurrentUserId();
  }

  ngOnInit(): void {
    this.loadConversations();

    // Listen for new messages to update conversation list
    const messageSub = this.chatService.messageReceived$.subscribe(message => {
      this.updateConversationWithMessage(message);
    });

    // Listen for read messages
    const readSub = this.chatService.messageRead$.subscribe(messageId => {
      this.updateMessageReadStatus(messageId);
    });

    this.subscriptions.push(messageSub, readSub);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  loadConversations(): void {
    this.loading = true;
    this.chatService.getUserConversations().subscribe({
      next: (data) => {
        this.conversations = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading conversations', error);
        this.loading = false;
      }
    });
  }

  openConversation(conversation: Conversation): void {
    this.router.navigate(['/chat', conversation.id]);
  }

  getOtherUser(conversation: Conversation): any {
    return conversation.user1Id === this.currentUserId ? conversation.user2 : conversation.user1;
  }

  getLastMessage(conversation: Conversation): string {
    if (!conversation.messages || conversation.messages.length === 0) {
      return 'No messages yet';
    }
    return conversation.messages[0].content;
  }

  hasUnreadMessages(conversation: Conversation): boolean {
    if (!conversation.messages || conversation.messages.length === 0) {
      return false;
    }

    return conversation.messages.some(m =>
      m.senderId !== this.currentUserId && !m.readAt
    );
  }

  private updateConversationWithMessage(message: Message): void {
    // Find the conversation this message belongs to
    const conversationIndex = this.conversations.findIndex(c => c.id === message.conversationId);

    if (conversationIndex > -1) {
      // Add message to beginning of messages array
      const conversation = { ...this.conversations[conversationIndex] };
      if (!conversation.messages) {
        conversation.messages = [];
      }
      conversation.messages.unshift(message);

      // Remove conversation from current position
      this.conversations.splice(conversationIndex, 1);
      // Add it to the beginning of the array
      this.conversations.unshift(conversation);
    }
  }

  private updateMessageReadStatus(messageId: number): void {
    // Update read status for message across all conversations
    this.conversations.forEach(conversation => {
      if (conversation.messages) {
        const message = conversation.messages.find((m) => m.id === messageId);
        if (message) {
          message.readAt = new Date();
        }
      }
    });
  }
}