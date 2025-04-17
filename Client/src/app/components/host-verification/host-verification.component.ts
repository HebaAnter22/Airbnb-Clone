import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-host-verification',
  templateUrl: './host-verification.component.html',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HttpClientModule,
  ],
  standalone: true,
  styleUrls: ['./host-verification.component.css']
})
export class HostVerificationComponent implements OnInit {
  verificationForm: FormGroup;
  idVerificationStep = 1; // 1: upload, 2: verification failed, 3: verification success
  phoneVerificationStep: number = 1; // 1: enter phone, 2: enter code, 3: verified - using number type
  selectedFile: File | null = null;
  previewUrl: string | ArrayBuffer | null = null;
  generatedCode: string = '';
  verificationMessage: string = '';
  isProcessing: boolean = false;
  
  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.verificationForm = this.fb.group({
      phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{10,11}$/)]],
      verificationCode: ['', [Validators.required, Validators.pattern(/^[0-9]{4}$/)]]
    });
  }
  
  ngOnInit(): void {}
  
  onFileSelected(event: any): void {
    this.selectedFile = event.target.files[0];
    this.previewFile();
  }
  
  previewFile(): void {
    if (this.selectedFile) {
      const reader = new FileReader();
      reader.onload = () => {
        this.previewUrl = reader.result;
      };
      reader.readAsDataURL(this.selectedFile);
    }
  }
  
  retakePhoto(): void {
    this.selectedFile = null;
    this.previewUrl = null;
    this.idVerificationStep = 1;
  }
  
  verifyIdentity(): void {
    // Here you would call your backend API to verify the ID
    // For this example, we'll simulate a failed verification first
    this.idVerificationStep = 2;
  }
  
  generateRandomCode(): string {
    // Generate a random 4-digit code
    return Math.floor(1000 + Math.random() * 9000).toString();
  }
  
  submitPhoneNumber(): void {
    if (this.verificationForm.get('phoneNumber')?.valid) {
      this.isProcessing = true;
      // Generate a random 4-digit code
      this.generatedCode = this.generateRandomCode();
      console.log('Generated code:', this.generatedCode);
      
      const phoneNumber = this.verificationForm.get('phoneNumber')?.value;
      const formattedPhone = '+20' + phoneNumber; // Adding Egypt country code
      
      // Send SMS using SendGrid
      this.sendSmsViaSendGrid(formattedPhone, this.generatedCode);
    }
  }
  
  sendSmsViaSendGrid(phoneNumber: string, code: string): void {
    // This would typically be handled by your backend for security reasons
    // But for demonstration purposes, this shows how the request would be structured
    
    // In a real implementation, this API endpoint would be on your server
    // which would then call SendGrid's API
    const backendEndpoint =  'localhost:7228/api/send-verification-sms';
    
    const smsData = {
      phoneNumber: phoneNumber,
      verificationCode: code,
      message: `Your verification code is: ${code}`
    };
    
    this.http.post(backendEndpoint, smsData).subscribe({
      next: (response: any) => {
        console.log('SMS sent successfully', response);
        this.isProcessing = false;
        this.phoneVerificationStep = 2;
      },
      error: (error) => {
        console.error('Failed to send SMS', error);
        this.isProcessing = false;
        this.verificationMessage = 'Failed to send verification code. Please try again.';
        // Handle error appropriately
      }
    });
  }
  
  verifyPhoneNumber(): void {
    const enteredCode = this.verificationForm.get('verificationCode')?.value;
    
    if (enteredCode === this.generatedCode) {
      // Verification successful
      this.verificationMessage = 'Phone number verified successfully!';
      this.phoneVerificationStep = 3; // Move to verified state
    } else {
      // Verification failed
      this.verificationMessage = 'Invalid code. Please try again.';
      // Optionally clear the input field
      this.verificationForm.get('verificationCode')?.setValue('');
    }
  }
  
  resendCode(): void {
    // Generate a new code
    this.generatedCode = this.generateRandomCode();
    console.log('New generated code:', this.generatedCode);
    
    const phoneNumber = this.verificationForm.get('phoneNumber')?.value;
    const formattedPhone = '+20' + phoneNumber; // Adding Egypt country code
    
    // Resend SMS using SendGrid
    this.sendSmsViaSendGrid(formattedPhone, this.generatedCode);
    
    this.verificationMessage = 'A new code has been sent to your phone.';
  }
  
  requestCall(): void {
    // In a real app, you would call an API to initiate a phone call
    // SendGrid doesn't provide voice calling, so you might need another service for this
    this.verificationMessage = 'Call functionality is not available with this service.';
  }
}