// header.component.ts
import { Component, HostListener } from '@angular/core';
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

  @HostListener('window:scroll', ['$event'])
  onScroll() {
    const header = document.querySelector('.header');
    if (window.scrollY > 50) {
      header?.classList.add('scrolled');
    } else {
      header?.classList.remove('scrolled');
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