// src/app/components/chat/message-user-button/message-user-button.component.ts
import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ChatSignalRService } from '../../../../services/chatSignal.service';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-message-user-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './message-user-button.component.html',
  styleUrls: ['./message-user-button.component.css']
})
export class MessageUserButtonComponent implements OnInit {
  @Input() userId: number = 0;
  @Input() buttonText: string = 'Message';
  @Input() buttonClass: string = '';
  
  isCurrentUser: boolean = false;
  shouldShowButton: boolean = true;

  constructor(
    private chatService: ChatSignalRService,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    // Check if the userId is the current user's ID
    const currentUserId = parseInt(this.authService.userId || '0');
    this.isCurrentUser = this.userId === currentUserId;
    
    // Don't show message button for Admin users or when messaging yourself
    this.shouldShowButton = !this.isCurrentUser && !this.authService.isAdmin();
    
    console.log('Message button for user ID:', this.userId);
    console.log('Current user ID:', currentUserId);
    console.log('Is current user (message button):', this.isCurrentUser);
    console.log('Is admin:', this.authService.isAdmin());
    console.log('Should show button:', this.shouldShowButton);
  }

  startChat(): void {
    if (!this.userId) {
      console.error('No user ID provided');
      return;
    }

    this.chatService.createConversation(this.userId).subscribe({
      next: (conversation) => {
        // Navigate to chat with this conversation ID
        this.router.navigate(['/chat', conversation.id]);
      },
      error: (err) => console.error('Error starting chat:', err)
    });
  }
}