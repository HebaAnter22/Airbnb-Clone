// src/app/services/chat.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { HttpTransportType, HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface Message {
    id: number;
    conversationId: number;
    senderId: number;
    content: string;
    sentAt: Date;
    readAt: Date | null;
    sender: any;
}

export interface Conversation {
    id: number;
    propertyId?: number;
    subject?: string;
    user1Id: number;
    user2Id: number;
    createdAt: Date;
    user1: any;
    user2: any;
    messages: Message[];
}

@Injectable({
    providedIn: 'root'
})
export class ChatService {
    private baseUrl = 'https://localhost:7228';
    private hubConnection: HubConnection | null = null;
    private conversationsSubject: BehaviorSubject<Conversation[]> = new BehaviorSubject<Conversation[]>([]);
    public conversations$ = this.conversationsSubject.asObservable();

    private selectedConversationSubject: BehaviorSubject<Conversation | null> = new BehaviorSubject<Conversation | null>(null);
    public selectedConversation$ = this.selectedConversationSubject.asObservable();
    private unreadCountSubject = new BehaviorSubject<number>(0);
    public unreadCount$ = this.unreadCountSubject.asObservable();
    private messagesSubject: BehaviorSubject<Message[]> = new BehaviorSubject<Message[]>([]);
    public messages$ = this.messagesSubject.asObservable();

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }
    public initializeSignalRConnection(): void {
        if (this.hubConnection && this.hubConnection.state === 'Connected') {
            return;
        }

        // Stop existing connection if it's in a different state
        if (this.hubConnection) {
            this.hubConnection.stop().catch(err => console.error('Error stopping connection:', err));
            this.hubConnection = null;
        }

        this.hubConnection = new HubConnectionBuilder()
            .withUrl(`${this.baseUrl}/chatHub`, {
                accessTokenFactory: () => {
                    const token = this.authService.currentUserValue?.accessToken || '';
                    console.log('Using token:', token ? 'Token available' : 'No token');
                    return token;
                },
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.elapsedMilliseconds < 60000) {
                        return 2000; // Retry every 2 seconds for the first minute
                    }
                    return 60000; // After a minute, retry every minute
                }
            })
            .build();

        this.hubConnection.onclose(error => {
            console.log('Connection closed with error:', error);

            // Wait a moment before attempting to reconnect
            setTimeout(() => {
                if (this.authService.isLoggedIn) {
                    console.log('Attempting to reconnect...');
                    this.initializeSignalRConnection();
                }
            }, 5000);
        });

        this.hubConnection.start()
            .then(() => console.log('SignalR connection established'))
            .catch(err => {
                console.error('Error establishing SignalR connection:', err);
                // Retry after 5 seconds
                setTimeout(() => this.initializeSignalRConnection(), 5000);
            });

        this.setupSignalREvents();
    }







    private setupSignalREvents(): void {
        if (!this.hubConnection) return;

        this.hubConnection.on('ReceiveMessage', (message: Message) => {
            console.log('Received message:', message);
            // Update messages array if this is for the current conversation
            if (this.selectedConversationSubject.value?.id === message.conversationId) {
                const currentMessages = this.messagesSubject.value;

                // Find if we have a temporary message with ID -1 that matches this message's content
                const tempMessageIndex = currentMessages.findIndex(m =>
                    m.id === -1 &&
                    m.senderId === message.senderId &&
                    m.content === message.content
                );

                let updatedMessages = [...currentMessages];

                if (tempMessageIndex >= 0) {
                    // Replace the temporary message with the real one
                    updatedMessages[tempMessageIndex] = message;
                } else if (!currentMessages.some(m => m.id === message.id)) {
                    // If no temporary message found and not a duplicate, add it
                    updatedMessages = [...updatedMessages, message];
                }

                this.messagesSubject.next(updatedMessages);
            }

            // Update conversation list to show latest message
            this.updateConversationWithLatestMessage(message);

            // After receiving a new message, get the latest conversations
            // to ensure unread counts are up-to-date
            this.getUserConversations().subscribe({
                next: () => { },
                error: (err) => console.error('Error updating conversations after message:', err)
            });
        });
        this.hubConnection.on('UpdateUnreadCount', (count: number) => {
            this.unreadCountSubject.next(count);
        });

        this.hubConnection.on('MessageRead', (messageId: number) => {
            // Update the message's read status
            const currentMessages = this.messagesSubject.value;
            const updatedMessages = currentMessages.map(msg =>
                msg.id === messageId ? { ...msg, readAt: new Date() } : msg
            );
            this.messagesSubject.next(updatedMessages);

            // Update conversations to refresh unread status
            this.getUserConversations().subscribe();
        });

        this.hubConnection.on('MessageError', (errorMessage: string) => {
            console.error('Message error from server:', errorMessage);
        });
    }


    public triggerUnreadCountUpdate(): void {
        this.getUnreadCount().subscribe(count => {
            this.unreadCountSubject.next(count);
        });
    }




    // In src/app/services/chat.service.ts
    // Update the setupSignalREvents method:


    // Update the updateConversationWithLatestMessage method:
    private updateConversationWithLatestMessage(message: Message): void {
        const conversations = this.conversationsSubject.value;
        const conversationIndex = conversations.findIndex(c => c.id === message.conversationId);

        if (conversationIndex !== -1) {
            const updatedConversations = [...conversations];

            // Check if the conversation already has this message to avoid duplicates
            if (!updatedConversations[conversationIndex].messages.some(m => m.id === message.id)) {
                updatedConversations[conversationIndex] = {
                    ...updatedConversations[conversationIndex],
                    messages: [message]  // Keep just the latest message for preview
                };

                // Sort conversations to put the one with the new message at the top
                updatedConversations.sort((a, b) => {
                    const aDate = a.messages.length > 0 ? new Date(a.messages[0].sentAt).getTime() : new Date(a.createdAt).getTime();
                    const bDate = b.messages.length > 0 ? new Date(b.messages[0].sentAt).getTime() : new Date(b.createdAt).getTime();
                    return bDate - aDate;
                });

                this.conversationsSubject.next(updatedConversations);
            }
        }
    }

    // Improve sendMessage method to handle optimistic UI updates
    // In src/app/services/chat.service.ts
    public sendMessage(conversationId: number, content: string): Promise<void> {
        return new Promise((resolve, reject) => {
            this.ensureConnection()
                .then(() => {
                    const senderId = Number(this.authService.userId);

                    // Create a temporary message for optimistic UI update
                    const tempMessage: Message = {
                        id: -1, // Temporary ID that will be replaced
                        conversationId: conversationId,
                        senderId: senderId,
                        content: content,
                        sentAt: new Date(),
                        readAt: null,
                        sender: null // We don't have the sender object here
                    };

                    // Update UI immediately for better user experience
                    if (this.selectedConversationSubject.value?.id === conversationId) {
                        const currentMessages = this.messagesSubject.value;
                        this.messagesSubject.next([...currentMessages, tempMessage]);
                    }

                    // Then send the actual message
                    return this.hubConnection!.invoke('SendMessage', conversationId, senderId, content);
                })
                .then(() => {
                    console.log('Message sent successfully');
                    resolve();
                })
                .catch(err => {
                    console.error('Error in sendMessage:', err);

                    // Remove the temporary message from UI on error
                    if (this.selectedConversationSubject.value?.id === conversationId) {
                        const currentMessages = this.messagesSubject.value;
                        this.messagesSubject.next(currentMessages.filter(m => m.id !== -1));
                    }

                    // Try to reconnect if connection was lost
                    if (this.hubConnection?.state !== 'Connected') {
                        this.initializeSignalRConnection();
                    }

                    reject(err);
                });
        });
    }








    public stopConnection(): void {
        if (this.hubConnection) {
            this.hubConnection.stop()
                .then(() => {
                    console.log('SignalR connection stopped');
                    this.hubConnection = null;
                })
                .catch(err => console.error('Error stopping SignalR connection:', err));
        }
    }

    public joinConversation(conversationId: number): Observable<Message[]> {
        if (this.hubConnection && this.hubConnection.state === 'Connected') {
            this.hubConnection.invoke('JoinConversation', conversationId)
                .catch(err => console.error('Error joining conversation:', err));
        }

        return this.getConversationMessages(conversationId);
    }

    public leaveConversation(conversationId: number): void {
        if (this.hubConnection && this.hubConnection.state === 'Connected') {
            this.hubConnection.invoke('LeaveConversation', conversationId)
                .catch(err => console.error('Error leaving conversation:', err));
        }
    }

    // In chat.service.ts
    public getUserConversations(): Observable<Conversation[]> {
        return this.http.get<Conversation[]>(`${this.baseUrl}/api/Chat/conversations`)
            .pipe(
                map(conversations => {
                    // Filter out conversations with no messages where current user is user2
                    const filtered = conversations.filter(conv =>
                        conv.messages.length > 0 ||
                        conv.user1Id === Number(this.authService.userId)
                    );
                    console.log('Filtered conversations:', filtered);
                    this.conversationsSubject.next(filtered);
                    return filtered;
                })
            );
    }
    // In chat.service.ts
    public hasUnreadMessages(conversation: Conversation): boolean {
        if (!conversation.messages || conversation.messages.length === 0) {
            return false;
        }

        const currentUserId = Number(this.authService.userId);
        return conversation.messages.some(m =>
            m.senderId !== currentUserId && !m.readAt
        );
    }

    // In chat.service.ts, modify getUnreadCount() to ensure connection
    public getUnreadCount(): Observable<number> {
        // Initialize connection if not already connected
        if (!this.hubConnection) {
            this.initializeSignalRConnection();
        }

        return this.conversations$.pipe(
            map(conversations => conversations.reduce((count, conv) =>
                count + (this.hasUnreadMessages(conv) ? 1 : 0), 0))
        );
    }
    public getConversation(conversationId: number): Observable<Conversation> {
        return this.http.get<Conversation>(`${this.baseUrl}/api/Chat/conversations/${conversationId}`)
            .pipe(
                map(conversation => {
                    this.selectedConversationSubject.next(conversation);
                    return conversation;
                })
            );
    }

    public getConversationMessages(conversationId: number): Observable<Message[]> {
        return this.http.get<Message[]>(`${this.baseUrl}/api/Chat/conversations/${conversationId}/messages`)
            .pipe(
                map(messages => {
                    this.messagesSubject.next(messages);
                    return messages;
                })
            );
    }

    public createConversation(otherUserId: number): Observable<Conversation> {
        return this.http.post<Conversation>(`${this.baseUrl}/api/Chat/conversations?otherUserId=${otherUserId}`, {})
            .pipe(
                map(conversation => {
                    const currentConversations = this.conversationsSubject.value;
                    this.conversationsSubject.next([conversation, ...currentConversations]);
                    this.selectedConversationSubject.next(conversation);
                    return conversation;
                })
            );
    }

    public ensureConnection(): Promise<void> {
        if (!this.hubConnection) {
            this.initializeSignalRConnection();

            // Give time for connection to establish
            return new Promise((resolve) => {
                setTimeout(() => resolve(), 1000);
            });
        }

        if (this.hubConnection.state === 'Connected') {
            return Promise.resolve();
        }

        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                reject('Connection attempt timed out');
            }, 5000);

            this.hubConnection!.start()
                .then(() => {
                    clearTimeout(timeout);
                    resolve();
                })
                .catch(err => {
                    clearTimeout(timeout);
                    reject(err);
                });
        });
    }


    public markMessageAsRead(messageId: number): void {
        if (this.hubConnection && this.hubConnection.state === 'Connected') {
            const userId = Number(this.authService.userId);
            this.hubConnection.invoke('MarkAsRead', messageId, userId)
                .catch(err => console.error('Error marking message as read:', err));
        }
    }

    public setSelectedConversation(conversation: Conversation | null): void {
        this.selectedConversationSubject.next(conversation);
    }

    public getOtherUser(conversation: Conversation): any {
        const currentUserId = Number(this.authService.userId);
        console.log(currentUserId, conversation.user1Id, conversation.user2Id);
        return conversation.user1Id === currentUserId ? conversation.user2 : conversation.user1;
    }

}