using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Hubs;
using MeraKharcha.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type = "info", string? linkUrl = null);
        Task NotifyGroupAsync(int groupId, string eventName, object data);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<GroupHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<GroupHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type = "info", string? linkUrl = null)
        {
            var notification = new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.InAppNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // Real-time broadcast to user's SignalR channel
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                linkUrl = notification.LinkUrl,
                createdAt = notification.CreatedAt.ToString("g")
            });
        }

        public async Task NotifyGroupAsync(int groupId, string eventName, object data)
        {
            await _hubContext.Clients.Group($"Group_{groupId}").SendAsync(eventName, data);
        }
    }

    public interface IActivityLogger
    {
        Task LogAsync(int? groupId, string? userId, string action, string description);
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int? groupId, string? userId, string action, string description)
        {
            var log = new ActivityLog
            {
                GroupId = groupId,
                UserId = userId,
                Action = action,
                Description = description,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
