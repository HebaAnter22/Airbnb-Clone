import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ViolationService, ViolationResponseDto, UpdateViolationStatusDto, BookingDto } from '../../../services/violation.service';
import { PaymentService, AdminRefundDto } from '../../../services/payment.service';

@Component({
  selector: 'app-violations-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './violations-management.component.html',
  styleUrls: ['./violations-management.component.css']
})
export class ViolationsManagementComponent implements OnInit {
  violations: ViolationResponseDto[] = [];
  filteredViolations: ViolationResponseDto[] = [];
  loading = false;
  error: string | null = null;
  success: string | null = null;
  
  selectedViolation: ViolationResponseDto | null = null;
  showDetailsModal = false;
  adminNotes = '';
  
  // Related bookings
  relatedBookings: BookingDto[] = [];
  loadingBookings = false;
  showRelatedBookings = false;
  
  // Refund
  showRefundModal = false;
  selectedBooking: BookingDto | null = null;
  refundAmount: number | null = null;
  refundReason = 'fraudulent';
  processingRefund = false;
  
  // Filters
  statusFilter = 'all';
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  Math = Math; // Make Math available to the template
  
  violationStatuses = [
    { value: 'Pending', label: 'Pending' },
    { value: 'UnderReview', label: 'Under Review' },
    { value: 'Resolved', label: 'Resolved' },
    { value: 'Dismissed', label: 'Dismissed' }
  ];
  
  refundReasons = [
    { value: 'duplicate', label: 'Duplicate Charge' },
    { value: 'fraudulent', label: 'Fraudulent Activity' },
    { value: 'requested_by_customer', label: 'Customer Request' }
  ];
  
  constructor(
    private violationService: ViolationService,
    private paymentService: PaymentService
  ) {}
  
  ngOnInit(): void {
    this.loadViolations();
  }
  
  loadViolations(): void {
    this.loading = true;
    this.error = null;
    
    this.violationService.getAllViolations()
      .subscribe({
        next: (data) => {
          this.violations = data;
          this.applyFilters();
          this.loading = false;
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to load violations';
          this.loading = false;
        }
      });
  }
  
  applyFilters(): void {
    let filtered = [...this.violations];
    
    // Apply status filter
    if (this.statusFilter !== 'all') {
      filtered = filtered.filter(v => v.status === this.statusFilter);
    }
    
    this.totalItems = filtered.length;
    this.filteredViolations = this.paginateArray(filtered, this.currentPage, this.pageSize);
  }
  
  paginateArray(array: any[], page: number, pageSize: number): any[] {
    const startIndex = (page - 1) * pageSize;
    return array.slice(startIndex, startIndex + pageSize);
  }
  
  onPageChange(page: number): void {
    this.currentPage = page;
    this.applyFilters();
  }
  
  onStatusFilterChange(): void {
    this.currentPage = 1;
    this.applyFilters();
  }
  
  viewDetails(violation: ViolationResponseDto): void {
    this.selectedViolation = violation;
    this.adminNotes = '';
    this.showDetailsModal = true;
    this.showRelatedBookings = false;
    this.relatedBookings = [];
  }
  
  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedViolation = null;
    this.showRelatedBookings = false;
    this.relatedBookings = [];
  }
  
  loadRelatedBookings(): void {
    if (!this.selectedViolation) return;
    
    this.loadingBookings = true;
    this.error = null;
    
    this.violationService.getRelatedBookings(this.selectedViolation.id)
      .subscribe({
        next: (bookings) => {
          this.relatedBookings = bookings;
          this.showRelatedBookings = true;
          this.loadingBookings = false;
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to load related bookings';
          this.loadingBookings = false;
        }
      });
  }
  
  openRefundModal(booking: BookingDto): void {
    this.selectedBooking = booking;
    this.refundAmount = booking.paymentAmount ? booking.paymentAmount - (booking.paymentAmount / 100) : null;
    this.refundReason = 'fraudulent';
    this.showRefundModal = true;
  }
  
  closeRefundModal(): void {
    this.showRefundModal = false;
    this.selectedBooking = null;
    this.refundAmount = null;
  }
  
  processRefund(): void {
    if (!this.selectedViolation || !this.selectedBooking || !this.refundAmount) return;
    
    if (this.refundAmount <= 0) {
      this.error = 'Refund amount must be greater than zero';
      return;
    }
    
    if (!this.selectedBooking.paymentId) {
      this.error = 'No payment ID found for this booking';
      return;
    }
    
    this.processingRefund = true;
    this.error = null;
    this.success = null;
    
    const refundData: AdminRefundDto = {
      paymentId: this.selectedBooking.paymentId,
      refundAmount: this.refundAmount,
      violationId: this.selectedViolation.id,
      reason: this.refundReason,
      adminNotes: this.adminNotes
    };
    
    this.paymentService.processAdminRefund(refundData)
      .subscribe({
        next: (response) => {
          this.success = `Refund of $${this.refundAmount} processed successfully`;
          this.processingRefund = false;
          
          // Close the refund modal after a delay
          setTimeout(() => {
            this.closeRefundModal();
            // Reload related bookings to reflect the change
            this.loadRelatedBookings();
            // Reload the violation to reflect updated status
            this.violationService.getViolationById(this.selectedViolation!.id)
              .subscribe(updatedViolation => {
                // Update the violation in the list
                const index = this.violations.findIndex(v => v.id === updatedViolation.id);
                if (index !== -1) {
                  this.violations[index] = updatedViolation;
                  this.selectedViolation = updatedViolation;
                }
                this.applyFilters();
              });
          }, 2000);
        },
        error: (err) => {
          this.error = err.error?.error || 'Failed to process refund';
          this.processingRefund = false;
        }
      });
  }
  
  updateStatus(status: string): void {
    if (!this.selectedViolation) return;
    
    const data: UpdateViolationStatusDto = {
      status,
      adminNotes: this.adminNotes || undefined
    };
    
    this.loading = true;
    this.error = null;
    this.success = null;
    
    this.violationService.updateViolationStatus(this.selectedViolation.id, data)
      .subscribe({
        next: (updatedViolation) => {
          // Update the violation in the list
          const index = this.violations.findIndex(v => v.id === updatedViolation.id);
          if (index !== -1) {
            this.violations[index] = updatedViolation;
          }
          
          this.success = `Violation status updated to ${status}`;
          this.loading = false;
          this.applyFilters();
          
          setTimeout(() => {
            this.closeDetailsModal();
          }, 2000);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to update violation status';
          this.loading = false;
        }
      });
  }
  
  blockHost(): void {
    if (!this.selectedViolation || !this.selectedViolation.reportedHostId) return;
    
    if (!confirm(`Are you sure you want to block host ${this.selectedViolation.reportedHostName}? This action will suspend all their properties and block their account.`)) {
      return;
    }
    
    this.loading = true;
    this.error = null;
    this.success = null;
    
    this.violationService.blockHost(this.selectedViolation.reportedHostId)
      .subscribe({
        next: () => {
          this.success = `Host ${this.selectedViolation?.reportedHostName} has been blocked`;
          this.loading = false;
          
          // Also mark the violation as resolved
          if (this.selectedViolation) {
            this.updateStatus('Resolved');
          }
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to block host';
          this.loading = false;
        }
      });
  }
} 