using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(int userId)
        {
            return await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.user1Id == userId || c.user2Id == userId)
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.SentAt) : c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Conversation> GetConversationAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation> GetOrCreateConversationAsync(int userId1, int userId2)
        {
            // Check if conversation already exists
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.user1Id == userId1 && c.user2Id == userId2) ||
                    (c.user1Id == userId2 && c.user2Id == userId1));

            if (conversation != null)
                return conversation;

            // Create new conversation
            var newConversation = new Conversation
            {
                user1Id = userId1,
                user2Id = userId2,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(newConversation);
            await _context.SaveChangesAsync();

            return newConversation;
        }

        public async Task<List<Message>> GetConversationMessagesAsync(int conversationId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<Message> SaveMessageAsync(int conversationId, int senderId, string content)
        {
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Load the sender to return a complete message object
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync();

            return message;
        }

        public async Task MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message != null &&
                (message.Conversation.user1Id == userId || message.Conversation.user2Id == userId) &&
                message.SenderId != userId)
            {
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
