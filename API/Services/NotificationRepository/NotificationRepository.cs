using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.NotificationRepository
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        private readonly AppDbContext _context;
        public NotificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications.Include(n=>n.Sender).Where(n => n.UserId == userId).ToListAsync();
        }
        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
        public async Task CreateNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Notification>> GetUnreadNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications.Include(n=>n.Sender).Include(n=>n.User).Where(n => n.UserId == userId && n.IsRead == false).ToListAsync();
        }
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead == false);
        }
        public async Task MarkAllNotificationsAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
        
    }
}
