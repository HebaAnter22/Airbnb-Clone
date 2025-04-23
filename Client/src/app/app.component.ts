import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/home/navbar/navbar.component';
import { AuthService } from './services/auth.service';
import { AiChatbotComponent } from './components/ai-chatbot/ai-chatbot.component';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet,NavbarComponent,AiChatbotComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent{
  title = 'AirBnb';
 
  constructor(public authService: AuthService) {}
}