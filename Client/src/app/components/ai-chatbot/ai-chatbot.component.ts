import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AIService, AIRequestDTO, AIResponseDTO } from '../../services/ai-service.service';
import { Subscription } from 'rxjs';
import { HttpClient } from '@angular/common/http';

interface Message {
  text: string;
  isUser: boolean;
  audioUrl?: string;
  sender: string;
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
  @ViewChild('audioPlayer') audioPlayer!: ElementRef<HTMLAudioElement>;
  @ViewChild('audioRecorder') audioRecorder!: ElementRef<HTMLAudioElement>;
  
  isChatOpen = false;
  messages: Message[] = [];
  userInput: string = '';
  isLoading = false;
  private subscription: Subscription | null = null;
  maxMessages = 10;
  isRecording: boolean = false;
  mediaRecorder: MediaRecorder | null = null;
  audioChunks: Blob[] = [];
  isPlaying: boolean = false;
  currentPlayingMessageIndex: number = -1;
  isVoiceSession: boolean = false; // Track if current session was initiated by voice

  constructor(private aiService: AIService, private http: HttpClient) { }

  ngOnInit(): void {
    const storedMessages = localStorage.getItem('chatMessages');
    if (storedMessages) {
      this.messages = JSON.parse(storedMessages);
      if (this.messages.length > this.maxMessages) {
        this.messages = this.messages.slice(-this.maxMessages);
        this.saveMessages();
      }
    }

    this.messages.push({
      text: 'Hello! I am your AI assistant. How can I help you today?',
      isUser: false,
      sender: 'Assistant',
      content: 'Hello! I am your AI assistant. How can I help you today?',
      timestamp: new Date()
    });
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  toggleChat(): void {
    this.isChatOpen = !this.isChatOpen;
  }

  async startRecording() {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.mediaRecorder = new MediaRecorder(stream);
      this.audioChunks = [];
      this.isRecording = true;
      this.isVoiceSession = true; // Mark this as a voice session

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.onstop = async () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/wav' });
        await this.sendAudioForTranscription(audioBlob);
        this.isRecording = false;
      };

      this.mediaRecorder.start();
    } catch (error) {
      console.error('Error accessing microphone:', error);
    }
  }

  stopRecording() {
    if (this.mediaRecorder && this.isRecording) {
      this.mediaRecorder.stop();
      this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
    }
  }

  async sendAudioForTranscription(audioBlob: Blob) {
    const formData = new FormData();
    formData.append('audioFile', audioBlob, 'recording.wav');

    try {
      // Add a transcribing feedback message
      const transcribingMessage: Message = {
        text: 'Transcribing your voice...',
        isUser: false,
        sender: 'System',
        content: 'Transcribing your voice...',
        timestamp: new Date()
      };
      this.messages.push(transcribingMessage);
      this.saveMessages();

      const response = await this.http.post<AIResponseDTO>('https://localhost:7228/api/ai/transcribe', formData).toPromise();
      
      // Remove the transcribing message
      const index = this.messages.indexOf(transcribingMessage);
      if (index !== -1) {
        this.messages.splice(index, 1);
        this.saveMessages();
      }
      
      if (response && response.success) {
        this.userInput = response.response;
        this.sendMessage(true); // Pass true to indicate voice input
      }
    } catch (error) {
      console.error('Error transcribing audio:', error);
      
      // Remove any transcribing message and show error
      const index = this.messages.findIndex(m => m.text === 'Transcribing your voice...');
      if (index !== -1) {
        this.messages.splice(index, 1);
      }
      
      this.messages.push({
        text: 'Sorry, I couldn\'t transcribe your voice. Please try again.',
        isUser: false,
        sender: 'System',
        content: 'Sorry, I couldn\'t transcribe your voice. Please try again.',
        timestamp: new Date()
      });
      this.saveMessages();
    }
  }

  async sendMessage(isVoiceInput: boolean = false) {
    if (!this.userInput.trim()) return;

    // If this is a text input, reset the voice session flag
    if (!isVoiceInput) {
      this.isVoiceSession = false;
    }

    const userMessage: Message = {
      text: this.userInput,
      isUser: true,
      sender: 'You',
      content: this.userInput,
      timestamp: new Date()
    };
    this.messages.push(userMessage);
    this.isLoading = true;
    this.saveMessages();
    this.scrollToBottom(); // Scroll to show the user's message

    // Add thinking indicator immediately after user input
    const thinkingMessage: Message = {
      text: 'Thinking...',
      isUser: false,
      sender: 'Assistant',
      content: '<p><i>Thinking...</i></p>',
      timestamp: new Date()
    };
    
    // Only add thinking indicator if we don't already have the loading animation
    if (!this.isLoading) {
      this.messages.push(thinkingMessage);
      this.saveMessages();
      this.scrollToBottom();
    }

    try {
      const request: AIRequestDTO = {
        Query: this.userInput,
        RequestType: 'chat'
      };

      const response = await this.aiService.getChatResponse(request).toPromise();
      
      // Remove thinking message
      const thinkingIndex = this.messages.indexOf(thinkingMessage);
      if (thinkingIndex !== -1) {
        this.messages.splice(thinkingIndex, 1);
        this.saveMessages();
      }
      
      if (response?.success) {
        const aiMessage: Message = {
          text: response.response,
          isUser: false,
          sender: 'Assistant',
          content: response.response,
          timestamp: new Date()
        };
        this.messages.push(aiMessage);
        this.saveMessages();
        this.scrollToBottom(); // Scroll to show the AI's response

        // Generate audio for AI response
        if (response.response.length > 0) {
          try {
            console.log('Requesting text-to-speech for response');
            
            // Add a small processing message for voice responses if needed
            let processingMessage: Message | null = null;
            if (this.isVoiceSession) {
              processingMessage = {
                text: 'Generating audio...',
                isUser: false,
                sender: 'System',
                content: '<p><i>Generating audio...</i></p>',
                timestamp: new Date()
              };
              this.messages.push(processingMessage);
              this.saveMessages();
              this.scrollToBottom();
            }
            
            const audioResponse = await this.aiService.textToSpeech(response.response).toPromise();
            
            // Remove processing message if it exists
            if (processingMessage) {
              const processingIndex = this.messages.indexOf(processingMessage);
              if (processingIndex !== -1) {
                this.messages.splice(processingIndex, 1);
                this.saveMessages();
              }
            }
            
            if (audioResponse?.success && audioResponse.response) {
              console.log('Audio response received, length:', audioResponse.response.length);
              
              // Validate the base64 data before creating the data URL
              const base64Data = audioResponse.response.trim();
              
              try {
                // Create data URL from the base64 string
                aiMessage.audioUrl = `data:audio/mp3;base64,${base64Data}`;
                this.saveMessages();
                
                // Auto-play audio if this is a voice session
                if (this.isVoiceSession) {
                  // Reduce delay from 500ms to 100ms
                  setTimeout(() => {
                    const messageIndex = this.messages.indexOf(aiMessage);
                    if (messageIndex !== -1 && aiMessage.audioUrl) {
                      console.log('Auto-playing audio response for voice input');
                      this.playAudio(aiMessage.audioUrl, messageIndex);
                    }
                  }, 100); // Reduced delay to make response feel more immediate
                }
              } catch (validationError) {
                console.error('Audio validation error:', validationError);
                throw new Error('Failed to create valid audio from response');
              }
            } else {
              console.warn('No audio data in response or unsuccessful response');
              throw new Error('No audio data received from server');
            }
          } catch (audioError) {
            console.error('Error generating speech:', audioError);
            
            // Only show audio error if in voice mode
            if (this.isVoiceSession) {
              // Add a message to notify the user of the audio error
              this.messages.push({
                text: 'Sorry, I couldn\'t generate audio for this response.',
                isUser: false,
                sender: 'System',
                content: 'Sorry, I couldn\'t generate audio for this response. Text-to-speech is currently unavailable.',
                timestamp: new Date()
              });
              this.saveMessages();
              this.scrollToBottom();
            }
          }
        }
      }
    } catch (error) {
      console.error('Error sending message:', error);
      this.messages.push({
        text: 'Sorry, there was an error processing your request.',
        isUser: false,
        sender: 'Assistant',
        content: 'Sorry, there was an error processing your request.',
        timestamp: new Date()
      });
      this.saveMessages();
      this.scrollToBottom();
    } finally {
      this.isLoading = false;
      this.userInput = '';
    }
  }

  playAudio(audioUrl: string, index: number) {
    if (!this.audioPlayer) {
      console.error('Audio player element not found');
      return;
    }
    
    if (!audioUrl) {
      console.error('No audio URL provided');
      return;
    }
    
    console.log(`Playing audio for message ${index}, URL starts with: ${audioUrl.substring(0, 30)}...`);
    
    try {
      this.audioPlayer.nativeElement.src = audioUrl;
      
      // Set up oncanplaythrough event to handle when audio is ready to play
      this.audioPlayer.nativeElement.oncanplaythrough = () => {
        console.log('Audio can play through - ready to play');
        this.isPlaying = true;
        this.currentPlayingMessageIndex = index;
        this.audioPlayer.nativeElement.play()
          .then(() => {
            console.log('Audio playback started successfully');
          })
          .catch(error => {
            console.error('Error playing audio:', error);
            this.isPlaying = false;
            this.currentPlayingMessageIndex = -1;
          });
      };
      
      // Set up onerror event to handle loading errors
      this.audioPlayer.nativeElement.onerror = (e) => {
        console.error('Audio loading error:', e);
        console.error('Audio error code:', this.audioPlayer.nativeElement.error?.code);
        console.error('Audio error message:', this.audioPlayer.nativeElement.error?.message);
        this.isPlaying = false;
        this.currentPlayingMessageIndex = -1;
      };
      
      // Set up onended event to handle when audio finishes playing
      this.audioPlayer.nativeElement.onended = () => {
        console.log('Audio playback completed');
        this.isPlaying = false;
        this.currentPlayingMessageIndex = -1;
      };
      
      // Load the audio
      this.audioPlayer.nativeElement.load();
      console.log('Audio loading initiated');
    } catch (error) {
      console.error('General error in audio playback setup:', error);
      this.isPlaying = false;
      this.currentPlayingMessageIndex = -1;
    }
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

  // Helper method to scroll chat to the bottom
  private scrollToBottom(): void {
    setTimeout(() => {
      const chatContainer = document.querySelector('.chat-messages');
      if (chatContainer) {
        chatContainer.scrollTop = chatContainer.scrollHeight;
      }
    }, 50);
  }
}