import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ViolationService, CreateViolationDto } from '../../../services/violation.service';

@Component({
  selector: 'app-report-violation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-violation.component.html',
  styleUrls: ['./report-violation.component.css']
})
export class ReportViolationComponent {
  @Input() propertyId?: number;
  @Input() hostId?: number;
  @Output() reportSubmitted = new EventEmitter<boolean>();
  
  showModal = false;
  isSubmitting = false;
  violationTypes = [
    { value: 'PropertyMisrepresentation', label: 'Property Misrepresentation' },
    { value: 'HostMisconduct', label: 'Host Misconduct' },
    { value: 'SafetyIssue', label: 'Safety Issue' },
    { value: 'PolicyViolation', label: 'Policy Violation' },
    { value: 'FraudulentActivity', label: 'Fraudulent Activity' },
    { value: 'Other', label: 'Other' }
  ];
  
  formData: CreateViolationDto = {
    violationType: '',
    description: ''
  };
  
  error: string | null = null;
  success: string | null = null;
  
  constructor(private violationService: ViolationService) {}
  
  openModal(): void {
    this.showModal = true;
    this.resetForm();
  }
  
  closeModal(): void {
    this.showModal = false;
  }
  
  resetForm(): void {
    this.formData = {
      violationType: '',
      description: ''
    };
    this.error = null;
    this.success = null;
  }
  
  submitReport(): void {
    if (!this.formData.violationType || !this.formData.description) {
      this.error = 'Please fill in all required fields';
      return;
    }
    
    if (this.propertyId) {
      this.formData.reportedPropertyId = this.propertyId;
    }
    
    if (this.hostId) {
      this.formData.reportedHostId = this.hostId;
    }
    
    this.isSubmitting = true;
    
    this.violationService.reportViolation(this.formData)
      .subscribe({
        next: () => {
          this.success = 'Report submitted successfully';
          this.isSubmitting = false;
          this.reportSubmitted.emit(true);
          setTimeout(() => {
            this.closeModal();
          }, 2000);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to submit report';
          this.isSubmitting = false;
        }
      });
  }
} 