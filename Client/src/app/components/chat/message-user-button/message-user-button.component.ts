// src/app/components/chat/message-user-button/message-user-button.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ChatService } from '../../../services/chat.service';

@Component({
  selector: 'app-message-user-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './message-user-button.component.html',
  styleUrls: ['./message-user-button.component.css']
})
export class MessageUserButtonComponent {
  @Input() userId: number = 0;
  @Input() buttonText: string = 'Message';
  @Input() buttonClass: string = '';

  constructor(
    private chatService: ChatService,
    private router: Router
  ) { }

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