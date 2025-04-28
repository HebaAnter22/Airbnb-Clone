export interface PropertyImageDto {
  id: number;
  imageUrl: string;  // Changed from 'url' to match backend
  isPrimary: boolean;
  category?: string;  // Added to match backend
}

export interface Amenity {
  id: number;
  name: string;
  category: string;
  iconUrl: string;
}

export interface CancellationPolicy {
  id: number;
  name: string;
  description: string;
  refundPercentage: number;
}

export interface PropertyDto {
  id: number;
  hostId?: number;
  title: string;
  cancellationPolicyId: number;
  cancellationPolicy?: CancellationPolicy;
  description: string;
  propertyType: string;
  address: string;
  categoryId: number;
  city: string;
  country?: string;  // Made optional
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
  amenities?: Amenity[];  // Added amenities array
  
  // Rating and favorite fields
  averageRating?: number;
  reviewCount?: number;
  favoriteCount?: number;
  isGuestFavorite?: boolean;
  
  // UI-only fields
  viewCount?: number;
  dates?: string;
  currentImageIndex?: number; // Optional property for carousel functionality
  instantBook?: boolean; // Added instantBook property
}

export interface PropertyCategory {
  categoryId: number;
  name: string;
  description?: string;
  iconUrl: string;
}