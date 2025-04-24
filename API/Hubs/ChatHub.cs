using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using API.Models;
using API.Services;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(int conversationId, int senderId, string message)
        {
            // Save message to database
            var savedMessage = await _chatService.SaveMessageAsync(conversationId, senderId, message);

            // Broadcast message to all clients in the conversation group
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", savedMessage);
        }

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task MarkAsRead(int messageId, int userId)
        {
            await _chatService.MarkMessageAsReadAsync(messageId, userId);
            await Clients.Group(Context.ConnectionId).SendAsync("MessageRead", messageId);
        }
    }
}