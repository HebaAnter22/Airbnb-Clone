// header.component.ts
import { Component, HostListener, Output, EventEmitter } from '@angular/core';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { NavbarComponent } from '../navbar/navbar.component';
import { Route } from '@angular/router';
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [SearchBarComponent,NavbarComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  isSearchModalOpen: boolean = false;
  modalMode: string | null = null;
  isScrolled: boolean = false;

  @Output() scrollStateChanged = new EventEmitter<boolean>();

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

  dropdownClicked() {
    const dropdown = document.querySelector('.dropdown-menu') as HTMLElement;
    dropdown.classList.toggle('show');
  }
}