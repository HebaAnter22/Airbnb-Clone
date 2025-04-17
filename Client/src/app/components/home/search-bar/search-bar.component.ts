import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, HostListener, EventEmitter, Output, Renderer2, Inject, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatAutocompleteModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './search-bar.component.html',
  styleUrls: ['./search-bar.component.css'],
})
export class SearchBarComponent implements OnInit, AfterViewInit {
  @Input() isHeaderScrolled: boolean = false;
  isSearchModalOpen = false;
  modalMode: 'destination' | 'date' | 'guests' | null = null;
  destination: string = '';
  checkIn: Date | null = null;
  checkOut: Date | null = null;
  guests = { adults: 0, children: 0, infants: 0, pets: 0 };
  isMobile: boolean = false;

  // Date picker properties
  currentDate: Date = new Date(2025, 3, 1); // April 2025
  nextMonthDate: Date = new Date(2025, 4, 1); // May 2025
  daysInMonth: number[] = [];
  daysInNextMonth: number[] = [];
  emptyDaysBefore: number[] = [];
  emptyDaysBeforeNext: number[] = [];
  selectedStartDay: number | null = null;
  selectedEndDay: number | null = null;

  // Suggested destinations
  suggestedDestinations = [
    { name: 'Nearby', description: "Find what's around you", icon: 'near_me' },
    { name: 'Istanbul, Türkiye', description: 'For sights like Galata Tower', icon: 'location_city' },
    { name: 'London, United Kingdom', description: 'For the bustling nightlife', icon: 'landscape' },
    { name: 'Beirut, Lebanon', description: 'Popular beach destination', icon: 'beach_access' },
    { name: 'Paris, France', description: 'For its stunning architecture', icon: 'apartment' },
  ];

  @Output() searchPerformed = new EventEmitter<any>();
  @ViewChild('container', { static: false }) containerRef!: ElementRef<HTMLDivElement>;
  @ViewChild(MatAutocompleteTrigger) autocompleteTrigger!: MatAutocompleteTrigger;

  constructor(
    private renderer: Renderer2,
    @Inject(DOCUMENT) private document: Document
  ) {}

  ngAfterViewInit() {
    setTimeout(() => {
      if (this.autocompleteTrigger && this.autocompleteTrigger.autocomplete) {
        this.autocompleteTrigger.autocomplete.panel.nativeElement.style.width = '300px';
        this.autocompleteTrigger.autocomplete.panel.nativeElement.style.minWidth = '300px';
        this.autocompleteTrigger.autocomplete.panel.nativeElement.style.maxWidth = '300px';
      }
      
      // Create a style element and add it to the document head
      const styleElement = this.renderer.createElement('style');
      const cssText = `
        .cdk-overlay-pane {
          width: 300px !important;
          min-width: 300px !important;
          max-width: 300px !important;
        }
        .mat-mdc-autocomplete-panel {
          width: 300px !important;
          min-width: 300px !important;
          max-width: 300px !important;
        }
      `;
      
      this.renderer.appendChild(styleElement, this.renderer.createText(cssText));
      this.renderer.appendChild(this.document.head, styleElement);
    });
  }

  ngOnInit(): void {
    this.checkScreenSize();
    window.addEventListener('resize', this.checkScreenSize.bind(this));
    this.updateCalendar();
  }

  checkScreenSize(): void {
    this.isMobile = window.innerWidth < 768;
  }

  openSearchModal(): void {
    this.isSearchModalOpen = true;
    this.modalMode = 'destination'; // Start with destination section
    document.querySelector('.search-bar-container')?.classList.add('search-model-open');
  }

  closeSearchModal(): void {
    this.isSearchModalOpen = false;
    this.modalMode = null;
    document.querySelector('.search-bar-container')?.classList.remove('search-model-open');
  }

  toggleSection(section: 'destination' | 'date' | 'guests', event?: Event): void {
    // If event is provided, stop propagation
    if (event) {
      event.stopPropagation();
    }
    
    // Only toggle the section if it's not already active
    if (this.modalMode !== section) {
      this.modalMode = section;
    }
    
    // Only close the modal if explicitly requested (not when toggling sections)
    if (this.isMobile && !this.modalMode && section === null) {
      this.closeSearchModal();
    }
  }

  clearAll(): void {
    this.destination = '';
    this.checkIn = null;
    this.checkOut = null;
    this.guests = { adults: 0, children: 0, infants: 0, pets: 0 };
    this.selectedStartDay = null;
    this.selectedEndDay = null;
    this.modalMode = null;
    if (this.isMobile) {
      this.closeSearchModal();
    }
  }

