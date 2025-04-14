import { Component, EventEmitter, Output, ChangeDetectorRef, OnDestroy, Input } from '@angular/core';
import { CreatePropertyService } from '../../services/property-crud.service';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-image-upload',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="image-upload-container">
      <div class="upload-area" (click)="fileInput.click()">
        <input 
          type="file" 
          #fileInput 
          (change)="onFileSelected($event)" 
          multiple 
          accept="image/*"
          style="display: none"
        >
        <mat-icon>cloud_upload</mat-icon>
        <p>Click to upload images</p>
        <p class="hint">Supported formats: JPEG, PNG, WebP (max 5MB each)</p>
      </div>

      <div class="uploaded-images" *ngIf="uploadedFiles.length > 0">
        <div class="image-preview" *ngFor="let preview of imagePreviews; let i = index">
          <img [src]="preview.url" alt="Preview">
          <button mat-icon-button class="remove-btn" (click)="removeImage(i)">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>

      <div class="error-message" *ngIf="errorMessage">
        {{ errorMessage }}
      </div>

      <mat-spinner *ngIf="isUploading" diameter="30"></mat-spinner>
    </div>
  `,
  styles: [`
    .image-upload-container {
      padding: 20px;
    }

    .upload-area {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 20px;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s ease;
      margin-bottom: 20px;
    }

    .upload-area:hover {
      border-color: #2196F3;
      background-color: #f5f5f5;
    }

    .upload-area mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #2196F3;
    }

    .hint {
      font-size: 12px;
      color: #666;
      margin-top: 5px;
    }

    .uploaded-images {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      margin-top: 20px;
    }

    .image-preview {
      position: relative;
      width: 150px;
      height: 150px;
      border-radius: 8px;
      overflow: hidden;
    }

    .image-preview img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .remove-btn {
      position: absolute;
      top: 5px;
      right: 5px;
      background-color: rgba(255, 255, 255, 0.8);
    }

    .error-message {
      color: #f44336;
      margin-top: 10px;
    }

    mat-spinner {
      margin: 20px auto;
    }
  `]
})
export class ImageUploadComponent implements OnDestroy {
  @Output() uploadComplete = new EventEmitter<string[]>();
  @Output() uploadError = new EventEmitter<string>();

  isUploading = false;
  uploadedFiles: File[] = [];
  imagePreviews: { file: File; url: string }[] = [];
  errorMessage = '';

  constructor(
    private propertyService: CreatePropertyService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnDestroy() {
    // Clean up blob URLs
    this.imagePreviews.forEach(preview => {
      URL.revokeObjectURL(preview.url);
    });
  }

  async onFileSelected(event: any): Promise<void> {
    const files: FileList = event.target.files;
    
    if (files.length === 0) {
      this.uploadError.emit('No files selected');
      return;
    }

    // Convert FileList to array and validate
    const fileArray = Array.from(files);
    const validFiles = this.validateFiles(fileArray);
    
    if (validFiles.length === 0) {
      return;
    }

    this.isUploading = true;
    try {
      console.log('Starting upload process...');
      console.log('Valid files to upload:', validFiles.map(f => ({
        name: f.name,
        type: f.type,
        size: f.size
      })));

      const urls = await this.propertyService.uploadPropertyImages(validFiles).toPromise();
      console.log('Upload successful. Received URLs:', urls);
      
      if (!urls || urls.length === 0) {
        throw new Error('No URLs received from server');
      }
      
      // Create previews for the new files
      const newPreviews = validFiles.map(file => ({
        file,
        url: URL.createObjectURL(file)
      }));

      // Update the arrays
      this.uploadedFiles = [...this.uploadedFiles, ...validFiles];
      this.imagePreviews = [...this.imagePreviews, ...newPreviews];
      
      // // If we have a propertyId, add the images to the property
      // if (this.propertyId) {
      //   await this.propertyService.addImagesToProperty(this.propertyId, urls).toPromise();
      // }
      
      // Emit the server URLs
      this.uploadComplete.emit(urls);
      
      // Force change detection
      this.cdr.detectChanges();
    } catch (error) {
      console.error('Upload error:', error);
      this.uploadError.emit('Failed to upload images. Please try again.');
    } finally {
      this.isUploading = false;
    }
  }

  private validateFiles(files: File[]): File[] {
    const validFiles: File[] = [];
    const maxFileSize = 5 * 1024 * 1024; // 5MB
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    for (const file of files) {
      if (!allowedTypes.includes(file.type)) {
        console.warn(`Invalid file type: ${file.name} (${file.type})`);
        this.uploadError.emit(`${file.name} is not a supported image type. Please use JPEG, PNG, or WebP.`);
        continue;
      }

      if (file.size > maxFileSize) {
        console.warn(`File too large: ${file.name} (${file.size} bytes)`);
        this.uploadError.emit(`${file.name} is too large. Maximum file size is 5MB.`);
        continue;
      }

      validFiles.push(file);
    }

    console.log('Validated files:', validFiles.map(f => f.name));
    return validFiles;
  }

  removeImage(index: number): void {
    console.log('Removing image at index:', index);
    
    // Revoke the blob URL
    URL.revokeObjectURL(this.imagePreviews[index].url);
    
    // Remove the file and preview
    this.uploadedFiles.splice(index, 1);
    this.imagePreviews.splice(index, 1);
    
    // Create new blob URLs for remaining files
    const remainingUrls = this.uploadedFiles.map(file => URL.createObjectURL(file));
    this.uploadComplete.emit(remainingUrls);
    
    // Force change detection
    this.cdr.detectChanges();
  }
} 