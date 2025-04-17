import { Component, HostListener, Input } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  @Input() isHeaderScrolled: boolean = false;
  isDropdownOpen = false;

  toggleDropdown(event: Event) {
    event.stopPropagation(); // Prevent the click from bubbling up to the document
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    const target = event.target as HTMLElement;
    const menuProfile = document.querySelector('.menu-profile');
    const dropdown = document.querySelector('.dropdown');

    // Close dropdown if clicking outside the menu-profile and dropdown
    if (
      this.isDropdownOpen &&
      menuProfile &&
      dropdown &&
      !menuProfile.contains(target) &&
      !dropdown.contains(target)
    ) {
      this.isDropdownOpen = false;
    }
  }
}