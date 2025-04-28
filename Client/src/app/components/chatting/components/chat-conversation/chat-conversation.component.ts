import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChatSignalRService } from '../../../../services/chatSignal.service';
import { AuthService } from '../../../../services/auth.service';
import { Message, Conversation } from '../../../../models/messaging.model';
import { Subscription } from 'rxjs';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgForOf, NgIf } from '@angular/common';

@Component({
  selector: 'app-chat-conversation',
  imports: [ReactiveFormsModule, NgIf, NgForOf],

  templateUrl: './chat-conversation.component.html',
  styleUrls: ['./chat-conversation.component.scss']
})
export class ChatConversationComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  @ViewChild('messageInput') messageInput!: ElementRef;

  conversationId!: number;
  messages: Message[] = [];
  conversation: Conversation | null = null;
  loading = true;
  sending = false;
  currentUserId: number;
  otherUser: any = null;
  messageControl = new FormControl('', [Validators.required]);
  private subscriptions: Subscription[] = [];
  private shouldScrollToBottom = true;

  constructor(
    private route: ActivatedRoute,
    private chatService: ChatSignalRService,
    private authService: AuthService
  ) {
    this.currentUserId = authService.getCurrentUserId();
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.conversationId = +params['id'];
      this.loadConversation();
    });

    // Start SignalR connection
    this.chatService.startConnection().then(() => {
      this.chatService.joinConversation(this.conversationId);

      // Listen for new messages
      const messageSub = this.chatService.messageReceived$.subscribe(message => {
        if (message.conversationId === this.conversationId) {
          this.addMessage(message);

          // Mark message as read if it's not from current user
          if (message.senderId !== this.currentUserId) {
            this.markAsRead(message.id);
          }
        }
      });

      this.subscriptions.push(messageSub);
    }).catch(err => {
      console.error('Error starting SignalR connection:', err);
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
    }
  }

  ngOnDestroy(): void {
    // Leave the conversation and stop connection
    if (this.conversationId) {
      this.chatService.leaveConversation(this.conversationId).catch(console.error);
    }
    this.chatService.stopConnection().catch(console.error);

    // Unsubscribe from all subscriptions
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  loadConversation(): void {
    this.loading = true;

    // Get conversation details
    this.chatService.getConversation(this.conversationId).subscribe({
      next: (conversation) => {
        this.conversation = conversation;
        this.otherUser = conversation.user1Id === this.currentUserId ? conversation.user2 : conversation.user1;

        // Get conversation messages
        this.chatService.getConversationMessages(this.conversationId).subscribe({
          next: (messages) => {
            this.messages = messages;
            this.loading = false;


            // Mark unread messages as read
            this.markUnreadMessagesAsRead();
          },
          error: (error) => {
            console.error('Error loading messages', error);
            this.loading = false;
          }
        });
      },
      error: (error) => {
        console.error('Error loading conversation', error);
        this.loading = false;
      }
    });
  }

  sendMessage(event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    if (this.messageControl.invalid || this.sending) {
      return;
    }

    const content = this.messageControl.value?.trim() || '';
    if (!content) {
      return;
    }

    this.sending = true;
    this.chatService.sendMessage(this.conversationId, content)
      .then(() => {
        this.messageControl.setValue('');
        this.sending = false;
        this.focusMessageInput();
        this.shouldScrollToBottom = true;
        // Allow the DOM to update before scrolling
        setTimeout(() => this.scrollToBottom(), 100);
      })
      .catch(error => {
        console.error('Error sending message:', error);
        this.sending = false;
      });
  }


  markAsRead(messageId: number): void {
    this.chatService.markAsRead(messageId).catch(console.error);
  }

  markUnreadMessagesAsRead(): void {
    // Mark all messages from the other user as read
    const unreadMessages = this.messages.filter(
      m => m.senderId !== this.currentUserId && !m.readAt
    );

    unreadMessages.forEach(message => {
      this.markAsRead(message.id);
    });
  }

  addMessage(message: Message): void {
    // Check if message already exists
    if (!this.messages.some(m => m.id === message.id)) {
      this.messages.push(message);
      this.shouldScrollToBottom = true;
    }
  }

  isCurrentUserMessage(message: Message): boolean {
    return message.senderId === this.currentUserId;
  }

  scrollToBottom(): void {
    try {
      setTimeout(() => {

        if (this.messagesContainer) {
          this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
        }
      }, 100); // Slightly longer delay to ensure DOM update
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }

  focusMessageInput(): void {
    setTimeout(() => {
      this.messageInput.nativeElement.focus();
    }, 0);
  }

  formatTime(date: Date): string {
    return new Date(date).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  onScroll(): void {
    const element = this.messagesContainer.nativeElement;
    const atBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 20;
    this.shouldScrollToBottom = atBottom;
  }
}