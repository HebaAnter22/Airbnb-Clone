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
  updateProfile,
  applyActionCode,
  confirmPasswordReset,
  verifyPasswordResetCode,
  ActionCodeSettings
} from '@angular/fire/auth';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FirebaseAuthService {
  currentUser$: Observable<User | null>;

  private actionCodeSettings: ActionCodeSettings = {
    url: 'http://localhost:4200/reset-password',
    handleCodeInApp: true,
  };

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
  async sendPasswordResetEmail(email: string): Promise<boolean> {
    try {
      await sendPasswordResetEmail(this.auth, email,
        this.actionCodeSettings
      );
      return true;
    } catch (error: any) {
      console.error('Error sending password reset email:', error);
      if (error.code === 'auth/user-not-found') {
        throw new Error('No account exists with this email address.');
      }
      return false;
    }
  }
  async verifyPasswordResetCode(code: string): Promise<string> {
    try {
      return await verifyPasswordResetCode(this.auth, code);
    } catch (error) {
      console.error('Invalid or expired action code', error);
      throw error;
    }
  }
  async confirmPasswordReset(code: string, newPassword: string): Promise<boolean> {
    try {
      await confirmPasswordReset(this.auth, code, newPassword);

      return true;
    } catch (error) {
      console.error('Error confirming password reset:', error);
      return false;
    }
  }
  async verifyEmail(actionCode: string): Promise<boolean> {
    try {
      await applyActionCode(this.auth, actionCode);
      return true;
    } catch (error) {
      console.error('Error verifying email:', error);
      return false;
    }
  }

}
