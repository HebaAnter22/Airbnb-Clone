// src/app/components/chat/chat-list/chat-list.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Conversation } from '../../../services/chat.service';
import { ChatService } from '../../../services/chat.service';

@Component({
  selector: 'app-chat-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.css']
})
export class ChatListComponent {
  @Input() conversations: Conversation[] = [];
  @Input() selectedConversation: Conversation | null = null;
  @Input() currentUserId: number = 0;
  
  @Output() conversationSelected = new EventEmitter<Conversation>();
  
  constructor(private chatService: ChatService) {}
  
  selectConversation(conversation: Conversation): void {
    this.conversationSelected.emit(conversation);
  }
  
  getOtherUser(conversation: Conversation): any {
    return this.chatService.getOtherUser(conversation);
  }
  
  getLastMessage(conversation: Conversation): string {
    if (!conversation.messages || conversation.messages.length === 0) {
      return 'No messages yet';
    }

    const lastMessage = conversation.messages[0];
    const maxLength = 30;
    let content = lastMessage.content;
    
    if (content.length > maxLength) {
      content = content.substring(0, maxLength) + '...';
    }
    
    return content;
  }
  
  getLastMessageTime(conversation: Conversation): string {
    if (!conversation.messages || conversation.messages.length === 0) {
      return '';
    }
    
    const lastMessage = conversation.messages[0];
    const sentDate = new Date(lastMessage.sentAt);
    const now = new Date();
    
    // If today, return time
    if (sentDate.toDateString() === now.toDateString()) {
      return sentDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    
    // If this week, return day name
    const diffDays = Math.floor((now.getTime() - sentDate.getTime()) / (1000 * 3600 * 24));
    if (diffDays < 7) {
      return sentDate.toLocaleDateString([], { weekday: 'short' });
    }
    
    // Otherwise return date
    return sentDate.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }
  
  hasUnreadMessages(conversation: Conversation): boolean {
    if (!conversation.messages || conversation.messages.length === 0) {
      return false;
    }
    
    return conversation.messages.some(m => 
      m.senderId !== this.currentUserId && !m.readAt
    );
  }
}