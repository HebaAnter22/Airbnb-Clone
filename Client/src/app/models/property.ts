export interface PropertyImageDto {
  id: number;
  imageUrl: string;  // Changed from 'url' to match backend
  isPrimary: boolean;
  category?: string;  // Added to match backend
}

export interface PropertyDto {
  id: number;
  hostId?: number;
  title: string;
  description: string;
  propertyType: string;
  address: string;
  city: string;
  country?: string;  // Made optional
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  pricePerNight?: number;
  cleaningFee?: number;
  serviceFee?: number;
  minNights?: number;
  maxNights?: number;
  bedrooms?: number;
  bathrooms?: number;
  maxGuests?: number;
  status?: string;
  createdAt?: Date;
  updatedAt?: Date;
  isAvailable?: boolean;
  images: PropertyImageDto[];
  
  // Rating and favorite fields
  averageRating?: number;
  reviewCount?: number;
  favoriteCount?: number;
  isGuestFavorite?: boolean;
  
  // UI-only fields
  viewCount?: number;
  dates?: string;
}