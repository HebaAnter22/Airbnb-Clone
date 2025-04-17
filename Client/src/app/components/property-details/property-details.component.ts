import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CreatePropertyService } from '../../services/property-crud.service';
import { CommonModule, NgIf, NgForOf, DatePipe, NgClass } from '@angular/common';
import { ProfileService } from '../../services/profile.service';
import { GoogleMap, GoogleMapsModule } from "@angular/google-maps";
import * as L from 'leaflet';

@Component({
  selector: 'app-property-details',
  standalone: true,
  imports: [CommonModule, NgIf, NgForOf, DatePipe, NgClass,GoogleMapsModule],
  templateUrl: './property-details.component.html',
  styleUrls: ['./property-details.component.scss']
})
export class PropertyDetailsComponent implements OnInit  {
  propertyId: number = 0;
  property: any;
  loading = true;
  error: string | null = null;
  mainImage: string | null = null;
  secondaryImages: string[] = [];
  checkInDate: string = '5/9/2025';
  checkOutDate: string = '5/23/2025';
  guests: number = 1;
  showAll = false;
  isDescriptionModalOpen = false;
  showFullDescription = false;
  maxDescriptionLength = 300; // Characters to show before truncating
  hostProfile: any = null;

//   center: google.maps.LatLngLiteral = {lat: 22, lng: 72}; // Default values
// markerLatLong: google.maps.LatLngLiteral[] = [{lat: 22, lng: 72}]; // Default values
// mapOptions: google.maps.MapOptions = {
//   disableDefaultUI: false,
//   fullscreenControl: false,
//   zoomControl: true,
//   scrollwheel: false,
//   streetViewControl: false,
//   mapTypeControl: false,
//   zoom: 15,
//   styles: [
//     {
//       featureType: "poi",
//       elementType: "labels",
//       stylers: [{ visibility: "off" }]
//     }
//   ]
// };

// markerOptions: google.maps.MarkerOptions = {
//   icon: {
//     url: '/assets/images/home-marker.png', // You'll need to create this icon
//     // Use simple object instead of google.maps.Size
//     scaledSize: { width: 60, height: 60 } as google.maps.Size
//   }
//   // Remove animation property for now
// };






















private map: L.Map | null = null;
  private marker: L.Marker | null = null;
  // Default coordinates (can be the same as your Google Maps defaults)
  private defaultLat: number = 22;
  private defaultLng: number = 72;













  isHostBioModalOpen = false;

coHosts = [
  { name: 'Anas', image: '/assets/co-host-placeholder.jpg' } // Replace with actual co-host data
];
  // Calendar related properties
  showCalendar = false;
  currentDateSelection: 'checkIn' | 'checkOut' | null = null;
  currentMonth: Date = new Date(2025, 4, 1); // May 2025
  selectedDates: { start: Date | null, end: Date | null } = { start: null, end: null };
  months: Array<{ month: Date, days: Array<{ date: Date, isSelected: boolean, isInRange: boolean, isDisabled: boolean }> }> = [];
  inWishList = false; // Track if the property is in the wishlist
  constructor(
    private route: ActivatedRoute,
    private propertyService: CreatePropertyService,
    private profileService:ProfileService,
    private router :Router
  ) { }
  





  ngOnInit(): void {
    this.propertyId = +this.route.snapshot.paramMap.get('id')!;
    this.loadPropertyDetails();
    this.checkWishlistStatus();
    // Initialize date selection
    const checkIn = new Date(this.checkInDate);
    const checkOut = new Date(this.checkOutDate);
    this.selectedDates = {
      start: checkIn,
      end: checkOut
    };
    
    // Initialize calendar
    this.generateCalendarMonths();
  }


  checkWishlistStatus(): void {
    this.profileService.isPropertyInWishlist(this.propertyId).subscribe({
      next: (isInWishlist) => {
        this.inWishList = isInWishlist;
      },
      error: (err) => {
        console.error('Failed to check wishlist status:', err);
        this.inWishList = false;
      }
    });
  }
  
