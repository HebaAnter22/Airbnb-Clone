// auth.service.ts
import { Injectable } from '@angular/core';
import {
  Auth,
  createUserWithEmailAndPassword,
  sendEmailVerification,
  signInWithEmailAndPassword,
  User,
  authState,
  sendPasswordResetEmail,
  updateProfile
} from '@angular/fire/auth';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FirebaseAuthService {
  currentUser$: Observable<User | null>;

  constructor(private auth: Auth) {
    this.currentUser$ = authState(this.auth);
  }

  // Method to check if user exists and send verification email
  async sendVerificationEmail(email: string, password: string): Promise<boolean> {
    try {
      // First check if the user exists - try to sign in
      try {
        await signInWithEmailAndPassword(this.auth, email, password);
        // If we get here, user exists, send verification email
        if (this.auth.currentUser) {
          await sendEmailVerification(this.auth.currentUser);
          return true;
        }
      } catch (err) {
        // User doesn't exist, create new account
        const userCredential = await createUserWithEmailAndPassword(this.auth, email, password);
        await sendEmailVerification(userCredential.user);
        return true;
      }
    } catch (error) {
      console.error('Error sending verification email:', error);
      return false;
    }
    return false;
  }

  // Method to send just a verification email to the current user
  async resendVerificationEmail(): Promise<boolean> {
    try {
      if (this.auth.currentUser) {
        await sendEmailVerification(this.auth.currentUser);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Error resending verification email:', error);
      return false;
    }
  }

  // Check if email is verified
  isEmailVerified(): boolean {
    return this.auth.currentUser?.emailVerified || false;
  }

  // Sign out
  async signOut(): Promise<void> {
    return await this.auth.signOut();
  }

  // Get current user
  getCurrentUser(): User | null {
    return this.auth.currentUser;
  }

  // Update user profile
  async updateUserProfile(displayName: string): Promise<void> {
    if (this.auth.currentUser) {
      await updateProfile(this.auth.currentUser, { displayName });
    }
  }
}
