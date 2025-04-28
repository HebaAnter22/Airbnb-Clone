import { Directive, ElementRef, Input, OnInit, ViewContainerRef, ComponentRef, ComponentFactoryResolver } from '@angular/core';
import { ReportViolationComponent } from '../components/common/report-violation/report-violation.component';

@Directive({
  selector: '[appReportViolation]',
  standalone: true
})
export class ReportViolationDirective implements OnInit {
  @Input() propertyId?: number;
  @Input() hostId?: number;
  
  private componentRef: ComponentRef<ReportViolationComponent> | null = null;
  
  constructor(
    private elementRef: ElementRef,
    private viewContainerRef: ViewContainerRef
  ) {}
  
  ngOnInit(): void {
    // Create the component
    this.componentRef = this.viewContainerRef.createComponent(ReportViolationComponent);
    
    // Set the inputs
    if (this.propertyId) {
      this.componentRef.instance.propertyId = this.propertyId;
    }
    
    if (this.hostId) {
      this.componentRef.instance.hostId = this.hostId;
    }
    
    // Append the component to the element
    this.elementRef.nativeElement.appendChild(this.componentRef.location.nativeElement);
  }
} 