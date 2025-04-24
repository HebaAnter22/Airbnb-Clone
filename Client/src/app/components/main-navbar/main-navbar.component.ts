import { Component, HostListener, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { NavbarComponent } from '../home/navbar/navbar.component';
import {  Output, EventEmitter } from '@angular/core';
import { NgIf } from '@angular/common';
@Component({
  selector: 'app-main-navbar',
  imports: [CommonModule,NavbarComponent,NgIf],
  templateUrl: './main-navbar.component.html',
  styleUrl: './main-navbar.component.css'
})
export class MainNavbarComponent{
  isSearchModalOpen: boolean = false;
  modalMode: string | null = null;
  isScrolled: boolean = false;
  isGuest: boolean = false; 
  constructor(
    private authSerivce:AuthService,
    private router: Router
  ) {
    // Check if the user is a guest
    this.isGuest = this.authSerivce.isUserAGuest();

  }
switchToHosting() {
  this.authSerivce.switchToHosting().subscribe({
    next: (response) => {
      this.isGuest = false; // Update local flag
      this.router.navigate(['/host']); // Redirect to host dashboard
    },
    error: (err) => {
      console.error('Failed to switch to host', err);
      // Optional: Show a user-friendly error message
    }
  });
}
  @Output() scrollStateChanged = new EventEmitter<boolean>();
  @Output() searchPerformed = new EventEmitter<any>();

  @HostListener('window:scroll', ['$event'])
  onScroll() {
    const scrolled = window.scrollY > 50;
    
    if (scrolled !== this.isScrolled) {
      this.isScrolled = scrolled;
      this.scrollStateChanged.emit(this.isScrolled);
      
      const header = document.querySelector('.header');
      if (scrolled) {
        header?.classList.add('scrolled');
      } else {
        header?.classList.remove('scrolled');
      }
    }
  }

  // Method to open the search modal
  openSearchModal(mode: string) {
    console.log('Opening modal in mode:', mode);
    this.isSearchModalOpen = true;
    this.modalMode = mode;
  }

  // Your provided method to close the search modal
  closeSearchModal() {
    console.log('Closing modal');
    this.isSearchModalOpen = false;
    this.modalMode = null;
  }

  onSearch(searchParams: any) {
    console.log('Search params in header:', searchParams);
    this.searchPerformed.emit(searchParams);
  }

  dropdownClicked() {
    const dropdown = document.querySelector('.dropdown-menu') as HTMLElement;
    dropdown.classList.toggle('show');
  }
}