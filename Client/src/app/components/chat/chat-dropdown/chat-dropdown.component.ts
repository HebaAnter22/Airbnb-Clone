import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { ChatService, Message } from '../../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { Router } from '@angular/router';
import { TruncatePipe } from '../../../pipes/truncate.pipe';
import { NgForOf, NgIf } from '@angular/common';
import { debounceTime, distinct, distinctUntilKeyChanged, Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-chat-dropdown',
  templateUrl: './chat-dropdown.component.html',
  imports: [TruncatePipe, NgIf, NgForOf],
  standalone: true,
  styleUrls: ['./chat-dropdown.component.css']
})
export class ChatDropdownComponent implements OnInit, OnDestroy {
  unreadCount = 0;
  conversations: any[] = [];
  showDropdown = false;
  hasNewMessage = false;
  private previousUnreadCount = 0;
  private destroy$ = new Subject<void>();
  constructor(
    public chatService: ChatService,
    private authService: AuthService,
    private router: Router
  ) {
    this.chatService.conversations$.subscribe(conversations => {
      this.conversations = conversations;
      this.calculateUnreadCount();
    });

    // Subscribe to real-time SignalR messages to update unread count
    this.chatService.messages$.subscribe(() => {
      this.calculateUnreadCount();
    });

    // Additionally, subscribe to unreadCount directly if available
    this.chatService.getUnreadCount().subscribe(count => {
      this.unreadCount = count;
    });

  }


  ngOnInit(): void {
    // Subscribe to unread count updates
    this.chatService.unreadCount$.pipe(
      takeUntil(this.destroy$),
      debounceTime(300) // Debounce to avoid rapid updates
    ).subscribe(count => {
      this.unreadCount = count;
      if (count > this.previousUnreadCount) {
        this.triggerNewMessageEffect();
      }
      this.previousUnreadCount = count;
    });

    // Initial load
    this.chatService.conversations$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(conversations => {
      this.conversations = conversations;
      this.chatService.triggerUnreadCountUpdate(); // Trigger update when conversations change
    });

    // Subscribe to messages to update count
    this.chatService.messages$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.chatService.triggerUnreadCountUpdate();
    });

    // Initialize SignalR connection if not already done
    this.chatService.initializeSignalRConnection();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  calculateUnreadCount(): void {
    this.unreadCount = this.conversations.reduce((count, conv) => {
      return count + (this.chatService.hasUnreadMessages(conv) ? 1 : 0);
    }, 0);
  }

  private triggerNewMessageEffect(): void {
    this.hasNewMessage = true;

    // Remove the animation class after it completes
    setTimeout(() => {
      this.hasNewMessage = false;
    }, 3000); // Matches the animation duration
  }



  toggleDropdown(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.showDropdown = !this.showDropdown;

    if (this.showDropdown) {
      this.chatService.getUserConversations().subscribe({
        next: (conversations) => {
          console.log('Conversations loaded:', conversations);
        },
        error: (error) => {
          console.error('Error loading conversations:', error);
        }
      });
    }
  }

  navigateToChat(conversationId: number): void {
    this.router.navigate(['/chat']);
    this.showDropdown = false;
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.chat-dropdown-container') && this.showDropdown) {
      this.showDropdown = false;
    }
  }
}