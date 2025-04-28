import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CreatePropertyService } from '../../../services/property-crud.service';
import { CommonModule, NgIf, NgForOf, DatePipe, NgClass } from '@angular/common';
import { ProfileService } from '../../../services/profile.service';
import { GoogleMap, GoogleMapsModule } from "@angular/google-maps";
import * as L from 'leaflet';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { MainNavbarComponent } from '../../main-navbar/main-navbar.component';
import { MessageUserButtonComponent } from '../../chatting/components/message-user-button/message-user-button.component';


@Component({
  selector: 'app-property-details',
  standalone: true,
  imports: [CommonModule, NgIf, NgForOf, DatePipe, NgClass, GoogleMapsModule, FormsModule, MainNavbarComponent, MessageUserButtonComponent],
  templateUrl: './property-details.component.html',

  styleUrls: ['./property-details.component.scss']
})
export class PropertyDetailsComponent implements OnInit {
  propertyId: number = 0;
  property: any;
  loading = true;
  error: string | null = null;
  mainImage: string | null = null;
  secondaryImages: string[] = [];
  hostRating: number = 4.5;
  // Add to your component class
  isGuestDropdownOpen = false;
  checkInDate: string = '5/9/2025';
  checkOutDate: string = '5/23/2025';
  guests: number = 1;

  showAll = false;
  isDescriptionModalOpen = false;
  showFullDescription = false;
  maxDescriptionLength = 300; // Characters to show before truncating
  hostProfile: any = null;
  availabilityData: { [date: string]: boolean } = {};
  maxGuests: number = 10; // Maximum allowed guests
  userLat: number | null = null;
  userLng: number | null = null;
  distance: string = "Getting location...";
  isPromoCodeSectionOpen = false;
  promoCode = '';
  promoCodeLoading = false;
  promoCodeApplied = false;
  promoCodeError: string | null = null;
  promotion: any = null;
  guestIsNotHost: boolean = true;
  toastMessage = '';
  bookingId: number = 0;

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
  showToast = false;
  lastPropertyId: number = 0;
  loggedInUser: string | null = '';
  userRole = '';

  constructor(
    private route: ActivatedRoute,
    private propertyService: CreatePropertyService,
    private profileService: ProfileService,
    private router: Router,
    private authService: AuthService

  ) { }







