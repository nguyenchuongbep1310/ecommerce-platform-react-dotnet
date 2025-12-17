using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace NotificationService.Hubs
{
    public class OrderHub : Hub
    {
        // This method can be called by the client to join a specific group (e.g., their UserId)
        public async Task JoinGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task LeaveGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
    }
}
