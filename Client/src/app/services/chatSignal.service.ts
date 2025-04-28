import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, firstValueFrom, Observable, Subject } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Conversation, Message } from '../models/messaging.model';


@Injectable({
    providedIn: 'root'
})
export class ChatSignalRService {
    private hubConnection!: HubConnection;
    private baseUrl = 'https://localhost:7228/api/Chat';
    private messageReceivedSubject = new Subject<Message>();
    private messageReadSubject = new Subject<number>();
    private unreadCountSubject = new BehaviorSubject<number>(0);

    public messageReceived$ = this.messageReceivedSubject.asObservable();
    public messageRead$ = this.messageReadSubject.asObservable();
    public unreadCount$ = this.unreadCountSubject.asObservable();

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    public startConnection(): Promise<void> {
        this.hubConnection = new HubConnectionBuilder()
            .withUrl('https://localhost:7228/chatHub', {
                accessTokenFactory: () => this.authService.currentUserValue?.accessToken || ''
            })
            .withAutomaticReconnect()
            .build();

        this.registerOnServerEvents();

        return this.hubConnection.start();
    }

    public stopConnection(): Promise<void> {
        return this.hubConnection?.stop();
    }

    private registerOnServerEvents(): void {
        this.hubConnection.on('ReceiveMessage', (message: Message) => {
            this.messageReceivedSubject.next(message);
            // If message is not from current user, increment unread count
            if (message.senderId !== this.authService.getCurrentUserId()) {
                this.unreadCountSubject.next(this.unreadCountSubject.value + 1);
            }
        });

        this.hubConnection.on('MessageRead', (messageId: number) => {
            this.messageReadSubject.next(messageId);
        });
        this.hubConnection.on('UpdateUnreadCount', (count: number) => {
            this.unreadCountSubject.next(count);
        });
    }
    public async joinUserGroup(): Promise<void> {
        await this.hubConnection.invoke('JoinUserGroup');
    }

    public async loadUnreadCountUsingSignalR(): Promise<void> {
        const count = await firstValueFrom(
            this.http.get<number>(`${this.baseUrl}/unread-count`)
        );
        this.unreadCountSubject.next(count);
        await this.hubConnection.invoke('NotifyUnreadCount', count);
    }



    public joinConversation(conversationId: number): Promise<void> {
        return this.hubConnection.invoke('JoinConversation', conversationId);
    }

    public leaveConversation(conversationId: number): Promise<void> {
        return this.hubConnection.invoke('LeaveConversation', conversationId);
    }

    public sendMessage(conversationId: number, content: string): Promise<void> {
        const senderId = this.authService.getCurrentUserId();
        return this.hubConnection.invoke('SendMessage', conversationId, senderId, content);
    }

    public markAsRead(messageId: number): Promise<void> {
        const userId = this.authService.getCurrentUserId();
        return this.hubConnection.invoke('MarkAsRead', messageId, userId);
    }

    // API methods for data loading
    public getUserConversations(): Observable<Conversation[]> {
        return this.http.get<Conversation[]>(`${this.baseUrl}/conversations`);
    }

    public getConversation(conversationId: number): Observable<Conversation> {
        return this.http.get<Conversation>(`${this.baseUrl}/conversations/${conversationId}`);
    }

    public getConversationMessages(conversationId: number): Observable<Message[]> {
        return this.http.get<Message[]>(`${this.baseUrl}/conversations/${conversationId}/messages`);
    }

    public createConversation(otherUserId: number): Observable<Conversation> {
        return this.http.post<Conversation>(`${this.baseUrl}/conversations?otherUserId=${otherUserId}`, {});
    }

    public async loadUnreadCount(): Promise<void> {
        const userId = this.authService.getCurrentUserId();
        this.http.get<number>(`${this.baseUrl}/unread-count`).subscribe(count => {
            this.unreadCountSubject.next(count);
        });
    }
}