import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProfileService } from '../../services/profile.service';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './edit-profile.component.html',
  styleUrls: ['./edit-profile.component.css']
})
export class EditProfileComponent implements OnInit {
  userId: string = '';
  profileForm: FormGroup;
  user: any = {};
  selectedFile: File | null = null;
  apiBaseUrl: string = 'https://localhost:7228'; // API server base URL
  profileImageUrl: string = '';
  isUploading: boolean = false;
  uploadProgress: number = 0;
  isGuestUser: boolean = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private profileService: ProfileService
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      aboutMe: [''],
      whereIveAlwaysWantedToGo: [''],
      myWork: [''],
      myFavoriteSong: [''],
      pets: [''],
      whereIWasBorn: [''], // This will be a date value
      whereIWentToSchool: [''],
      timeSpent: [''],
      funFact: [''],
      uselessSkill: [''],
      languages: [''],
      biography: [''],
      obsessedWith: [''],
      whereILive: [''],
      specialAbout: ['']
    });
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.userId = params['id'];
      this.loadUserProfile();
    });
  }

  loadUserProfile(): void {
    this.profileService.getUserProfileForEdit(this.userId).subscribe({
      next: (user) => {
        this.user = user;
        // Check if user is a Guest
        this.isGuestUser = user.role === 'Guest'|| user.role === 'Admin';
        
        this.profileImageUrl = user.profilePictureUrl;
        console.log('Profile Image URL:', this.profileImageUrl);
              
        // Format the date for the HTML date input (YYYY-MM-DD)
        const formattedDateOfBirth = user.dateOfBirth ? this.formatDateForInput(new Date(user.dateOfBirth)) : '';
        
        // Populate form with user data
        this.profileForm.patchValue({
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          email: user.email || '',
          aboutMe: user.aboutMe || '',
          whereIveAlwaysWantedToGo: user.dreamDestination || '',
          myWork: user.work || '',
          myFavoriteSong: user.myFavoriteSong || '',
          pets: user.pets || '',
          whereIWasBorn: formattedDateOfBirth,
          whereIWentToSchool: user.education || '',
          funFact: user.funFact || '',
          languages: user.languages || '',
          obsessedWith: user.obsessedWith || '',
          whereILive: user.livesIn || '',
          specialAbout: user.specialAbout || ''
        });
        
        // If user is a Guest, disable form controls that they shouldn't edit
        if (this.isGuestUser) {
          this.disableNonGuestFields();
        }
      },
      error: (error) => {
        console.error('Error fetching user profile', error);
      }
    });
  }
  
  disableNonGuestFields(): void {
    // Disable all fields except firstName, lastName, and whereIWasBorn (date of birth)
    this.profileForm.get('email')?.disable();
    this.profileForm.get('aboutMe')?.disable();
    this.profileForm.get('whereIveAlwaysWantedToGo')?.disable();
    this.profileForm.get('myWork')?.disable();
    this.profileForm.get('myFavoriteSong')?.disable();
    this.profileForm.get('pets')?.disable();
    this.profileForm.get('whereIWentToSchool')?.disable();
    this.profileForm.get('timeSpent')?.disable();
    this.profileForm.get('funFact')?.disable();
    this.profileForm.get('uselessSkill')?.disable();
    this.profileForm.get('languages')?.disable();
    this.profileForm.get('biography')?.disable();
    this.profileForm.get('obsessedWith')?.disable();
    this.profileForm.get('whereILive')?.disable();
    this.profileForm.get('specialAbout')?.disable();
  }

  // Format date as YYYY-MM-DD for HTML date input
  formatDateForInput(date: Date): string {
    if (!date || isNaN(date.getTime())) return '';
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-based
    const day = String(date.getDate()).padStart(2, '0');
    
    return `${year}-${month}-${day}`;
  }

  onFileSelected(event: any): void {
    this.selectedFile = event.target.files[0] as File;
    if (this.selectedFile) {
      this.uploadProfilePicture();
    }
  }

  uploadProfilePicture(): void {
    if (!this.selectedFile) {
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;

    this.profileService.uploadProfilePicture(this.selectedFile).subscribe({
      next: (response) => {
        this.profileImageUrl = `${this.apiBaseUrl}/${response.fileUrl}`;
        this.isUploading = false;
        this.uploadProgress = 100;
      },
      error: (error) => {
        console.error('Error uploading profile picture', error);
        this.isUploading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.profileForm.valid) {
      // Get the date value directly from the form
      const dateOfBirth = this.profileForm.value.whereIWasBorn
        ? new Date(this.profileForm.value.whereIWasBorn)
        : null;
      
      // Create profile data object based on user role
      let profileData;
      
      if (this.isGuestUser) {
        // For Guest users, only update allowed fields
        profileData = {
          Id: this.userId,
          FirstName: this.profileForm.value.firstName,
          LastName: this.profileForm.value.lastName,
          DateOfBirth: dateOfBirth,
          ProfilePictureUrl: this.profileImageUrl.split(this.apiBaseUrl)[1] || ''
        };
      } else {
        // For other users, update all fields
        profileData = {
          Id: this.userId,
          FirstName: this.profileForm.value.firstName,
          LastName: this.profileForm.value.lastName,
          DateOfBirth: dateOfBirth,
          ProfilePictureUrl: this.profileImageUrl.split(this.apiBaseUrl)[1] || '',
          AboutMe: this.profileForm.value.aboutMe,
          Work: this.profileForm.value.myWork,
          Education: this.profileForm.value.whereIWentToSchool,
          Languages: this.profileForm.value.languages,
          LivesIn: this.profileForm.value.whereILive,
          DreamDestination: this.profileForm.value.whereIveAlwaysWantedToGo,
          FunFact: this.profileForm.value.funFact,
          Pets: this.profileForm.value.pets,
          ObsessedWith: this.profileForm.value.obsessedWith,
          SpecialAbout: this.profileForm.value.specialAbout,
        };
      }

      this.profileService.updateProfile(profileData).subscribe({
        next: () => {
          this.router.navigate(['/home']);
        },
        error: (error) => {
          console.error('Error updating profile', error);
        }
      });
    }
  }
}
