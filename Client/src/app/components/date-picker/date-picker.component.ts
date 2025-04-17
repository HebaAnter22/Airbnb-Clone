import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="date-picker-overlay" *ngIf="isOpen" (click)="closeOnBackdropClick($event)">
      <div class="date-picker-container">
        <div class="date-picker-header">
          <div class="stay-duration">
            <h2>{{ nights }} nights</h2>
            <p>{{ formatDateRange() }}</p>
          </div>
          <div class="date-inputs">
            <div class="date-input-group">
              <label>CHECK-IN</label>
              <div class="date-input">{{ formatDate(checkInDate) }}</div>
              <button class="clear-date" *ngIf="checkInDate" (click)="clearDate('checkIn')">×</button>
            </div>
            <div class="date-input-group">
              <label>CHECKOUT</label>
              <div class="date-input">{{ formatDate(checkOutDate) }}</div>
              <button class="clear-date" *ngIf="checkOutDate" (click)="clearDate('checkOut')">×</button>
            </div>
          </div>
        </div>
        
        <div class="calendar-container">
          <div class="calendar-navigation">
            <button class="nav-button prev" (click)="previousMonth()" [disabled]="isPreviousMonthDisabled()">
              <span>&#8592;</span>
            </button>
            <div class="month-selector">
              <div class="month">
                <h3>{{ getMonthName(currentMonth) }} {{ currentYear }}</h3>
              </div>
              <div class="month">
                <h3>{{ getMonthName(nextMonth) }} {{ nextMonthYear }}</h3>
              </div>
            </div>
            <button class="nav-button next" (click)="nextMonth()">
              <span>&#8594;</span>
            </button>
          </div>
          
          <div class="calendars">
            <div class="calendar">
              <div class="weekdays">
                <div *ngFor="let day of weekdays" class="weekday">{{ day }}</div>
              </div>
              <div class="days">
                <div *ngFor="let empty of getEmptyDays(currentMonth, currentYear)" class="day empty"></div>
                <div *ngFor="let day of getDaysInMonth(currentMonth, currentYear)" 
                     [ngClass]="getDayClasses(day, currentMonth, currentYear)"
                     (click)="selectDate(day, currentMonth, currentYear)">
                  {{ day }}
                </div>
              </div>
            </div>
            
            <div class="calendar">
              <div class="weekdays">
                <div *ngFor="let day of weekdays" class="weekday">{{ day }}</div>
              </div>
              <div class="days">
                <div *ngFor="let empty of getEmptyDays(nextMonth, nextMonthYear)" class="day empty"></div>
                <div *ngFor="let day of getDaysInMonth(nextMonth, nextMonthYear)" 
                     [ngClass]="getDayClasses(day, nextMonth, nextMonthYear)"
                     (click)="selectDate(day, nextMonth, nextMonthYear)">
                  {{ day }}
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div class="date-picker-footer">
          <button class="clear-dates" (click)="clearAllDates()">Clear dates</button>
          <button class="close-button" (click)="closeCalendar()">Close</button>
        </div>
      </div>
    </div>
  `,
  styles: [
    
  ]
})
export class DatePickerComponent implements OnInit {
  @Input() isOpen = false;
  @Input() initialCheckInDate: Date | null = null;
  @Input() initialCheckOutDate: Date | null = null;
  
  @Output() close = new EventEmitter<void>();
  @Output() dateSelected = new EventEmitter<{checkIn: Date | null, checkOut: Date | null}>();
  
  checkInDate: Date | null = null;
  checkOutDate: Date | null = null;
  currentMonth: number = new Date().getMonth();
  currentYear: number = new Date().getFullYear();
  ThenextMonth: number = (this.currentMonth + 1) % 12;
  nextMonthYear: number = this.currentMonth === 11 ? this.currentYear + 1 : this.currentYear;
  weekdays: string[] = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
  selecting: 'checkIn' | 'checkOut' = 'checkIn';
  
  get nights(): number {
    if (!this.checkInDate || !this.checkOutDate) return 0;
    const diffTime = this.checkOutDate.getTime() - this.checkInDate.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }
  
  ngOnInit(): void {
    if (this.initialCheckInDate) {
      this.checkInDate = new Date(this.initialCheckInDate);
      this.currentMonth = this.checkInDate.getMonth();
      this.currentYear = this.checkInDate.getFullYear();
      this.updateNextMonth();
      this.selecting = 'checkOut';
    }
    
    if (this.initialCheckOutDate) {
      this.checkOutDate = new Date(this.initialCheckOutDate);
    }
  }
  
  closeCalendar(): void {
    this.dateSelected.emit({
      checkIn: this.checkInDate,
      checkOut: this.checkOutDate
    });
    this.close.emit();
  }
  
  closeOnBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('date-picker-overlay')) {
      this.close.emit();
    }
  }
  
  previousMonth(): void {
    if (this.currentMonth === 0) {
      this.currentMonth = 11;
      this.currentYear--;
    } else {
      this.currentMonth--;
    }
    this.updateNextMonth();
  }
  
  nextMonth(): void {
    if (this.currentMonth === 11) {
      this.currentMonth = 0;
      this.currentYear++;
    } else {
      this.currentMonth++;
    }
    this.updateNextMonth();
  }
  
  updateNextMonth(): void {
    this.ThenextMonth = (this.currentMonth + 1) % 12;
    this.nextMonthYear = this.currentMonth === 11 ? this.currentYear + 1 : this.currentYear;
  }
  
  getMonthName(month: number): string {
    return new Date(0, month).toLocaleString('default', { month: 'long' });
  }
  
  getDaysInMonth(month: number, year: number): number[] {
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    return Array.from({ length: daysInMonth }, (_, i) => i + 1);
  }
  
  getEmptyDays(month: number, year: number): number[] {
    const firstDay = new Date(year, month, 1).getDay();
    return Array(firstDay).fill(0);
  }
  
  isPreviousMonthDisabled(): boolean {
    const today = new Date();
    const currentMonthStart = new Date(this.currentYear, this.currentMonth, 1);
    return currentMonthStart <= new Date(today.getFullYear(), today.getMonth(), 1);
  }
  
  isDateDisabled(day: number, month: number, year: number): boolean {
    const date = new Date(year, month, day);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  }
  
  selectDate(day: number, month: number, year: number): void {
    const selectedDate = new Date(year, month, day);
    
    if (this.isDateDisabled(day, month, year)) {
      return;
    }
    
    if (this.selecting === 'checkIn') {
      this.checkInDate = selectedDate;
      this.checkOutDate = null;
      this.selecting = 'checkOut';
    } else {
      if (selectedDate < this.checkInDate!) {
        // If selected checkout is before check-in, swap them
        this.checkOutDate = this.checkInDate;
        this.checkInDate = selectedDate;
      } else {
        this.checkOutDate = selectedDate;
      }
      this.selecting = 'checkIn';
    }
  }
  
  getDayClasses(day: number, month: number, year: number): any {
    const date = new Date(year, month, day);
    const classes = {
      'day': true,
      'disabled': this.isDateDisabled(day, month, year),
      'selected': false,
      'in-range': false,
      'range-start': false,
      'range-end': false
    };
    
    if (this.checkInDate && day === this.checkInDate.getDate() && 
        month === this.checkInDate.getMonth() && 
        year === this.checkInDate.getFullYear()) {
      classes['selected'] = true;
      classes['range-start'] = !!this.checkOutDate;
    }
    
    if (this.checkOutDate && day === this.checkOutDate.getDate() && 
        month === this.checkOutDate.getMonth() && 
        year === this.checkOutDate.getFullYear()) {
      classes['selected'] = true;
      classes['range-end'] = true;
    }
    
    if (this.checkInDate && this.checkOutDate) {
      const currentDate = new Date(year, month, day);
      if (currentDate > this.checkInDate && currentDate < this.checkOutDate) {
        classes['in-range'] = true;
      }
    }
    
    return classes;
  }
  
  clearDate(dateType: 'checkIn' | 'checkOut'): void {
    if (dateType === 'checkIn') {
      this.checkInDate = null;
      this.checkOutDate = null;
      this.selecting = 'checkIn';
    } else {
      this.checkOutDate = null;
      this.selecting = 'checkOut';
    }
  }
  
  clearAllDates(): void {
    this.checkInDate = null;
    this.checkOutDate = null;
    this.selecting = 'checkIn';
  }
  
  formatDate(date: Date | null): string {
    if (!date) return '';
    return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
  }
  
  formatDateRange(): string {
    if (!this.checkInDate) return 'Select dates';
    
    const formatOptions: Intl.DateTimeFormatOptions = { month: 'long', day: 'numeric', year: 'numeric' };
    const checkInFormatted = this.checkInDate.toLocaleDateString('en-US', formatOptions);
    
    if (!this.checkOutDate) return `${checkInFormatted} - Select checkout date`;
    
    const checkOutFormatted = this.checkOutDate.toLocaleDateString('en-US', formatOptions);
    return `${checkInFormatted} - ${checkOutFormatted}`;
  }
}
