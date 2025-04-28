import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, RouterOutlet } from '@angular/router';
import { ChatSignalRService } from '../../../../services/chatSignal.service';
import { Title } from '@angular/platform-browser';
import { NgIf } from '@angular/common';
import { ConversationListComponent } from '../conversation-list/conversation-list.component';

@Component({
  selector: 'app-chat-main',
  templateUrl: './chat-main.component.html',
  imports: [NgIf, RouterOutlet, ConversationListComponent],
  styleUrls: ['./chat-main.component.scss']
})
export class ChatMainComponent implements OnInit {
  unreadCount = 0;
  activeConversationId: number | null = null;

  constructor(
    private chatService: ChatSignalRService,
    private router: Router,
    private route: ActivatedRoute,
    private titleService: Title
  ) { }

  ngOnInit(): void {
    // Start SignalR connection when component initializes
    this.chatService.startConnection().then(() => {
      console.log('SignalR connection established');
    }).catch(err => {
      console.error('Error starting SignalR connection:', err);
    });

    // Load unread message count
    this.chatService.loadUnreadCount();

    // Subscribe to unread count updates
    this.chatService.unreadCount$.subscribe(count => {
      this.unreadCount = count;
      this.updatePageTitle();
    });

    // Check if a specific conversation is active
    this.route.firstChild?.params.subscribe(params => {
      if (params['id']) {
        this.activeConversationId = +params['id'];
      } else {
        this.activeConversationId = null;
      }
    });
  }

  updatePageTitle(): void {
    if (this.unreadCount > 0) {
      this.titleService.setTitle(`(${this.unreadCount}) Messages - Your App Name`);
    } else {
      this.titleService.setTitle('Messages - Your App Name');
    }
  }
}