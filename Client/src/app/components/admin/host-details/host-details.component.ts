import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AdminServiceService, HostDto, ViolationDto } from '../../../services/admin-service.service';
import { Observable, forkJoin } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../../services/toast.service';
import { ProfileService } from '../../../services/profile.service';

@Component({
  selector: 'app-host-details',
  templateUrl: './host-details.component.html',
  styleUrls: ['./host-details.component.css'],
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    RouterModule
  ]
})
export class HostDetailsComponent implements OnInit {
  @Input() hostId: number | null = null;
  host: HostDto | null = null;
  violations: ViolationDto[] = [];
  loading = false;
  error = '';
  hostRating: number = 0;
  hostReviews: any[] = [];
  
  // Filter options
  violationStatusFilter: string = 'all';
  
  // Stats
  totalViolations = 0;
  pendingViolations = 0;
  resolvedViolations = 0;
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adminService: AdminServiceService,
    private toastService: ToastService,
    private profileService: ProfileService
  ) { }

  ngOnInit(): void {
    if (!this.hostId) {
      this.route.params.subscribe(params => {
        const id = +params['id'];
        if (id) {
          this.hostId = id;
          this.loadHostData();
        }
      });
    } else {
      this.loadHostData();
    }
  }
  
  loadHostData(): void {
    if (!this.hostId) return;
    
    this.loading = true;
    const hostId = this.hostId;
    
    // Use forkJoin to make parallel requests
    forkJoin({
      // Get host details using the verified hosts endpoint
      hosts: this.adminService.getVerifiedHosts(),
      violations: this.adminService.getHostViolations(hostId)
    }).subscribe({
      next: (results) => {
        // Find the host in the verified hosts
        this.host = results.hosts.find(h => h.id === hostId) || null;
        this.violations = results.violations;
        
        // Calculate stats
        this.totalViolations = this.violations.length;
        this.pendingViolations = this.violations.filter(v => v.status === 'Pending').length;
        this.resolvedViolations = this.violations.filter(v => v.status === 'Resolved').length;
        
        if (this.host) {
          this.loadHostRating(this.host.id.toString());
        }
        
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load host data: ' + (err.message || 'Unknown error');
        this.loading = false;
      }
    });
  }
  
  loadHostRating(hostId: string): void {
    // Get host reviews from ProfileService
    this.profileService.getUserReviews(hostId).subscribe({
      next: (reviews) => {
        this.hostReviews = reviews;
        
        // Calculate average rating if there are reviews
        if (reviews && reviews.length > 0) {
          const totalRating = reviews.reduce((sum, review) => sum + review.rating, 0);
          this.hostRating = totalRating / reviews.length;
        }
      },
      error: (err) => {
        console.error('Failed to load host reviews:', err);
        // Use the rating from the host object as fallback
        this.hostRating = this.host?.Rating || 0;
      }
    });
  }
  
  blockHost(): void {
    if (!this.hostId || !this.host) return;
    
    if (confirm(`Are you sure you want to block ${this.host.firstName} ${this.host.lastName}? This will suspend all their properties and prevent them from hosting.`)) {
      this.loading = true;
      
      this.adminService.blockHost(this.hostId).subscribe({
        next: () => {
          this.toastService.success('Host blocked successfully');
          this.loading = false;
          
          // Navigate back to the verified hosts page
          this.router.navigate(['/admin'], { queryParams: { section: 'verified-hosts' } });
        },
        error: (err) => {
          this.error = 'Failed to block host: ' + (err.message || 'Unknown error');
          this.loading = false;
        }
      });
    }
  }
  
  getFilteredViolations(): ViolationDto[] {
    if (this.violationStatusFilter === 'all') {
      return this.violations;
    }
    return this.violations.filter(v => v.status.toLowerCase() === this.violationStatusFilter.toLowerCase());
  }
  
  getViolationTypeClass(type: string): string {
    switch (type.toLowerCase()) {
      case 'safety':
        return 'safety-violation';
      case 'policy':
        return 'policy-violation';
      case 'fraud':
        return 'fraud-violation';
      default:
        return 'other-violation';
    }
  }
  
  formatDate(date: Date | undefined | null): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }
  
  goBack(): void {
    this.router.navigate(['/admin'], { queryParams: { section: 'verified-hosts' } });
  }
} 