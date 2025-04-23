import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminServiceService } from '../../../services/admin-service.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

interface HostVerification {
  id: number;
  hostId: number;
  hostName: string;
  status: string;
  verificationDocumentUrl1: string;
  verificationDocumentUrl2: string;
  submittedAt: Date;
}

@Component({
  selector: 'app-verifinghost',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './verifinghost.component.html',
  styleUrl: './verifinghost.component.css'
})
export class VerifinghostComponent implements OnInit {
  hostId: number = 0;
  hostVerification: HostVerification | null = null;
  loading: boolean = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private adminService: AdminServiceService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.hostId = +params['id'];
      if (this.hostId) {
        this.loadHostVerification();
      } else {
        this.error = 'Host ID not provided';
        this.loading = false;
      }
    });
  }

  goBack(){

  }
  loadHostVerification(): void {
    this.loading = true;
    this.error = null;
    
    this.adminService.gethostverfication(this.hostId).subscribe({
      next: (data) => {
        this.hostVerification = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load host verification data';
        this.loading = false;
        console.error('Error loading host verification:', err);
      }
    });
  }


  verifyHost(): void {
    this.loading = true;
    this.adminService.verifyHost(this.hostId, true).subscribe({
      next: () => {
        this.snackBar.open('Host verified successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/admin']);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to verify host';
        this.loading = false;
        console.error('Error verifying host:', err);
      }
    });
  }
  


  rejectVerification(): void {
    this.loading = true;
    this.adminService.rejectHost(this.hostId, false).subscribe({
      next: () => {
        this.snackBar.open('Verification rejected', 'Close', { duration: 3000 });
        this.router.navigate(['/admin']);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to reject host verification';
        this.loading = false;
        console.error('Error rejecting host verification:', err);
      }
    });
  }
}
