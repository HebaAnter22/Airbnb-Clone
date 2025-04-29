import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/home/navbar/navbar.component';
import { AuthService } from './services/auth.service';
import { AiChatbotComponent } from './components/ai-chatbot/ai-chatbot.component';
import { HeaderComponent } from './components/home/header/header.component';
import { AdminRedirectService } from './services/admin-redirect.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, AiChatbotComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'AirBnb';
 
  constructor(
    public authService: AuthService,
    private adminRedirectService: AdminRedirectService
  ) {}

  ngOnInit(): void {
    // Initialize admin redirect service to check current route
    this.adminRedirectService.initializeRedirect();
  }
}