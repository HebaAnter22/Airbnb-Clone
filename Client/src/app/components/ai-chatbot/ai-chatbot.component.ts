import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AIService, AIRequestDTO, AIResponseDTO } from '../../services/ai-service.service';
import { Subscription } from 'rxjs';

interface ChatMessage {
  sender: 'You' | 'Assistant';
  content: string;
  timestamp: Date;
}

@Component({
  selector: 'app-ai-chatbot',
  templateUrl: './ai-chatbot.component.html',
  styleUrls: ['./ai-chatbot.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class AiChatbotComponent implements OnInit, OnDestroy {
  isChatOpen = false;
  messages: ChatMessage[] = [];
  userInput = '';
  isLoading = false;
  private subscription: Subscription | null = null;
  maxMessages = 10;

  constructor(private aiService: AIService) { }

  ngOnInit(): void {
    const storedMessages = localStorage.getItem('chatMessages');
    if (storedMessages) {
      this.messages = JSON.parse(storedMessages);
      if (this.messages.length > this.maxMessages) {
        this.messages = this.messages.slice(-this.maxMessages);
        this.saveMessages();
      }
    }
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  toggleChat(): void {
    this.isChatOpen = !this.isChatOpen;
  }

  sendMessage(): void {
    if (this.userInput.trim() === '' || this.isLoading) {
      return;
    }

    const userMessage: ChatMessage = {
      sender: 'You',
      content: this.userInput,
      timestamp: new Date()
    };
    this.messages.push(userMessage);
    this.userInput = '';
    this.isLoading = true;

    const requestType: AIRequestDTO['RequestType'] = this.determineRequestType(userMessage.content);

    this.subscription = this.aiService.sendAIRequest(
      userMessage.content,
      requestType
    ).subscribe({
      next: (response: AIResponseDTO) => {
        const aiMessage: ChatMessage = {
          sender: 'Assistant',
          content: response.success ? response.response : `Error: ${response.errorMessage || 'Unable to get response from AI service'}`,
          timestamp: new Date(response.timestamp)
        };
        this.messages.push(aiMessage);
        this.isLoading = false;
        this.trimMessages();
        this.saveMessages();
      },
      error: (err: Error) => {
        console.error('AI Service Error:', err);
        const errorMessage: ChatMessage = {
          sender: 'Assistant',
          content: `Sorry, something went wrong: ${err.message}`,
          timestamp: new Date()
        };
        this.messages.push(errorMessage);
        this.isLoading = false;
        this.trimMessages();
        this.saveMessages();
      }
    });
  }

  private determineRequestType(query: string): AIRequestDTO['RequestType'] {
    const lowerQuery = query.toLowerCase();
    if (lowerQuery.includes('book') || lowerQuery.includes('reservation') || lowerQuery.includes('stay')) {
      return 'booking';
    } else if (lowerQuery.includes('property') || lowerQuery.includes('apartment') || lowerQuery.includes('house')) {
      return 'property';
    } else if (lowerQuery.includes('available') || lowerQuery.includes('availability') || lowerQuery.includes('dates')) {
      return 'availability';
    }
    return 'chat';
  }

  private trimMessages(): void {
    if (this.messages.length > this.maxMessages) {
      this.messages = this.messages.slice(-this.maxMessages);
    }
  }

  private saveMessages(): void {
    localStorage.setItem('chatMessages', JSON.stringify(this.messages));
  }

  clearChat(): void {
    this.messages = [];
    localStorage.removeItem('chatMessages');
  }

  formatMessage(content: string): string {
    if (!content) return '';
    
    content = content.replace(/###\s+(.*?)(?=\n|$)/g, '<h3>$1</h3>');
    content = content.replace(/##\s+(.*?)(?=\n|$)/g, '<h2>$1</h2>');
    content = content.replace(/#\s+(.*?)(?=\n|$)/g, '<h1>$1</h1>');
    content = content.replace(/\*\*(.*?)\*\*/g, '$1');
    content = content.replace(/(?:[-*])\s+([^*\n]+?)(?=\n|$)/g, '<li>$1</li>');
    if (content.includes('<li>')) {
      content = content.replace(/(<li>.*<\/li>)/g, '<ul>$1</ul>');
    }
    content = content.replace(/\n\s*\n/g, '</p><p>');
    content = content.replace(/\n(?!\s*<)/g, '<br>');
    if (!content.includes('<p>')) {
      content = '<p>' + content + '</p>';
    }
    
    return content;
  }

  onEnterPress(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (!keyboardEvent.shiftKey) {
      this.sendMessage();
    }
  }
}