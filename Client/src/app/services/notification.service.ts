import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Notification {
    id: number;
    message: string;
    isRead: boolean;
    createdAt: string;
    senderName?: string;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private apiUrl = environment.apiUrl + '/notification';

    constructor(private http: HttpClient) { }

    getUserNotifications(): Observable<Notification[]> {
        return this.http.get<Notification[]>(`${this.apiUrl}/user-notifications`);
    }

    getUnreadNotifications(): Observable<Notification[]> {
        return this.http.get<Notification[]>(`${this.apiUrl}/unread-notifications`);
    }

    getUnreadCount(): Observable<{ unreadCount: number }> {
        return this.http.get<{ unreadCount: number }>(`${this.apiUrl}/unread-count`);
    }

    markAsRead(notificationId: number): Observable<any> {
        return this.http.post(`${this.apiUrl}/mark-as-read/${notificationId}`, {});
    }

    markAllAsRead(): Observable<any> {
        return this.http.post(`${this.apiUrl}/mark-all-as-read`, {});
    }
}
