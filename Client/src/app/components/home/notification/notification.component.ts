import { Component, HostListener, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { interval, Subscription, switchMap } from 'rxjs';
import { NotificationService, Notification } from '../../../services/notification.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification.component.html',
  styleUrl: './notification.component.css'
})
export class NotificationComponent implements OnInit, OnDestroy {
  isNotificationOpen: boolean = false;
  notificationCount: number = 0;
  notifications: Notification[] = [];

  private notificationSubscription: Subscription | null = null;
  private pollingSubscription: Subscription | null = null;
  isDropdownOpen: boolean = false;
  loggedIn: boolean = false;

  constructor(
    private notificationService: NotificationService,
    private authService: AuthService
  ) {
    if (this.authService.userId) {
      this.loggedIn = true;
    }
  }

  ngOnInit() {
    if (this.loggedIn) {
      // Initial fetch of notifications
      this.loadNotifications();

      // Set up polling for notifications (every 30 seconds)
      this.pollingSubscription = interval(30000).pipe(
        switchMap(() => this.notificationService.getUnreadCount())
      ).subscribe({
        next: (result) => {
          this.notificationCount = result.unreadCount;
        },
        error: (error) => {
          console.error('Error fetching notification count:', error);
        }
      });
    }
  }

  ngOnDestroy() {
    // Clean up subscriptions
    if (this.notificationSubscription) {
      this.notificationSubscription.unsubscribe();
    }
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }

  loadNotifications() {
    // Load user notifications
    this.notificationSubscription = this.notificationService.getUserNotifications().subscribe({
      next: (data) => {
        this.notifications = data;
        this.updateNotificationCount();
      },
      error: (error) => {
        if (error.status === 404) {
          console.warn('No notifications found');
          this.notifications = [];

        } else {
          console.error('Error fetching notifications:', error);
        }
      }
    });
  }

  toggleDropdown(event: Event) {
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  toggleNotifications(event: Event) {
    event.stopPropagation();
    this.isNotificationOpen = !this.isNotificationOpen;

    // If opening notifications, refresh the list
    if (this.isNotificationOpen) {
      this.loadNotifications();
    }

    if (this.isDropdownOpen) {
      this.isDropdownOpen = false;
    }
  }

  updateNotificationCount() {
    this.notificationCount = this.notifications.filter(notification => !notification.isRead).length;
  }

  markAllAsRead() {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        // Update local notifications
        this.notifications.forEach(notification => {
          notification.isRead = true;
        });
        this.updateNotificationCount();
      },
      error: (error) => {
        console.error('Error marking notifications as read:', error);
      }
    });
  }

  markAsRead(notificationId: number) {
    this.notificationService.markAsRead(notificationId).subscribe({
      next: () => {
        // Update local notification
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification) {
          notification.isRead = true;
          this.updateNotificationCount();
        }
      },
      error: (error) => {
        console.error(`Error marking notification ${notificationId} as read:`, error);
      }
    });
  }

  formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.round(diffMs / 1000);
    const diffMin = Math.round(diffSec / 60);
    const diffHour = Math.round(diffMin / 60);
    const diffDay = Math.round(diffHour / 24);

    if (diffSec < 60) {
      return 'just now';
    } else if (diffMin < 60) {
      return `${diffMin} minute${diffMin === 1 ? '' : 's'} ago`;
    } else if (diffHour < 24) {
      return `${diffHour} hour${diffHour === 1 ? '' : 's'} ago`;
    } else if (diffDay < 30) {
      return `${diffDay} day${diffDay === 1 ? '' : 's'} ago`;
    } else {
      return date.toLocaleDateString();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    const target = event.target as HTMLElement;

    // Handle notification dropdown clicks
    if (this.isNotificationOpen && !target.closest('.notification-container')) {
      this.isNotificationOpen = false;
    }
  }
}
