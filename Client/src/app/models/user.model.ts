// src/app/models/user.model.ts
export interface User {
    role: string;
    accessToken: string;
    refreshToken: string;
    email?: string;
    firstName?: string;
    lastName?: string;
    imageUrl?: string;
  }
  
  export interface TokenResponse {
    accessToken: string;
    refreshToken: string;
  }
  // models/user.model.ts
export interface UserProfile {
    username: string;
    role: string;
    // Add any other profile-related fields you store
    email?: string;
    firstName?: string;
    lastName?: string;
    avatar?: string;
  }
  