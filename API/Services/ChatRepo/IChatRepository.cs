using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Services
{
    public interface IChatService
    {
        Task<List<Conversation>> GetUserConversationsAsync(int userId);
        Task<Conversation> GetConversationAsync(int conversationId);
        Task<Conversation> GetOrCreateConversationAsync(int userId1, int userId2);
        Task<List<Message>> GetConversationMessagesAsync(int conversationId);
        Task<Message> SaveMessageAsync(int conversationId, int senderId, string content);
        Task MarkMessageAsReadAsync(int messageId, int userId);
    }
}