  clearDates(): void {
    this.checkIn = null;
    this.checkOut = null;
    this.selectedStartDay = null;
    this.selectedEndDay = null;
  }

  selectDestination(destination: string, event?: any): void {
    this.destination = destination;
    this.modalMode = this.isMobile ? 'date' : null; // Move to next section on mobile
    
    // We don't need to stop propagation for MatOptionSelectionChange
  }

  getCheckInDisplay(): string {
    return this.checkIn
      ? this.checkIn.toLocaleString('default', { month: 'short', day: 'numeric' })
      : 'From';
  }

  getCheckOutDisplay(): string {
    return this.checkOut
      ? this.checkOut.toLocaleString('default', { month: 'short', day: 'numeric' })
      : 'To';
  }

  getGuestSummary(): string {
    const total = this.guests.adults ;
    return total > 0 ? `${total} guest${total > 1 ? 's' : ''}` : 'Guests';
  }

  updateGuests(type: 'adults', increment: boolean, event?: Event): void {
    const currentValue = this.guests[type];
    if (increment) {
      this.guests[type] = currentValue + 1;
    } else if (currentValue > 0) {
      this.guests[type] = currentValue - 1;
    }
    
    // Prevent the modal from closing when updating guests
    if (event) {
      event.stopPropagation();
    }
  }

  updateCalendar(): void {
    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    this.emptyDaysBefore = Array(firstDay).fill(null);
    this.daysInMonth = Array.from({ length: daysInMonth }, (_, i) => i + 1);

    const nextYear = this.nextMonthDate.getFullYear();
    const nextMonth = this.nextMonthDate.getMonth();
    const firstDayNext = new Date(nextYear, nextMonth, 1).getDay();
    const daysInNextMonth = new Date(nextYear, nextMonth + 1, 0).getDate();
    this.emptyDaysBeforeNext = Array(firstDayNext).fill(null);
    this.daysInNextMonth = Array.from({ length: daysInNextMonth }, (_, i) => i + 1);
  }

  previousMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    this.nextMonthDate = new Date(this.nextMonthDate.getFullYear(), this.nextMonthDate.getMonth() - 1, 1);
    this.updateCalendar();
  }

  nextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
    this.nextMonthDate = new Date(this.nextMonthDate.getFullYear(), this.nextMonthDate.getMonth() + 1, 1);
    this.updateCalendar();
  }

  getMonthYear(date: Date): string {
    return date.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  selectDay(day: number, isNextMonth: boolean = false): void {
    const date = isNextMonth ? this.nextMonthDate : this.currentDate;
    const selectedDate = new Date(date.getFullYear(), date.getMonth(), day);
    if (!this.checkIn || (this.checkIn && this.checkOut)) {
      this.checkIn = selectedDate;
      this.checkOut = null;
      this.selectedStartDay = day;
      this.selectedEndDay = null;
    } else if (this.checkIn && !this.checkOut) {
      if (selectedDate < this.checkIn) {
        this.checkIn = selectedDate;
        this.selectedStartDay = day;
      } else {
        this.checkOut = selectedDate;
        this.selectedEndDay = day;
      }
    }
  }

  isDaySelected(day: number, isNextMonth: boolean = false): boolean {
    if (!this.checkIn) return false;
    const date = isNextMonth ? this.nextMonthDate : this.currentDate;
    const currentDate = new Date(date.getFullYear(), date.getMonth(), day);
    const checkInDate = new Date(this.checkIn);
    const checkOutDate = this.checkOut ? new Date(this.checkOut) : null;
    return (
      currentDate.getTime() === checkInDate.getTime() ||
      (checkOutDate !== null && currentDate.getTime() === checkOutDate.getTime())
    );
  }

  isDayInRange(day: number, isNextMonth: boolean = false): boolean {
    if (!this.checkIn || !this.checkOut) return false;
    const date = isNextMonth ? this.nextMonthDate : this.currentDate;
    const currentDate = new Date(date.getFullYear(), date.getMonth(), day);
    return currentDate > this.checkIn && currentDate < this.checkOut;
  }

  onSearch(): void {
    this.searchPerformed.emit({
      destination: this.destination,
      checkIn: this.checkIn,
      checkOut: this.checkOut,
      guests: this.guests,
    });
    if (this.isMobile) {
      this.closeSearchModal();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isMobile && this.modalMode) {
      const searchBar = this.containerRef.nativeElement.querySelector('.search-bar');
      const searchModel = this.containerRef.nativeElement.querySelector('.search-model-container');
      const target = event.target as Node;
      if (!searchBar?.contains(target) && !searchModel?.contains(target)) {
        this.modalMode = null;
      }
    }
  }
}