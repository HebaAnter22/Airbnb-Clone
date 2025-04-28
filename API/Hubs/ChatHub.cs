//using Microsoft.AspNetCore.SignalR;
//using System.Threading.Tasks;
//using API.Models;
//using API.Services;
//using System.Security.Claims;

//namespace API.Hubs
//{
//    public class ChatHub : Hub
//    {
//        private readonly IChatService _chatService;

//        public ChatHub(IChatService chatService)
//        {
//            _chatService = chatService;
//        }

//        public override async Task OnConnectedAsync()
//        {
//            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
//            if (userId != 0)
//            {
//                // Get initial unread count and send to client
//                var unreadCount = await _chatService.GetUnreadCountAsync(userId);
//                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
//            }
//            await base.OnConnectedAsync();
//        }
//        public async Task SendMessage(int conversationId, int senderId, string message)
//        {
//            try
//            {
//                // Verify the sender matches the authenticated user
//                var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

//                // Better null checking
//                if (Context.User == null || userId == 0)
//                {
//                    throw new HubException("Unauthorized: User not authenticated");
//                }

//                if (userId != senderId)
//                {
//                    throw new HubException("Unauthorized: User ID mismatch");
//                }

//                // Save message to database
//                var savedMessage = await _chatService.SaveMessageAsync(conversationId, senderId, message);

//                // Broadcast message to all clients in the conversation group
//                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", savedMessage);


//                var conversation = await _chatService.GetConversationAsync(conversationId);
//                var recipientId = conversation.user1Id == senderId ? conversation.user2Id : conversation.user1Id;
//                var unreadCount = await _chatService.GetUnreadCountAsync(recipientId);

//                await Clients.User(recipientId.ToString()).SendAsync("UpdateUnreadCount", unreadCount);
//            }
//            catch (Exception ex)
//            {
//                // Log the error but don't throw it back to the client
//                Console.WriteLine($"Error in SendMessage: {ex}");

//                // Send a more graceful error to the client
//                await Clients.Caller.SendAsync("MessageError", "Failed to send message. Please try again.");
//            }
//        }
//        public async Task SendTypingNotification(int conversationId, int userId, bool isTyping)
//        {
//            try
//            {
//                var conversation = await _chatService.GetConversationAsync(conversationId);
//                if (conversation == null || (conversation.user1Id != userId && conversation.user2Id != userId))
//                {
//                    throw new HubException("Invalid conversation or user");
//                }

//                var otherUserId = conversation.user1Id == userId ? conversation.user2Id : conversation.user1Id;
//                await Clients.User(otherUserId.ToString())
//                    .SendAsync("ReceiveTypingNotification", conversationId, userId, isTyping);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error in SendTypingNotification: {ex}");
//                await Clients.Caller.SendAsync("TypingError", ex.Message);
//            }
//        }
//        public async Task NotifyNewConversation(Conversation conversation, int otherUserId)
//        {
//            await Clients.User(otherUserId.ToString())
//                .SendAsync("ReceiveNewConversation", conversation);
//        }
//        public async Task JoinConversation(int conversationId)
//        {
//            // Check if user is authorized to join this conversation
//            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
//            if (userId == 0)
//            {
//                throw new HubException("Unauthorized: User not authenticated");
//            }

//            // Add the client to the group
//            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            
//            Console.WriteLine($"User {userId} joined conversation {conversationId}");
          
//        }

//        public async Task LeaveConversation(int conversationId)
//        {
//            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
//        }

//        public async Task MarkAsRead(int messageId, int userId)
//        {
//            await _chatService.MarkMessageAsReadAsync(messageId, userId);
//            await Clients.Group(Context.ConnectionId).SendAsync("MessageRead", messageId);
//        }
//    }
//}


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

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task SendMessage(int conversationId, int senderId, string message)
        {
            var savedMessage = await _chatService.SaveMessageAsync(conversationId, senderId, message);

            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", savedMessage);
        }
        public async Task JoinUserGroup()
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{Context.UserIdentifier}");
}

public async Task NotifyUnreadCount(int count)
{
    await Clients.User(Context.UserIdentifier).SendAsync("UpdateUnreadCount", count);
}

        public async Task MarkAsRead(int messageId, int userId)
        {
            await _chatService.MarkMessageAsReadAsync(messageId, userId);
            await Clients.All.SendAsync("MessageRead", messageId);
        }
    }
}