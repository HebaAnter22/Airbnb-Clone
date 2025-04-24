/// <reference types="@types/google.maps" />
import { Component, ElementRef, Input, OnInit, ViewChild, Output, EventEmitter, NgZone, OnDestroy, AfterViewInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';

@Component({
  selector: 'app-location-map',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="map-container">
      <div #mapContainer class="map" [class.error]="mapError"></div>
      <input #searchInput class="map-search-input" 
             placeholder="Search for a location" 
             type="text"
             (keyup.enter)="searchLocation(searchInput.value)">
      <button class="location-button" (click)="getCurrentLocation()" title="Get my location">
        <span class="material-icons">my_location</span>
      </button>
      <div *ngIf="mapError" class="error-message">{{ mapError }}</div>
      <div *ngIf="selectedAddress" class="selected-address">
        Selected location: {{ selectedAddress }}
      </div>
    </div>
  `,
  styles: [`
    .map-container {
      position: relative;
      height: 300px;
      width: 100%;
      max-width: 800px;
      margin: 0 auto;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .map {
      height: 100%;
      width: 100%;
      z-index: 1;
    }
    .map.error {
      background-color: #f8d7da;
    }
    .error-message {
      color: #721c24;
      background-color: #f8d7da;
      padding: 8px;
      margin-top: 8px;
      border-radius: 4px;
      font-size: 14px;
    }
    .selected-address {
      margin-top: 8px;
      padding: 8px;
      background-color: #e9ecef;
      border-radius: 4px;
      font-size: 14px;
    }
    .map-search-input {
      position: absolute;
      top: 10px;
      left: 50%;
      transform: translateX(-50%);
      width: 280px;
      padding: 8px;
      border: 1px solid #ccc;
      border-radius: 4px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      z-index: 2;
      font-size: 14px;
    }
    .location-button {
      position: absolute;
      top: 10px;
      right: 10px;
      z-index: 2;
      background: white;
      border: 1px solid #ccc;
      border-radius: 4px;
      padding: 6px;
      cursor: pointer;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .location-button:hover {
      background: #f8f9fa;
    }
    .material-icons {
      font-size: 18px;
      color: #666;
    }
  `]
})
export class LocationMapComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('mapContainer') mapContainer!: ElementRef;
  @ViewChild('searchInput') searchInput!: ElementRef;
  @Input() latitude: number = 0;
  @Input() longitude: number = 0;
  @Output() locationSelected = new EventEmitter<{
    address: string;
    city: string;
    country: string;
    postalCode: string;
    latitude: number;
    longitude: number;
  }>();

  private map!: L.Map;
  private marker!: L.Marker;
  mapError: string | null = null;
  selectedAddress: string | null = null;
  private lastKnownLocation: any = null;

  constructor(
    private snackBar: MatSnackBar,
    private ngZone: NgZone
  ) {}

  ngOnInit(): void {
    // Any initialization that doesn't require view elements
  }

  ngAfterViewInit(): void {
    // Initialize map after view is ready
    setTimeout(() => {
      this.initializeMap();
    });
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  private initializeMap(): void {
    if (!this.mapContainer) {
      console.error('Map container not available');
      return;
    }

    try {
      // Use saved location or default
      const defaultLat = this.latitude || this.lastKnownLocation?.latitude || 31.2001;
      const defaultLng = this.longitude || this.lastKnownLocation?.longitude || 29.9187;

      // Initialize the map
      this.map = L.map(this.mapContainer.nativeElement).setView([defaultLat, defaultLng], 13);

      // Add OpenStreetMap tiles
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
      }).addTo(this.map);

      // Add a draggable marker
    //   this.marker = L.marker([defaultLat, defaultLng], {
    //     draggable: true
    //   },
    
      
    // ).addTo(this.map);
      const homeIcon = L.icon({
            iconUrl: '/assets/images/home-marker.png',
            iconSize: [60, 60],
            iconAnchor: [30, 30]
          });
      this.marker = L.marker([defaultLat, defaultLng],{
        icon: homeIcon
      }
    ).addTo(this.map);

      // Handle marker drag events
      this.marker.on('dragend', () => {
        const position = this.marker.getLatLng();
        this.reverseGeocode(position.lat, position.lng);
      });

      // Get user's current location only if we don't have a saved location
      if (!this.lastKnownLocation && !this.latitude && !this.longitude) {
        this.getCurrentLocation();
      } else if (this.lastKnownLocation) {
        // Restore last known location
        this.selectedAddress = this.lastKnownLocation.address;
        this.locationSelected.emit(this.lastKnownLocation);
      }

      // Trigger a resize event after map initialization
      setTimeout(() => {
        this.map.invalidateSize();
      }, 100);

    } catch (error) {
      console.error('Error initializing map:', error);
      this.mapError = 'Error initializing map';
      this.snackBar.open('Error initializing map', 'Close', { duration: 5000 });
    }
  }

  getCurrentLocation(): void {
    if (!navigator.geolocation) {
      this.snackBar.open('Geolocation is not supported by your browser', 'Close', { duration: 5000 });
      return;
    }

    this.snackBar.open('Getting your location...', 'Close', { duration: 2000 });

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.ngZone.run(() => {
          const pos = {
            lat: position.coords.latitude,
            lng: position.coords.longitude
          };
          
          if (this.map && this.marker) {
            this.map.setView([pos.lat, pos.lng], 15);
            this.marker.setLatLng([pos.lat, pos.lng]);
            this.reverseGeocode(pos.lat, pos.lng);
            this.snackBar.open('Location found!', 'Close', { duration: 2000 });
          }
        });
      },
      (error) => {
        let errorMessage = 'Error getting your location';
        switch (error.code) {
          case error.PERMISSION_DENIED:
            errorMessage = 'Please allow location access to use this feature';
            break;
          case error.POSITION_UNAVAILABLE:
            errorMessage = 'Location information is unavailable';
            break;
          case error.TIMEOUT:
            errorMessage = 'Location request timed out';
            break;
        }
        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0
      }
    );
  }

  searchLocation(query: string): void {
    if (!query.trim()) return;

    this.snackBar.open('Searching location...', 'Close', { duration: 2000 });

    // Use Nominatim API for geocoding
    fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`)
      .then(response => response.json())
      .then(data => {
        if (data && data.length > 0) {
          const location = data[0];
          const lat = parseFloat(location.lat);
          const lon = parseFloat(location.lon);

          this.map.setView([lat, lon], 15);
          this.marker.setLatLng([lat, lon]);
          this.reverseGeocode(lat, lon);
        } else {
          this.snackBar.open('Location not found', 'Close', { duration: 5000 });
        }
      })
      .catch(error => {
        console.error('Error searching location:', error);
        this.snackBar.open('Error searching location', 'Close', { duration: 5000 });
      });
  }

  private reverseGeocode(lat: number, lon: number): void {
    // Use Nominatim API for reverse geocoding
    fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`)
      .then(response => response.json())
      .then(data => {
        this.ngZone.run(() => {
          if (data && data.address) {
            const address = data.address;
            const locationData = {
              address: data.display_name || '',
              city: address.city || address.town || address.village || address.county || '',
              country: address.country || '',
              postalCode: address.postcode || '',
              latitude: Number(lat.toFixed(6)),
              longitude: Number(lon.toFixed(6))
            };

            // Save the location data
            this.lastKnownLocation = locationData;
            
            // Update the selected address display
            this.selectedAddress = locationData.address;
            
            // Emit the location data
            this.locationSelected.emit(locationData);
          } else {
            this.snackBar.open('Could not get address details for this location', 'Close', { duration: 5000 });
          }
        });
      })
      .catch(error => {
        console.error('Error reverse geocoding:', error);
        this.snackBar.open('Error getting address details', 'Close', { duration: 5000 });
      });
  }
} 