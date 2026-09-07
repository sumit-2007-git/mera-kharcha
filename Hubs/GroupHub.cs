using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MeraKharcha.Hubs
{
    public class GroupHub : Hub
    {
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{groupId}");
        }

        public async Task JoinUserNotifications(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
    }
}
