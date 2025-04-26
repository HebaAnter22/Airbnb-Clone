// src/app/components/chat/chat.component.ts
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { ChatService, Conversation, Message } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { ChatListComponent } from './chat-list/chat-list.component';
import { ChatMessagesComponent } from './chat-messages/chat-messages.component';
import { ChatInputComponent } from './chat-input/chat-input.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ChatListComponent,
    ChatMessagesComponent,
    ChatInputComponent
  ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, OnDestroy {
  conversations: Conversation[] = [];
  selectedConversation: Conversation | null = null;
  messages: Message[] = [];
  currentUserId: number = 0;
  isMobileView: boolean = false;
  showConversationList: boolean = true;

  private destroy$ = new Subject<void>();

  constructor(
    public chatService: ChatService,
    private authService: AuthService,
    private route: ActivatedRoute
  ) {
    this.currentUserId = Number(this.authService.userId);
    this.isMobileView = window.innerWidth < 768;
  }

  ngOnInit(): void {
    //do a refresh reload the page 
    if (!sessionStorage.getItem('chatRefreshed')) {
      sessionStorage.setItem('chatRefreshed', 'true');
      window.location.reload();
    } else {
      sessionStorage.removeItem('chatRefreshed');
    }
    this.chatService.initializeSignalRConnection();

    this.loadConversations();

    this.chatService.conversations$
      .pipe(takeUntil(this.destroy$))
      .subscribe(conversations => {
        this.conversations = conversations;
      });

    this.chatService.selectedConversation$
      .pipe(takeUntil(this.destroy$))
      .subscribe(conversation => {
        this.selectedConversation = conversation;
        if (conversation && this.isMobileView) {
          this.showConversationList = false;
        }
      });

    this.chatService.messages$
      .pipe(takeUntil(this.destroy$))
      .subscribe(messages => {
        this.messages = messages;
        this.markUnreadMessagesAsRead();
      });

    // Check if a conversation ID was passed in the route
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const conversationId = params['id'];
      if (conversationId) {
        this.loadConversation(Number(conversationId));
      }
    });

    // Listen for window resize events to adjust the view
    window.addEventListener('resize', this.onResize.bind(this));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.chatService.stopConnection();
    window.removeEventListener('resize', this.onResize.bind(this));
  }

  loadConversations(): void {
    this.chatService.getUserConversations().subscribe({
      error: (err) => console.error('Error loading conversations:', err)
    });
  }

  loadConversation(conversationId: number): void {
    this.chatService.getConversation(conversationId).subscribe({
      next: (conversation) => {
        this.joinConversation(conversation);
      },
      error: (err) => console.error('Error loading conversation:', err)
    });
  }

  selectConversation(conversation: Conversation): void {
    if (this.selectedConversation?.id === conversation.id) {
      return;
    }

    // Leave current conversation if any
    if (this.selectedConversation) {
      this.chatService.leaveConversation(this.selectedConversation.id);
    }

    this.joinConversation(conversation);
  }

  joinConversation(conversation: Conversation): void {
    this.chatService.setSelectedConversation(conversation);

    // Make sure we're connected to SignalR before joining
    this.chatService.ensureConnection().then(() => {
      this.chatService.joinConversation(conversation.id).subscribe({
        next: (messages) => {
          console.log(`Joined conversation ${conversation.id} with ${messages.length} messages`);
        },
        error: (err) => console.error('Error joining conversation:', err)
      });
    });
  }

  sendMessage(content: string): void {
    if (!this.selectedConversation || !content.trim()) {
      return;
    }

    this.chatService.sendMessage(this.selectedConversation.id, content);
  }

  markUnreadMessagesAsRead(): void {
    if (!this.messages.length || !this.currentUserId) {
      return;
    }

    const unreadMessages = this.messages.filter(
      m => m.senderId !== this.currentUserId && !m.readAt
    );

    unreadMessages.forEach(message => {
      this.chatService.markMessageAsRead(message.id);
    });
  }

  backToConversations(): void {
    this.showConversationList = true;
  }

  private onResize(): void {
    this.isMobileView = window.innerWidth < 768;
    if (!this.isMobileView) {
      this.showConversationList = true;
    }
  }

  startChat(userId: number): void {
    this.chatService.createConversation(userId).subscribe({
      next: (conversation) => {
        this.joinConversation(conversation);
      },
      error: (err) => console.error('Error creating conversation:', err)
    });
  }
}