  toggleWishlist(propertyId: number): void {
    if (this.inWishList) {
      this.removeFromWishlist(propertyId);
    } else {
      this.addToWishlist(propertyId);
    }
  }






























  initializeMap(): void {
    // Make sure we have a valid property with coordinates
    if (!this.map && this.property) {
      // Get coordinates from property or use defaults
      const lat = this.property.latitude ? parseFloat(this.property.latitude) : this.defaultLat;
      const lng = this.property.longitude ? parseFloat(this.property.longitude) : this.defaultLng;
      
      // Create map
      this.map = L.map('map').setView([lat, lng], 15);
      
      // Add tile layer (OpenStreetMap)
      const tileLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
      }).addTo(this.map);
      tileLayer.on('tileerror', (error) => {
        console.error('Tile loading error:', error);
      });
      // Add marker
      const homeIcon = L.icon({
        iconUrl: '/assets/images/home-marker.png',
        iconSize: [60, 60],
        iconAnchor: [30, 30]
      });
      
      this.marker = L.marker([lat, lng], { icon: homeIcon }).addTo(this.map);
      
      // Add a circle around the marker to match the pink area in your image
      L.circle([lat, lng], {
        color: 'rgba(255, 0, 0, 0)',
        fillColor: '#ff595e',
        fillOpacity: 0.2,
        radius: 300
      }).addTo(this.map);
    }
  }

  // Update map when property coordinates change
  updateMapLocation(): void {
    if (this.property && this.property.latitude && this.property.longitude) {
      const lat = parseFloat(this.property.latitude);
      const lng = parseFloat(this.property.longitude);
      
      if (!isNaN(lat) && !isNaN(lng)) {
        // If map already exists, update its view and marker position
        if (this.map) {
          this.map.setView([lat, lng], 15);
          if (this.marker) {
            this.marker.setLatLng([lat, lng]);
          }
        } else {
          // Otherwise initialize the map
          setTimeout(() => this.initializeMap(), 300);

          
        }
      }
    }
  }
















  addToWishlist(propertyId: number): void {
    this.profileService?.addOrRemoveToFavourites(propertyId)?.subscribe({
      next: () => {
        this.inWishList = true;
      },
      error: (err) => console.error('Error adding to wishlist:', err)
    });
  }
  
  removeFromWishlist(propertyId: number): void {
    this.profileService?.addOrRemoveToFavourites(propertyId)?.subscribe({
      next: () => {
        this.inWishList = false;
      },
      error: (err) => console.error('Error removing from wishlist:', err)
    });
  }







  isReviewsModalOpen = false;

  // Method to open the reviews modal
  openReviewsModal() {
    this.isReviewsModalOpen = true;
    document.body.style.overflow = 'hidden'; // Prevent scrolling when modal is open
  }
  
  // Method to close the reviews modal
  closeReviewsModal(event?: MouseEvent) {
    if (!event || (event.target as Element).classList.contains('reviews-modal')) {
      this.isReviewsModalOpen = false;
      document.body.style.overflow = '';
    }
  }
  
  // Method to get rating distribution
  getRatingDistribution(): { [key: number]: number } {
    if (!this.property?.reviews || this.property.reviews.length === 0) {
      return {};
    }
    
    const distribution: { [key: number]: number } = {5: 0, 4: 0, 3: 0, 2: 0, 1: 0};
    
    this.property.reviews.forEach((review: any) => {
      if (review.rating >= 1 && review.rating <= 5) {
        distribution[review.rating]++;
      }
    });
    
    return distribution;
  }
  
  goToHostPage() {

    this.router.navigate(['/profile', this.property.hostId]);
  }
  scrollDown(){
    //scroll to the end of the page
    window.scrollTo(0, document.body.scrollHeight);
  }
  // Calculate percentage for rating bar
  getRatingPercentage(rating: number): number {
    if (!this.property?.reviews || this.property.reviews.length === 0) {
      return 0;
    }
    
    const distribution = this.getRatingDistribution();
    return (distribution[rating] / this.property.reviews.length) * 100;
  }








  isModalOpen = false;

  openAmenitiesModal() {
    this.isModalOpen = true;
    document.body.style.overflow = 'hidden'; // Prevent scrolling when modal is open
  }

  closeModal(event?: MouseEvent) {
    // Close only if clicking on backdrop or close button
    if (!event || (event.target as Element).classList.contains('amenities-modal')) {
      this.isModalOpen = false;
      document.body.style.overflow = '';
    }
  }

  shouldShowReadMore(): boolean {
    return this.property?.description?.length > this.maxDescriptionLength;
  }

  openDescriptionModal() {
    this.isDescriptionModalOpen = true;
    document.body.style.overflow = 'hidden';
  }

  closeDescriptionModal(event?: MouseEvent) {
    if (!event || (event.target as Element).classList.contains('description-modal')) {
      this.isDescriptionModalOpen = false;
      document.body.style.overflow = '';
    }
  }


  showAllPhotos(): void {
    // Navigate to the gallery route with the property ID as a parameter
    this.router.navigate(['/property', this.propertyId, 'gallery']);
  }


  // Add this to your loadPropertyDetails method, right after setting this.property = data
  // updateMapLocation(): void {
  //   if (this.property && this.property.latitude && this.property.longitude) {
  //     const lat = parseFloat(this.property.latitude);
  //     const lng = parseFloat(this.property.longitude);
      
  //     if (!isNaN(lat) && !isNaN(lng)) {
  //       this.center = { lat, lng };
  //       this.markerLatLong = [{ lat, lng }];
  //     }
  //   }
  // }
  loadPropertyDetails(): void {
    this.propertyService.getPropertyById(this.propertyId).subscribe({
      next: (data) => {
        this.property = data;
        console.log('Property details:', this.property);
        this.loading = false;

     
             this.updateMapLocation();

        // Organize images
        if (this.property.images && this.property.images.length > 0) {
          // Find primary image or use first one
          const primaryImage = this.property.images.find((img: any) => img.isPrimary);
          this.mainImage = primaryImage ?
            this.getFullImageUrl(primaryImage.imageUrl) :
            this.getFullImageUrl(this.property.images[0].imageUrl);
         
          // Get remaining images
          this.secondaryImages = this.property.images
            .filter((img: any) => img !== primaryImage)
            .map((img: any) => this.getFullImageUrl(img.imageUrl));
        }
        if (this.property && this.property.hostId) {
          this.loadHostProfile(this.property.hostId);
        }
      },
      error: (err) => {
        this.error = 'Failed to load property details';
        this.loading = false;
        console.error(err);
      }
    });
  }





  loadHostProfile(hostId: number): void {
    this.profileService.getHostProfile(hostId.toString()).subscribe({
      next: (data) => {
        this.hostProfile = data;
        console.log('Host profile:', this.hostProfile);
      },
      error: (err) => {
        console.error('Failed to load host profile:', err);
      }
    });
  }














  isSuperhost(): boolean {
    // Logic to determine if host is a superhost
    // This could be based on rating, review count, etc.
    return this.hostProfile && this.hostProfile.rating >= 4.5 && this.hostProfile.totalReviews >= 3;
  }
  

  getYearsHosting(): number {
    // Calculate years from hostProfile.startDate
    if (!this.hostProfile || !this.hostProfile.startDate) return 5; // Default value
    
    const startDate = new Date(this.hostProfile.startDate);
    const now = new Date();
    return Math.floor((now.getTime() - startDate.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
  }
  
  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }
  
  formatLanguages(languages: string): string {
    if (!languages) return '';
    
    // Split the string by commas, spaces, or other separators
    const languageArray = languages.split(/[,\s]+/).filter(lang => lang.trim() !== '');
    
    // Capitalize first letter of each language
    const formattedLanguages = languageArray.map(lang => 
      lang.charAt(0).toUpperCase() + lang.slice(1).toLowerCase()
    );
    
    // Join with 'and' for the last item if there are multiple languages
    if (formattedLanguages.length > 1) {
      const lastLanguage = formattedLanguages.pop();
      return `${formattedLanguages.join(', ')} and ${lastLanguage}`;
    }
    
    return formattedLanguages.join(', ');
  }
  
  truncateBio(text: string, maxLength: number): string {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    
    // Find the last space before maxLength
    const lastSpace = text.lastIndexOf(' ', maxLength);
    if (lastSpace === -1) return text.substring(0, maxLength) + '...';
    
    return text.substring(0, lastSpace) + '...';
  }
  
  hasCoHosts(): boolean {
    return this.coHosts && this.coHosts.length > 0;
  }
  
  getCoHosts(): any[] {
    return this.coHosts;
  }
  
  openHostBioModal(): void {
    this.isHostBioModalOpen = true;
    document.body.style.overflow = 'hidden';
  }
  
  closeHostBioModal(event?: MouseEvent): void {
    if (!event || (event.target as Element).classList.contains('host-bio-modal')) {
      this.isHostBioModalOpen = false;
      document.body.style.overflow = '';
    }
  }




  
  
  getFullImageUrl(imageUrl: string): string {
    // Check if the URL is already absolute
    if (imageUrl.startsWith('http')) {
      return imageUrl;
    }
   
    // Otherwise, prepend the base URL
    return `https://localhost:7228${imageUrl}`;
  }
 
  reserve(): void {
    // Implement reservation logic
    console.log('Reservation requested');
    // You could navigate to a checkout page or open a modal
  }
 
  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: this.property?.currency || 'USD'
    }).format(price);
  }
 
  getTotalPrice(): number {
    if (!this.property) return 0;
   
    // Calculate nights from check-in to check-out
    const checkIn = new Date(this.checkInDate);
    const checkOut = new Date(this.checkOutDate);
    const nights = Math.round((checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60 * 24));
   
    return this.property.pricePerNight * nights;
  }
  
  // Get reviewer's initial for the avatar placeholder
  getReviewerInitial(reviewer: string): string {
    return reviewer ? reviewer.charAt(0).toUpperCase() : 'G';
  }
  
  // Toggle expanded comment view
  toggleExpandedComment(reviewId: number): void {
    console.log(`Toggling expanded view for review ${reviewId}`);
    // Implement the logic to expand/collapse comments
  }
  
  // Calendar related methods
  openCalendar(type: 'checkIn' | 'checkOut'): void {
    this.currentDateSelection = type;
    this.showCalendar = true;
    // Set current month to the month of the selected date
    const date = type === 'checkIn' ? new Date(this.checkInDate) : new Date(this.checkOutDate);
    this.currentMonth = new Date(date.getFullYear(), date.getMonth(), 1);
    this.generateCalendarMonths();
    document.body.style.overflow = 'hidden';
  }
  
  closeCalendar(): void {
    this.showCalendar = false;
    this.currentDateSelection = null;
    document.body.style.overflow = '';
  }
  
  clearDates(): void {
  // Set start date to today
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  this.selectedDates.start = today;
  this.checkInDate = this.formatDateForDisplay(today);
  
  // Set end date to tomorrow
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);
  this.selectedDates.end = tomorrow;
  this.checkOutDate = this.formatDateForDisplay(tomorrow);
  
  // Update the calendar
  this.generateCalendarMonths();
}
  
  selectDate(date: Date): void {
    if (this.isDateDisabled(date)) {
      return;
    }
    
    if (this.currentDateSelection === 'checkIn' || !this.selectedDates.start) {
      // If selecting check-in date or no start date is selected yet
      this.selectedDates.start = date;
      this.checkInDate = this.formatDateForDisplay(date);
      
      // If end date is before start date, clear end date
      if (this.selectedDates.end && date > this.selectedDates.end) {
        this.selectedDates.end = null;
        this.checkOutDate = '';
      }
      
      // If selecting check-in and already have an end date, move to select checkout
      if (this.currentDateSelection === 'checkIn' && this.selectedDates.end) {
        this.currentDateSelection = 'checkOut';
      } else {
        this.currentDateSelection = 'checkOut';
      }
    } else if (this.currentDateSelection === 'checkOut') {
      // If selecting check-out date
      if (date < this.selectedDates.start!) {
        // If selected date is before check-in, update check-in and set checkout to null
        this.selectedDates.end = this.selectedDates.start;
        this.checkOutDate = this.formatDateForDisplay(this.selectedDates.end);
        this.selectedDates.start = date;
        this.checkInDate = this.formatDateForDisplay(date);
      } else {
        this.selectedDates.end = date;
        this.checkOutDate = this.formatDateForDisplay(date);
        // Close calendar after selecting both dates
        this.closeCalendar();
      }
    }
    
    this.generateCalendarMonths();
  }
  
  navigateMonth(direction: 'prev' | 'next'): void {
    const newMonth = new Date(this.currentMonth);
    if (direction === 'prev') {
      newMonth.setMonth(newMonth.getMonth() - 1);
    } else {
      newMonth.setMonth(newMonth.getMonth() + 1);
    }
    this.currentMonth = newMonth;
    this.generateCalendarMonths();
  }
  
  generateCalendarMonths(): void {
    this.months = [];
    
    // Generate current month and next month
    for (let i = 0; i < 2; i++) {
      const monthDate = new Date(this.currentMonth);
      monthDate.setMonth(monthDate.getMonth() + i);
      
      const month = {
        month: monthDate,
        days: this.generateDaysForMonth(monthDate)
      };
      
      this.months.push(month);
    }
  }
  
  generateDaysForMonth(monthDate: Date): Array<{ date: Date, isSelected: boolean, isInRange: boolean, isDisabled: boolean }> {
    const days = [];
    const year = monthDate.getFullYear();
    const month = monthDate.getMonth();
    
    // Get the first day of the month
    const firstDay = new Date(year, month, 1);
    
    // Get the last day of the month
    const lastDay = new Date(year, month + 1, 0);
    
    // Get the day of the week for the first day (0 = Sunday, 6 = Saturday)
    const firstDayOfWeek = firstDay.getDay();
    
    // Calculate the number of days from the previous month to show
    const daysFromPrevMonth = firstDayOfWeek;
    
    // Generate days for the previous month (if any)
    for (let i = 0; i < daysFromPrevMonth; i++) {
      const date = new Date(year, month, -daysFromPrevMonth + i + 1);
      days.push(this.createDayObject(date));
    }
    
    // Generate days for the current month
    for (let i = 1; i <= lastDay.getDate(); i++) {
      const date = new Date(year, month, i);
      days.push(this.createDayObject(date));
    }
    
    // Fill out the remaining days of the week for the last row if needed
    const remainingDays = 7 - (days.length % 7);
    if (remainingDays < 7) {
      for (let i = 1; i <= remainingDays; i++) {
        const date = new Date(year, month + 1, i);
        days.push(this.createDayObject(date));
      }
    }
    
    return days;
  }
  
  createDayObject(date: Date): { date: Date, isSelected: boolean, isInRange: boolean, isDisabled: boolean } {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const isSelected = 
      (this.selectedDates.start && this.isSameDay(date, this.selectedDates.start)) || 
      (this.selectedDates.end && this.isSameDay(date, this.selectedDates.end));
    
    const isInRange = 
      this.selectedDates.start && 
      this.selectedDates.end && 
      date > this.selectedDates.start && 
      date < this.selectedDates.end;
    
    // Disable dates in the past
    const isDisabled = date < today;
    
    return {
      date,
      isSelected: isSelected ?? false,
      isInRange: isInRange ?? false,
      isDisabled
    };
  }
  
  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getDate() === date2.getDate() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getFullYear() === date2.getFullYear();
  }
  
  formatDateForDisplay(date: Date): string {
    // Format date as M/D/YYYY (e.g., 5/9/2025)
    return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
  }
  
  getMonthName(date: Date): string {
    return date.toLocaleString('default', { month: 'long' });
  }
  
  getYearFromDate(date: Date): number {
    return date.getFullYear();
  }
  
  isDateDisabled(date: Date): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today;
  }
  
  getDaysInRange(): number {
    if (!this.selectedDates.start || !this.selectedDates.end) {
      return 0;
    }
    
    const diffTime = Math.abs(this.selectedDates.end.getTime() - this.selectedDates.start.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }
}