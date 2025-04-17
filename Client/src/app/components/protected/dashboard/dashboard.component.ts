import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth.service';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../../services/profile.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  imports: [CommonModule],
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent {
  constructor(public authService: AuthService,
              private profileService: ProfileService, 
  ) {}

  logout() {
    console.log('Logging out...'); // Debugging log
    this.authService.logout();
    
  }
  goToMyProfile() {
    return this.profileService.navigateToUserProfile();
  }
 
 

}