  ngOnInit(): void {
    this.propertyId = +this.route.snapshot.paramMap.get('id')!;
    this.loadPropertyDetails();
    this.checkWishlistStatus();
    this.loggedInUser = this.authService.userId;
    this.userRole = this.profileService.getUserRole();

    this.getUserLocation();
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

  switchToGuestAccount() {
    // You can implement logic here to switch the user's role
    // This might involve a service call or navigation to a login page

    // Example implementation:
    this.authService.logout(); // Log out the current user
    this.router.navigate(['/login']); // Redirect to login page

  }

  togglePromoCodeSection(): void {
    this.isPromoCodeSectionOpen = !this.isPromoCodeSectionOpen;
  }

  applyPromoCode(): void {
    if (!this.promoCode || this.promoCodeLoading || this.promoCodeApplied) {
      return;
    }

    this.promoCodeLoading = true;
    this.promoCodeError = null;

    this.propertyService.getPromoCodeDetails(this.promoCode).subscribe({
      next: (response) => {
        this.promoCodeLoading = false;

        // Check if promotion is valid
        if (!response || !response.isActive) {
          this.promoCodeError = 'This promo code is not valid or has expired';
          return;
        }

        // Check if promotion has reached max usage
        if (response.maxUses > 0 && response.usedCount >= response.maxUses) {
          this.promoCodeError = 'This promo code has reached its maximum usage limit';
          return;
        }

        // Check if promotion has expired
        const now = new Date();
        const endDate = new Date(response.endDate);
        if (endDate < now) {
          this.promoCodeError = 'This promo code has expired';
          return;
        }

        // Apply promotion
        this.promotion = response;
        this.promoCodeApplied = true;
      },
      error: (error) => {
        this.promoCodeLoading = false;
        this.promoCodeError = 'Invalid promo code';
        console.error('Error validating promo code:', error);
      }
    });
  }

  removePromoCode(): void {
    this.promoCode = '';
    this.promoCodeApplied = false;
    this.promoCodeError = null;
    this.promotion = null;
  }

  getDiscountText(): string {
    if (!this.promotion) {
      return '';
    }

    if (this.promotion.discountType === 'fixed') {
      return `${this.formatPrice(this.promotion.amount)} off`;
    } else {
      return `${this.promotion.amount}% off`;
    }
  }

  getDiscountAmount(): number {
    if (!this.promoCodeApplied || !this.promotion) {
      return 0;
    }

    const subtotal = this.getSubtotalPrice();

    if (this.promotion.discountType === 'fixed') {
      return Math.min(subtotal, this.promotion.amount);
    } else {
      // Percentage discount
      return subtotal * (this.promotion.amount / 100);
    }
  }

  getSubtotalPrice(): number {
    if (!this.property) return 0;

    // Calculate nights from check-in to check-out
    const checkIn = new Date(this.checkInDate);
    const checkOut = new Date(this.checkOutDate);
    const nights = Math.round((checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60 * 24));

    return (this.property.pricePerNight + this.property.cleaningFee + this.property.serviceFee) * nights;
  }


  // Update your existing getTotalPrice method
  getTotalPrice(): number {
    const subtotal = this.getSubtotalPrice();
    const discount = this.getDiscountAmount();

    return subtotal - discount;
  }
  reserve(): void {
    // Check if dates are selected
    if (!this.checkInDate || !this.checkOutDate) {
      alert('Please select check-in and check-out dates');
      return;
    }
    const startDate = new Date(this.checkInDate);
    startDate.setHours(12, 0, 0, 0);

    const endDate = new Date(this.checkOutDate);
    endDate.setHours(12, 0, 0, 0);

    // Create booking object
    const bookingData: any = {
      propertyId: this.propertyId,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString()
    };

    // Add promotion ID if a code has been applied
    if (this.promoCodeApplied && this.promotion) {
      bookingData.promotionId = this.promotion.id;
    }
    else {
      bookingData.promotionId = 0
    }

    // Call API to create booking
    this.propertyService.createBooking(bookingData).subscribe({
      next: (response) => {
        this.bookingId = response.id;
        // Navigate to booking confirmation page
        if (this.promoCode && this.promoCodeApplied) {
          this.propertyService.updatePromoCode(this.promoCode).subscribe({
            next: (response) => {
              console.log('Promo code updated successfully:', response);
            },
            error: (error) => {
              console.error('Error updating promo code:', error);
            }
          });
        }
        if (this.property.instantBook) {

          this.showToast = true;
          console.log(this.bookingId)
          this.toastMessage = `🏡 Property added to your Bookings! <a href='/payment/${this.bookingId}'>Now You Can Continue to payment From here</a>`;

          setTimeout(() => {
            this.showToast = false;
          }, 5000);


          //this.router.navigate(['/payment', response.bookingId]);
        }
      },
      error: (error) => {
        console.error('Error creating booking:', error);
        // Display error message
        if (error.error && typeof error.error === 'string') {
          alert(`Booking failed: ${error.error}`);
        } else if (error.error && error.error.message) {
          alert(`Booking failed: ${error.error.message}`);
        } else {
          alert('Failed to create booking. Please try again later.');
        }
      }
    });
  }



  // Update your calculateDistance method:
  calculateDistance(): void {
    if (this.userLat && this.userLng && this.property?.latitude && this.property?.longitude) {
      try {
        const lat1 = this.userLat;
        const lon1 = this.userLng;
        const lat2 = parseFloat(this.property.latitude);
        const lon2 = parseFloat(this.property.longitude);

        if (isNaN(lat2) || isNaN(lon2)) {
          throw new Error('Invalid property coordinates');
        }

        // Haversine formula
        const R = 6371; // Earth radius in km
        const dLat = this.toRad(lat2 - lat1);
        const dLon = this.toRad(lon2 - lon1);
        const a =
          Math.sin(dLat / 2) * Math.sin(dLat / 2) +
          Math.cos(this.toRad(lat1)) * Math.cos(this.toRad(lat2)) *
          Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        const distance = R * c;

        // Update the distance text
        this.distance = distance < 1
          ? `${Math.round(distance * 1000)} meters away`
          : `${distance.toFixed(1)} km away`;
      } catch (error) {
        console.error('Distance calculation error:', error);
        this.distance = "Distance unavailable";
      }
    } else {
      this.distance = "Getting location...";
    }
  }

  // Update your getUserLocation method:
  getUserLocation(): void {
    console.log('Getting user location...');
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          console.log('Got user position:', position);
          this.userLat = position.coords.latitude;
          this.userLng = position.coords.longitude;
          this.distance = "Calculating distance...";

          // Calculate distance if property is loaded, otherwise it will happen when property loads
          if (this.property) {
            this.calculateDistance();
          }
        },
        (error) => {
          console.error('Error getting user location:', error);
          this.distance = `Location access error: ${error.message}`;
        },
        {
          timeout: 10000,
          enableHighAccuracy: true,
          maximumAge: 0 // Don't use cached position
        }
      );
    } else {
      console.error('Geolocation not supported');
      this.distance = "Geolocation not supported";
    }
  }


  // In your loadPropertyDetails method, add this after setting this.property = data:


  toRad(value: number): number {
    return value * Math.PI / 180;
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
      //add a success toast 

      this.lastPropertyId = propertyId;
      this.showToast = true;
      this.toastMessage = "🏡 Property added to your wishlist! <a href='/wishlist'>Click here to view</a>";

      setTimeout(() => {
        this.showToast = false;
      }, 3000);

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

    const distribution: { [key: number]: number } = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };

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
  scrollDown() {
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
        this.loading = false;
        this.maxGuests = this.property.maxGuests; // Set your maximum allowed guests
        if (this.userLat && this.userLng) {
          this.calculateDistance();
        } else {
          // If user location isn't available yet, try to get it
          this.getUserLocation();
        }

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


        this.loadAvailabilityDate(this.propertyId)
      },
      error: (err) => {
        this.error = 'Failed to load property details';
        this.loading = false;
        console.error(err);
      }
    });
  }

  getHostTotalReviewsAndRating(hostId: number): void {
    this.profileService.getUserReviews(hostId.toString()).subscribe({
      next: (data) => {

        this.hostRating = data.reduce((acc: number, review: any) => acc + review.rating, 0) / data.length || 0;
        this.hostProfile.totalReviews = data.length || 0;
      },
      error: (err) => {
        console.error('Failed to load host reviews and rating:', err);
      }
    });
  }
  loadAvailabilityDate(propertyId: number): void {
    this.propertyService.getPropertyAvailability(propertyId).subscribe({
      next: (data: any[]) => {
        // Convert array of availability objects to a map of date -> isAvailable
        this.availabilityData = {};
        data.forEach(item => {
          // Convert date string to YYYY-MM-DD format for consistent comparison
          const dateStr = new Date(item.date).toISOString().split('T')[0];
          this.availabilityData[dateStr] = item.isAvailable;
        });

        // Initialize calendar with valid dates after loading availability data
        this.initializeCalendarAndDates();
      },
      error: (err) => {
        console.error('Failed to load availability dates:', err);
      }
    });
  }





  loadHostProfile(hostId: number): void {
    this.profileService.getHostProfile(hostId.toString()).subscribe({
      next: (data) => {
        this.hostProfile = data;

        this.getHostTotalReviewsAndRating(this.hostProfile.hostId);
      },
      error: (err) => {
        console.error('Failed to load host profile:', err);
      }
    });

  }














  isSuperhost(): boolean {
    // Logic to determine if host is a superhost
    // This could be based on rating, review count, etc.
    return this.hostProfile && this.hostRating >= 4.5;
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
    //Check if the URL is already absolute
    if (imageUrl.startsWith('http')) {
      return imageUrl;

    }

    //Otherwise, prepend the base URL
    return `https://localhost:7228${imageUrl}`;
  }

  reserve2(): void {
    // Check if dates are selected
    if (!this.checkInDate || !this.checkOutDate) {
      alert('Please select check-in and check-out dates');
      return;
    }
    const startDate = new Date(this.checkInDate);
    startDate.setHours(12, 0, 0, 0);

    const endDate = new Date(this.checkOutDate);
    endDate.setHours(12, 0, 0, 0);

    // Create booking object
    const bookingData = {
      propertyId: this.propertyId,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      promotionId: 0 // Default value, you can modify this if you implement promotions
    };

    // Call API to create booking
    this.propertyService.createBooking(bookingData).subscribe({
      next: (response) => {
        // Navigate to booking confirmation page
        if (this.promoCode && this.promoCodeApplied) {
          this.propertyService.updatePromoCode(this.promoCode)
        }
        this.router.navigate(['/bookings', response.id]);
      },
      error: (error) => {
        console.error('Error creating booking:', error);
        // Display error message
        if (error.error && typeof error.error === 'string') {
          alert(`Booking failed: ${error.error}`);
        } else if (error.error && error.error.message) {
          alert(`Booking failed: ${error.error.message}`);
        } else {
          alert('Failed to create booking. Please try again later.');
        }
      }
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: this.property?.currency || 'USD'
    }).format(price);
  }

  getTotalPrice2(): number {
    if (!this.property) return 0;

    // Calculate nights from check-in to check-out
    const checkIn = new Date(this.checkInDate);
    const checkOut = new Date(this.checkOutDate);
    const nights = Math.round((checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60 * 24));

    return (this.property.pricePerNight + this.property.cleaningFee + this.property.serviceFee) * nights;
  }

  // Get reviewer's initial for the avatar placeholder
  getReviewerInitial(reviewer: string): string {
    return reviewer ? reviewer.charAt(0).toUpperCase() : 'G';
  }

  // Toggle expanded comment view
  toggleExpandedComment(reviewId: number): void {
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

  closeCalendar(forceClear: boolean = false): void {
    // Check if only one date is selected (incomplete range)
    if ((this.selectedDates.start && !this.selectedDates.end)) {
      // If only check-in is selected but no check-out, reset dates
      this.clearDates();
    }

    // Close the calendar
    this.showCalendar = false;
    this.currentDateSelection = null;
    document.body.style.overflow = '';
  }

  clearDates(): void {
    // Find the first available date from today
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    let firstAvailableDate = new Date(today);
    while (this.isDateDisabled(firstAvailableDate)) {
      firstAvailableDate.setDate(firstAvailableDate.getDate() + 1);
    }

    // Set start date to first available date
    this.selectedDates.start = firstAvailableDate;
    this.checkInDate = this.formatDateForDisplay(firstAvailableDate);

    // Find the next available date after check-in
    let nextAvailableDate = new Date(firstAvailableDate);
    nextAvailableDate.setDate(nextAvailableDate.getDate() + 1);

    while (this.isDateDisabled(nextAvailableDate)) {
      nextAvailableDate.setDate(nextAvailableDate.getDate() + 1);
    }

    // Set end date to the next available date
    this.selectedDates.end = nextAvailableDate;
    this.checkOutDate = this.formatDateForDisplay(nextAvailableDate);

    // Update the calendar
    this.generateCalendarMonths();
  }

  initializeCalendarAndDates(): void {
    // Only run this if we have availability data
    if (Object.keys(this.availabilityData).length === 0) {
      return;
    }

    // Check if current dates are valid
    const checkInDate = new Date(this.checkInDate);
    const checkOutDate = new Date(this.checkOutDate);

    let checkInValid = !this.isDateDisabled(checkInDate);
    let checkOutValid = !this.isDateDisabled(checkOutDate);

    // Check if range is valid (no unavailable dates in between)
    let rangeValid = true;
    if (checkInValid && checkOutValid) {
      for (let d = new Date(checkInDate); d <= checkOutDate; d.setDate(d.getDate() + 1)) {
        if (this.isDateDisabled(d)) {
          rangeValid = false;
          break;
        }
      }
    }

    // If current selection isn't valid, clear and find first valid dates
    if (!checkInValid || !checkOutValid || !rangeValid) {
      this.clearDates();
    } else {
      // Set selected dates from current check-in/check-out
      this.selectedDates.start = checkInDate;
      this.selectedDates.end = checkOutDate;
      this.generateCalendarMonths();
    }
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

      this.currentDateSelection = 'checkOut';
    } else if (this.currentDateSelection === 'checkOut') {
      // If selecting check-out date
      if (date >= this.selectedDates.start!) {
        // Check if any date in the range is unavailable
        let rangeHasUnavailableDates = false;
        const startDate = new Date(this.selectedDates.start!);
        const endDate = new Date(date);

        // Loop through each day in the range
        for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
          if (this.isDateDisabled(d)) {
            rangeHasUnavailableDates = true;
            break;
          }
        }

        if (rangeHasUnavailableDates) {
          // Don't allow selection of a range with unavailable dates
          return;
        }

        this.selectedDates.end = date;
        this.checkOutDate = this.formatDateForDisplay(date);
        // Close calendar after selecting both dates
        this.closeCalendar(false); // Pass false to not force clearing
      } else {
        // If selecting a date before check-in date, make it the new check-in date
        this.selectedDates.start = date;
        this.checkInDate = this.formatDateForDisplay(date);
        this.selectedDates.end = null;
        this.checkOutDate = '';
        this.currentDateSelection = 'checkOut';
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
    const isSelected =
      (this.selectedDates.start && this.isSameDay(date, this.selectedDates.start)) ||
      (this.selectedDates.end && this.isSameDay(date, this.selectedDates.end));

    const isInRange =
      this.selectedDates.start &&
      this.selectedDates.end &&
      date > this.selectedDates.start &&
      date < this.selectedDates.end;

    // Use our updated isDateDisabled method
    const isDisabled = this.isDateDisabled(date);

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
    // Check if date is in the past
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (date < today) {
      return true;
    }

    // Check if date is outside the range of availability data

    const availabilityDates = Object.keys(this.availabilityData);
    if (availabilityDates.length > 0) {
      const lastAvailableDateStr = availabilityDates[availabilityDates.length - 1];
      const lastAvailableDate = new Date(lastAvailableDateStr);
      lastAvailableDate.setHours(0, 0, 0, 0);

      if (date > lastAvailableDate) {
        return true; // Disable dates beyond the last available date
      }
    }
    // Check availability data
    const dateStr = date.toISOString().split('T')[0];

    // If we have availability data for this date and it's explicitly marked as not available
    return this.availabilityData[dateStr] === false;
  }
  getDaysInRange(): number {
    if (!this.selectedDates.start || !this.selectedDates.end) {
      return 0;
    }

    const diffTime = Math.abs(this.selectedDates.end.getTime() - this.selectedDates.start.getTime());
    return Math.round(diffTime / (1000 * 60 * 60 * 24));
  }





  // Add to your component class

  toggleGuestDropdown(event: Event): void {
    event.stopPropagation(); // Prevent event bubbling
    this.isGuestDropdownOpen = !this.isGuestDropdownOpen;

    // Add click outside listener when dropdown is open
    if (this.isGuestDropdownOpen) {
      setTimeout(() => {
        document.addEventListener('click', this.onDocumentClick);
      });
    }
  }

  // Use arrow function to maintain 'this' context
  onDocumentClick = (event: MouseEvent): void => {
    const dropdownEl = document.querySelector('.guests-dropdown');
    if (dropdownEl && !dropdownEl.contains(event.target as Node)) {
      this.closeGuestDropdown();
    }
  }

  closeGuestDropdown(): void {
    this.isGuestDropdownOpen = false;
    document.removeEventListener('click', this.onDocumentClick);
  }

  updateGuestCount(change: number): void {
    const newCount = this.guests + change;
    if (newCount >= 1 && newCount <= this.maxGuests) {
      this.guests = newCount;
    }
  }


  // Clean up on component destruction
  ngOnDestroy(): void {
    document.removeEventListener('click', this.onDocumentClick);
  }




}