import { Component, OnInit, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProfileComponent } from '../profile/profile.component';
import { AdminServiceService, HostDto, GuestDto, PropertyDto, BookingDto } from '../../services/admin-service.service';
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProfileComponent],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit, AfterViewInit {
  adminName: string = 'Admin User';
  adminPicture: string = 'assets/images/admin-avatar.png';
  currentSection: string = 'all-hosts';
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
  isGuestDropdownOpen: boolean = false;
  isAnalyticsDropdownOpen: boolean = false;

  hosts: HostDto[] = [];
  guests: GuestDto[] = [];
  pendingProperties: PropertyDto[] = [];
  approvedProperties: PropertyDto[] = [];
  bookings: BookingDto[] = [];
  loading: boolean = false;
  error: string | null = null;

  analytics: any = {
    propertiesCount: 0,
    hostsCount: 0,
    guestsCount: 0,
    topRatedHost: null,
    mostBookedProperty: null,
    propertiesByCountry: {},
    topBookingGuests: [],
    topRatedHosts: [],
    topPaidGuests: [],
    topRatedProperties: [],
    topBookedProperties: [],
    topRatedPropertiesByCountry: {}
  };

  countries: string[] = [];
  selectedCountry: string = '';
  propertiesByCountryChart: any;
  bookingsByPropertyChart: any;
  guestsAnalyticsChart: any;
  hostsAnalyticsChart: any;
  propertiesAnalyticsChart: any;
  propertyBookingsCount: { [key: number]: number } = {};

  @ViewChild('guestsAnalyticsChart') guestsAnalyticsChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('hostsAnalyticsChart') hostsAnalyticsChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('propertiesAnalyticsChart') propertiesAnalyticsChartRef!: ElementRef<HTMLCanvasElement>;

  constructor(private router: Router, private adminService: AdminServiceService) {}

  ngOnInit(): void {
    this.loadDataForSection(this.currentSection);
    this.loadAnalytics();
  }

  ngAfterViewInit(): void {
    if (this.currentSection === 'Users' || this.currentSection === 'Properties') {
      this.renderCharts();
    }
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    if (this.isSidebarCollapsed) {
      this.isHostDropdownOpen = false;
      this.isPropertyDropdownOpen = false;
      this.isGuestDropdownOpen = false;
      this.isAnalyticsDropdownOpen = false;
    }
  }

  toggleHostSection(): void {
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('all-hosts');
      return;
    }
    this.isHostDropdownOpen = !this.isHostDropdownOpen;
    if (this.isPropertyDropdownOpen) this.isPropertyDropdownOpen = false;
    if (this.isGuestDropdownOpen) this.isGuestDropdownOpen = false;
    if (this.isAnalyticsDropdownOpen) this.isAnalyticsDropdownOpen = false;
  }

  togglePropertySection(): void {
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('all-properties');
      return;
    }
    this.isPropertyDropdownOpen = !this.isPropertyDropdownOpen;
    if (this.isHostDropdownOpen) this.isHostDropdownOpen = false;
    if (this.isGuestDropdownOpen) this.isGuestDropdownOpen = false;
    if (this.isAnalyticsDropdownOpen) this.isAnalyticsDropdownOpen = false;
  }

  toggleGuestSection(): void {
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('all-guests');
      return;
    }
    this.isGuestDropdownOpen = !this.isGuestDropdownOpen;
    if (this.isHostDropdownOpen) this.isHostDropdownOpen = false;
    if (this.isPropertyDropdownOpen) this.isPropertyDropdownOpen = false;
    if (this.isAnalyticsDropdownOpen) this.isAnalyticsDropdownOpen = false;
  }

  toggleAnalyticsSection(): void {
    if (this.isSidebarCollapsed) {
      this.setCurrentSection('Users');
      return;
    }
    this.isAnalyticsDropdownOpen = !this.isAnalyticsDropdownOpen;
    if (this.isHostDropdownOpen) this.isHostDropdownOpen = false;
    if (this.isPropertyDropdownOpen) this.isPropertyDropdownOpen = false;
    if (this.isGuestDropdownOpen) this.isGuestDropdownOpen = false;
  }

  setCurrentSection(section: string): void {
    this.currentSection = section;
    this.isHostDropdownOpen = false;
    this.isPropertyDropdownOpen = false;
    this.isGuestDropdownOpen = false;
    this.isAnalyticsDropdownOpen = false;
    this.loadDataForSection(section);
    setTimeout(() => {
      if (section === 'Users' || section === 'Properties') {
        this.renderCharts();
      }
    }, 0);
  }

  loadDataForSection(section: string): void {
    this.loading = true;
    this.error = null;

    switch (section) {
      case 'all-hosts':
        this.loadHosts();
        break;
      case 'verified-hosts':
        this.loadVerifiedHosts();
        break;
      case 'unverified-hosts':
        this.loadNotVerifiedHosts();
        break;
      case 'all-guests':
        this.loadAllGuests();
        break;
      case 'active-guests':
        this.loadActiveGuests();
        break;
      case 'blocked-guests':
        this.loadBlockedGuests();
        break;
      case 'unverified-properties':
        this.loadPendingProperties();
        break;
      case 'verified-properties':
        this.loadApprovedProperties();
        break;
      case 'bookings':
        this.loadBookings();
        break;
      case 'Users':
        this.loadUsersAnalytics();
        break;
      case 'Properties':
        this.loadPropertiesAnalytics();
        break;
      default:
        this.loading = false;
    }
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.router.navigate(['/login']);
  }
  loadAnalytics(): void {
    this.loading = true;
    this.error = null;

    this.adminService.getAllHosts().subscribe({
      next: (hosts: HostDto[]) => {
        this.analytics.hostsCount = hosts.length;
        this.analytics.topRatedHosts = hosts
          .sort((a, b) => (b.Rating || 0) - (a.Rating || 0))
          .slice(0, 3);
        this.adminService.getAllGuests().subscribe({
          next: (guests: GuestDto[]) => {
            this.analytics.guestsCount = guests.length;
            this.analytics.topBookingGuests = guests
              .sort((a, b) => (b.bookingsCount || 0) - (a.bookingsCount || 0))
              .slice(0, 3);
            this.analytics.topPaidGuests = guests
              .sort((a, b) => (b.totalSpent || 0) - (a.totalSpent || 0))
              .slice(0, 3);
            this.adminService.getApprovedProperties().subscribe({
              next: (properties: PropertyDto[]) => {
                this.approvedProperties = properties; // Store for filtering
                this.analytics.propertiesCount = properties.length;
                this.analytics.propertiesByCountry = properties.reduce((acc: { [country: string]: number }, property) => {
                  const country = property.country || 'Unknown';
                  acc[country] = (acc[country] || 0) + 1;
                  return acc;
                }, {});
                this.countries = Object.keys(this.analytics.propertiesByCountry);
                this.analytics.topRatedProperties = properties
                  .sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0))
                  .slice(0, 3);
                this.adminService.getAllBookings().subscribe({
                  next: (bookings: BookingDto[]) => {
                    const bookingsCount: { [propertyId: number]: number } = {};
                    bookings.forEach(b => {
                      bookingsCount[b.propertyId] = (bookingsCount[b.propertyId] || 0) + 1;
                    });
                    this.analytics.topBookedProperties = properties
                      .map(p => ({
                        ...p,
                        bookingsCount: bookingsCount[p.id] || 0
                      }))
                      .sort((a, b) => b.bookingsCount - a.bookingsCount)
                      .slice(0, 3);
                    this.analytics.topRatedPropertiesByCountry = properties.reduce((acc: { [country: string]: { count: number, totalRating: number, avgRating: number } }, p) => {
                      const country = p.country || 'Unknown';
                      if (!acc[country]) {
                        acc[country] = { count: 0, totalRating: 0, avgRating: 0 };
                      }
                      acc[country].count += 1;
                      acc[country].totalRating += p.averageRating || 0;
                      acc[country].avgRating = acc[country].totalRating / acc[country].count;
                      return acc;
                    }, {});
                    this.propertyBookingsCount = bookingsCount;
                    this.loading = false;
                    if (this.currentSection === 'Users' || this.currentSection === 'Properties') {
                      setTimeout(() => this.renderCharts(), 0);
                    }
                  },
                  error: (error: any) => {
                    this.error = 'Failed to load bookings: ' + error.message;
                    this.loading = false;
                    console.error('Error loading bookings:', error);
                  }
                });
              },
              error: (error: any) => {
                this.error = 'Failed to load properties: ' + error.message;
                this.loading = false;
                console.error('Error loading properties:', error);
              }
            });
          },
          error: (error: any) => {
            this.error = 'Failed to load guests: ' + error.message;
            this.loading = false;
            console.error('Error loading guests:', error);
          }
        });
      },
      error: (error: any) => {
        this.error = 'Failed to load hosts: ' + error.message;
        this.loading = false;
        console.error('Error loading hosts:', error);
      }
    });
  }

  loadPropertiesAnalytics(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getApprovedProperties().subscribe({
      next: (properties: PropertyDto[]) => {
        this.approvedProperties = properties; // Store for filtering
        this.analytics.propertiesCount = properties.length;
        this.analytics.propertiesByCountry = properties.reduce((acc: { [country: string]: number }, property) => {
          const country = property.country || 'Unknown';
          acc[country] = (acc[country] || 0) + 1;
          return acc;
        }, {});
        this.countries = Object.keys(this.analytics.propertiesByCountry);
        this.analytics.topRatedProperties = properties
          .sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0))
          .slice(0, 3);
        this.analytics.topRatedPropertiesByCountry = properties.reduce((acc: { [country: string]: { count: number, totalRating: number, avgRating: number } }, p) => {
          const country = p.country || 'Unknown';
          if (!acc[country]) {
            acc[country] = { count: 0, totalRating: 0, avgRating: 0 };
          }
          acc[country].count += 1;
          acc[country].totalRating += p.averageRating || 0;
          acc[country].avgRating = acc[country].totalRating / acc[country].count;
          return acc;
        }, {});
        this.adminService.getAllBookings().subscribe({
          next: (bookings: BookingDto[]) => {
            const bookingsCount: { [propertyId: number]: number } = {};
            bookings.forEach(b => {
              bookingsCount[b.propertyId] = (bookingsCount[b.propertyId] || 0) + 1;
            });
            this.analytics.topBookedProperties = properties
              .map(p => ({
                ...p,
                bookingsCount: bookingsCount[p.id] || 0
              }))
              .sort((a, b) => b.bookingsCount - a.bookingsCount)
              .slice(0, 3);
            this.propertyBookingsCount = bookingsCount;
            this.loading = false;
            setTimeout(() => this.renderCharts(), 0);
          },
          error: (error: any) => {
            this.error = 'Failed to load bookings: ' + error.message;
            this.loading = false;
            console.error('Error loading bookings:', error);
          }
        });
      },
      error: (error: any) => {
        this.error = 'Failed to load properties: ' + error.message;
        this.loading = false;
        console.error('Error loading properties:', error);
      }
    });
  }


    getPropertyImage(property: PropertyDto): string {
    if (!property.images || property.images.length === 0) {
      return 'assets/images/property-placeholder.jpg';
    }
    console.log('Property images:', property.images[0].imageUrl);
    return property.images[0]?.imageUrl || 'assets/images/property-placeholder.jpg';
  }

  filterPropertiesByCountry(): void {
    if (this.selectedCountry) {
      this.analytics.topRatedProperties = this.approvedProperties
        .filter(p => p.country === this.selectedCountry)
        .sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0))
        .slice(0, 3);
      this.analytics.topBookedProperties = this.approvedProperties
        .filter(p => p.country === this.selectedCountry)
        .map(p => ({
          ...p,
          bookingsCount: this.propertyBookingsCount[p.id] || 0
        }))
        .sort((a, b) => b.bookingsCount - a.bookingsCount)
        .slice(0, 3);
    } else {
      // Reset to original top properties
      this.analytics.topRatedProperties = this.approvedProperties
        .sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0))
        .slice(0, 3);
      this.analytics.topBookedProperties = this.approvedProperties
        .map(p => ({
          ...p,
          bookingsCount: this.propertyBookingsCount[p.id] || 0
        }))
        .sort((a, b) => b.bookingsCount - a.bookingsCount)
        .slice(0, 3);
    }
    this.renderCharts();
  }

  loadUsersAnalytics(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getAllHosts().subscribe({
      next: (hosts: HostDto[]) => {
        this.analytics.hostsCount = hosts.length;
        this.analytics.topRatedHosts = hosts
          .sort((a, b) => (b.Rating || 0) - (a.Rating || 0))
          .slice(0, 3);
        this.adminService.getAllGuests().subscribe({
          next: (guests: GuestDto[]) => {
            this.analytics.guestsCount = guests.length;
            this.analytics.topBookingGuests = guests
              .sort((a, b) => (b.bookingsCount || 0) - (a.bookingsCount || 0))
              .slice(0, 3);
            this.analytics.topPaidGuests = guests
              .sort((a, b) => (b.totalSpent || 0) - (a.totalSpent || 0))
              .slice(0, 3);
            this.loading = false;
            setTimeout(() => this.renderCharts(), 0);
          },
          error: (error: any) => {
            this.error = 'Failed to load guests: ' + error.message;
            this.loading = false;
            console.error('Error loading guests:', error);
          }
        });
      },
      error: (error: any) => {
        this.error = 'Failed to load hosts: ' + error.message;
        this.loading = false;
        console.error('Error loading hosts:', error);
      }
    });
  }



  renderCharts(): void {
    if (this.currentSection === 'Users') {
      this.renderGuestsAnalyticsChart();
      this.renderHostsAnalyticsChart();
    } else if (this.currentSection === 'Properties') {
      this.renderPropertiesAnalyticsChart();
    }
  }

  renderGuestsAnalyticsChart(): void {
    if (this.guestsAnalyticsChart) {
      this.guestsAnalyticsChart.destroy();
    }
    const ctx = this.guestsAnalyticsChartRef?.nativeElement.getContext('2d');
    if (ctx) {
      this.guestsAnalyticsChart = new Chart(ctx, {
        type: 'bar',
        data: {
          labels: ['Total Guests', 'Top Booking Guests', 'Top Paid Guests'],
          datasets: [{
            label: 'Guests Analytics',
            data: [
              this.analytics.guestsCount,
              this.analytics.topBookingGuests.reduce((sum: number, g: GuestDto) => sum + (g.bookingsCount || 0), 0),
              this.analytics.topPaidGuests.reduce((sum: number, g: GuestDto) => sum + (g.totalSpent || 0), 0) / 100
            ],
            backgroundColor: ['#FF5A5F', '#FFB400', '#00A699'],
            borderColor: ['#FF385C', '#FFA000', '#008489'],
            borderWidth: 1
          }]
        },
        options: {
          responsive: true,
          plugins: {
            legend: { display: false },
            title: { display: true, text: 'Guests Analytics', color: '#FF5A5F', font: { size: 16 } }
          },
          scales: {
            y: { beginAtZero: true, title: { display: true, text: 'Value' } }
          }
        }
      });
    }
  }

  renderHostsAnalyticsChart(): void {
    if (this.hostsAnalyticsChart) {
      this.hostsAnalyticsChart.destroy();
    }
    const ctx = this.hostsAnalyticsChartRef?.nativeElement.getContext('2d');
    if (ctx) {
      this.hostsAnalyticsChart = new Chart(ctx, {
        type: 'bar',
        data: {
          labels: ['Total Hosts', 'Top Rated Hosts'],
          datasets: [{
            label: 'Hosts Analytics',
            data: [
              this.analytics.hostsCount,
              this.analytics.topRatedHosts.reduce((sum: number, h: HostDto) => sum + (h.Rating || 0), 0)
            ],
            backgroundColor: ['#FF5A5F', '#008489'],
            borderColor: ['#FF385C', '#006B5F'],
            borderWidth: 1
          }]
        },
        options: {
          responsive: true,
          plugins: {
            legend: { display: false },
            title: { display: true, text: 'Hosts Analytics', color: '#FF5A5F', font: { size: 16 } }
          },
          scales: {
            y: { beginAtZero: true, title: { display: true, text: 'Value' } }
          }
        }
      });
    }
  }

  renderPropertiesAnalyticsChart(): void {
    if (this.propertiesAnalyticsChart) {
      this.propertiesAnalyticsChart.destroy();
    }
    const ctx = this.propertiesAnalyticsChartRef?.nativeElement.getContext('2d');
    if (ctx) {
      const topCountries = Object.entries(this.analytics.topRatedPropertiesByCountry)
        .sort(([, a]: any, [, b]: any) => b.avgRating - a.avgRating)
        .slice(0, 5)
        .map(([country]) => country);
      this.propertiesAnalyticsChart = new Chart(ctx, {
        type: 'bar',
        data: {
          labels: ['Total Properties', 'Top Rated Properties', 'Top Booked Properties', ...topCountries],
          datasets: [{
            label: 'Properties Analytics',
            data: [
              this.analytics.propertiesCount,
              this.analytics.topRatedProperties.reduce((sum: number, p: PropertyDto) => sum + (p.averageRating || 0), 0),
              this.analytics.topBookedProperties.reduce((sum: number, p: any) => sum + p.bookingsCount, 0),
              ...topCountries.map(country => this.analytics.topRatedPropertiesByCountry[country].avgRating || 0)
            ],
            backgroundColor: ['#FF5A5F', '#FFB400', '#00A699', '#FF385C', '#008489'],
            borderColor: ['#FF385C', '#FFA000', '#008489', '#FF385C', '#006B5F'],
            borderWidth: 1
          }]
        },
        options: {
          responsive: true,
          plugins: {
            legend: { display: false },
            title: { display: true, text: 'Properties Analytics', color: '#FF5A5F', font: { size: 16 } }
          },
          scales: {
            y: { beginAtZero: true, title: { display: true, text: 'Value' } }
          }
        }
      });
    }
  }

  // Host Management
  loadHosts(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getAllHosts().subscribe({
      next: (data) => {
        this.hosts = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load hosts';
        this.loading = false;
        console.error('Error loading hosts:', err);
      }
    });
  }

  loadVerifiedHosts(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getVerifiedHosts().subscribe({
      next: (data) => {
        this.hosts = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load verified hosts';
        this.loading = false;
        console.error('Error loading verified hosts:', err);
      }
    });
  }

  loadNotVerifiedHosts(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getNotVerifiedHosts().subscribe({
      next: (data) => {
        this.hosts = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load unverified hosts';
        this.loading = false;
        console.error('Error loading unverified hosts:', err);
      }
    });
  }

  // verifyHost(verificationId: number): void {
  //   this.loading = true;
  //   this.adminService.verifyHost(verificationId).subscribe({
  //     next: () => {
  //       this.loadHosts();
  //       this.loading = false;
  //       alert('Host verified successfully.');
  //     },
  //     error: (error: any) => {
  //       console.error('Error verifying host:', error);
  //       this.error = 'Failed to verify host.';
  //       this.loading = false;
  //     }
  //   });
  // }


  verifyHost(hostId: number) {
    this.router.navigate(['/admin/verifinghost', hostId]);
  }

  // Guest Management
  loadAllGuests(): void {
    this.adminService.getAllGuests().subscribe({
      next: (guests: GuestDto[]) => {
        this.guests = guests;
        this.loading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load guests: ' + error.message;
        this.loading = false;
      }
    });
  }

  loadActiveGuests(): void {
    this.adminService.getAllGuests().subscribe({
      next: (guests: GuestDto[]) => {
        this.guests = guests.filter(guest => guest.accountStatus === 'Active');
        this.loading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load active guests: ' + error.message;
        this.loading = false;
      }
    });
  }

  loadBlockedGuests(): void {
    this.adminService.getAllGuests().subscribe({
      next: (guests: GuestDto[]) => {
        this.guests = guests.filter(guest => guest.accountStatus === 'Blocked');
        this.loading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load blocked guests: ' + error.message;
        this.loading = false;
      }
    });
  }

  // Property Management
  loadPendingProperties(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getPendingProperties().subscribe({
      next: (data) => {
        this.pendingProperties = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load pending properties';
        this.loading = false;
        console.error('Error loading pending properties:', err);
      }
    });
  }

  loadApprovedProperties(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getApprovedProperties().subscribe({
      next: (data) => {
        this.approvedProperties = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load approved properties';
        this.loading = false;
        console.error('Error loading approved properties:', err);
      }
    });
  }

  // Booking Management
  loadBookings(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getAllBookings().subscribe({
      next: (data) => {
        this.bookings = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load bookings';
        this.loading = false;
        console.error('Error loading bookings:', err);
      }
    });
  }

  // User Management
  blockUser(userId: number, isBlocked: boolean): void {
    this.loading = true;
    this.adminService.blockUser(userId, isBlocked).subscribe({
      next: () => {
        this.loadAllGuests();
        this.loading = false;
      },
      error: (error: Error) => {
        console.error('Error blocking/unblocking user:', error);
        this.error = 'Failed to update user status';
        this.loading = false;
      }
    });
  }

  // Property Management
  approveProperty(propertyId: number, isApproved: boolean): void {
    this.adminService.approveProperty(propertyId, isApproved).subscribe({
      next: () => {
        this.pendingProperties = this.pendingProperties.filter(p => p.id !== propertyId);
        if (!isApproved) {
          alert('Property rejected successfully.');
        }
      },
      error: (err) => {
        console.error('Error approving/rejecting property:', err);
        alert('Failed to process the request.');
      }
    });
  }

  suspendProperty(propertyId: number, isSuspended: boolean): void {
    this.loading = true;
    this.error = null;
    this.adminService.suspendProperty(propertyId, isSuspended).subscribe({
      next: () => {
        this.loadApprovedProperties();
        this.loading = false;
      },
      error: (err) => {
        this.error = `Failed to ${isSuspended ? 'suspend' : 'activate'} property`;
        this.loading = false;
        console.error('Error suspending/activating property:', err);
      }
    });
  }

  // Booking Management
  updateBookingStatus(bookingId: number, status: string): void {
    this.loading = true;
    this.error = null;
    this.adminService.updateBookingStatus(bookingId, status).subscribe({
      next: () => {
        this.loadBookings();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to update booking status';
        this.loading = false;
        console.error('Error updating booking status:', err);
      }
    });
  }
}