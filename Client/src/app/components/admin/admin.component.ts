import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ProfileComponent } from '../profile/profile.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileComponent],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {
  adminName: string = 'Admin User';
  adminPicture: string = 'assets/images/admin-avatar.png';
  currentSection: string = 'dashboard';
  isSidebarCollapsed: boolean = false;

  // Host management sections
  hostSections = {
    allHosts: false,
    verifiedHosts: false,
    unverifiedHosts: false,
    reports: false
  };

  // Property management sections
  propertySections = {
    allProperties: false,
    unverified: false,
    verified: false
  };

  // Dropdown states
  isHostDropdownOpen: boolean = false;
  isPropertyDropdownOpen: boolean = false;

  constructor(private router: Router) { }

  ngOnInit(): void {
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    
    // Close dropdowns when collapsing sidebar
    if (this.isSidebarCollapsed) {
      this.isHostDropdownOpen = false;
      this.isPropertyDropdownOpen = false;
    }
  }

  toggleHostSection(): void {
    // If sidebar is collapsed, directly navigate to the first host section
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('all-hosts');
      return;
    }
    
    this.isHostDropdownOpen = !this.isHostDropdownOpen;
    // Close property dropdown if open
    if (this.isPropertyDropdownOpen) {
      this.isPropertyDropdownOpen = false;
    }
  }

  togglePropertySection(): void {
    // If sidebar is collapsed, directly navigate to the first property section
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('all-properties');
      return;
    }
    
    this.isPropertyDropdownOpen = !this.isPropertyDropdownOpen;
    // Close host dropdown if open
    if (this.isHostDropdownOpen) {
      this.isHostDropdownOpen = false;
    }
  }

  setCurrentSection(section: string): void {
    this.currentSection = section;
    // Close dropdowns after selection
    this.isHostDropdownOpen = false;
    this.isPropertyDropdownOpen = false;
  }

  logout(): void {
    // Clear any stored tokens or user data
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    
    // Navigate to login page
    this.router.navigate(['/login']);
  }
} 