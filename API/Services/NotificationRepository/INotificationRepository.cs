using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.NotificationRepository
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetNotificationsForUserAsync(int userId);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task CreateNotificationAsync(Notification notification);
        Task<IEnumerable<Notification>> GetUnreadNotificationsForUserAsync(int userId);
        Task<int> GetUnreadNotificationCountAsync(int userId);
        Task MarkAllNotificationsAsReadAsync(int userId);
    }
}
