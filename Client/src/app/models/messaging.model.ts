export interface Message {
  id: number;
  conversationId: number;
  senderId: number;
  content: string;
  sentAt: Date;
  readAt?: Date;
  sender?: {
    id: number;
    firstName: string;
    lastName: string;
    profilePictureUrl: string;
  };
}

export interface Conversation {
  id: number;
  propertyId?: number;
  subject?: string;
  user1Id: number;
  user2Id: number;
  createdAt: Date;
  user1?: {
    id: number;
    firstName: string;
    lastName: string;
    profilePictureUrl: string;
  };
  user2?: {
    id: number;
    firstName: string;
    lastName: string;
    profilePictureUrl: string;
  };
  messages?: Message[];
}