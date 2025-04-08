// services/profile.service.ts
import { Injectable } from '@angular/core';
import { UserProfile } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly STORAGE_KEY = 'currentUser';

  getProfile(): UserProfile | null {
    const userData = localStorage.getItem(this.STORAGE_KEY);
    return userData ? JSON.parse(userData) : null;
  }

  updateProfile(updatedProfile: Partial<UserProfile>): boolean {
    const currentProfile = this.getProfile();
    if (!currentProfile) return false;

    const newProfile = { ...currentProfile, ...updatedProfile };
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(newProfile));
    return true;
  }

  clearProfile(): void {
    localStorage.removeItem(this.STORAGE_KEY);
  }
}