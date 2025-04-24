// messaging.model.ts
export interface User {
    id: number;
    name: string;
    email: string;
    profilePicture?: string;
    // Add other user properties as needed
  }
  
  export interface Property {
    id: number;
    title: string;
    // Add other property properties as needed
  }
  
  export interface Message {
    id: number;
    conversationId: number;
    senderId: number;
    content: string;
    sentAt: Date;
    readAt?: Date;
    
    // Navigation properties
    sender?: User;
    conversation?: Conversation;
  }
  
  export interface Conversation {
    id: number;
    user1Id: number;
    user2Id: number;
    propertyId?: number;
    subject?: string;
    createdAt: Date;
    
    // Navigation properties
    user1: User;
    user2: User;
    property?: Property;
    messages: Message[];
  }
  
  // Optional: Create classes if you need methods
  export class MessageModel implements Message {
    constructor(
      public id: number,
      public conversationId: number,
      public senderId: number,
      public content: string,
      public sentAt: Date,
      public readAt?: Date,
      public sender?: User,
      public conversation?: Conversation
    ) {}
  
    get isRead(): boolean {
      return !!this.readAt;
    }
  
    get timeAgo(): string {
      // Implement your time ago logic or use date-fns
      return '';
    }
  }
  
  export class ConversationModel implements Conversation {
    constructor(
      public id: number,
      public user1Id: number,
      public user2Id: number,
      public createdAt: Date,
      public user1: User,
      public user2: User,
      public messages: Message[] = [],
      public propertyId?: number,
      public subject?: string,
      public property?: Property
    ) {}
  
    get lastMessage(): Message | undefined {
      return this.messages.length > 0 
        ? this.messages.reduce((latest, current) => 
            new Date(current.sentAt) > new Date(latest.sentAt) ? current : latest)
        : undefined;
    }
  
    get otherUser(): User {
      // You'll need to implement this based on your current user context
      return this.user1; // Placeholder
    }
  }