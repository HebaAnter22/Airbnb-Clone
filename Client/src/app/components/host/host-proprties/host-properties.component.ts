import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CreatePropertyService } from '../../../services/property-crud.service';
import { PropertyDto } from '../../../models/property';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-host-properties',
  standalone: true,
  templateUrl: './host-properties.component.html',
  styleUrls: ['./host-properties.component.css'],
  imports: [CommonModule, RouterModule]
})
export class HostPropertiesComponent implements OnInit {
  properties: PropertyDto[] = [];
  loading: boolean = true;
  error: string | null = null;

  constructor(
    private propertyService: CreatePropertyService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProperties();
  }

  async loadProperties() {
    try {
      const userId = await this.propertyService.getCurrentUserId();
      const result = await this.propertyService.getMyProperties().toPromise();
      this.properties = result || [];
      this.loading = false;
    } catch (err) {
      this.error = 'Failed to load properties';
      this.loading = false;
    }
  }

  editProperty(propertyId: number) {
    this.router.navigate(['/host/properties/edit', propertyId]);
  }

  async deleteProperty(propertyId: number) {
    if (confirm('Are you sure you want to delete this property?')) {
      try {
        await this.propertyService.deleteProperty(propertyId).toPromise();
        this.properties = this.properties.filter(p => p.id !== propertyId);
      } catch (err) {
        this.error = 'Failed to delete property';
      }
    }
  }
} 