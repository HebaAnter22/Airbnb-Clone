using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<Conversation>>> GetUserConversations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var conversations = await _chatService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }

        [HttpGet("conversations/{conversationId}")]
        public async Task<ActionResult<Conversation>> GetConversation(int conversationId)
        {
            var conversation = await _chatService.GetConversationAsync(conversationId);
            if (conversation == null)
                return NotFound();

            return Ok(conversation);
        }

        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<ActionResult<List<Message>>> GetConversationMessages(int conversationId)
        {
            var messages = await _chatService.GetConversationMessagesAsync(conversationId);
            return Ok(messages);
        }

        [HttpPost("conversations")]
        public async Task<ActionResult<Conversation>> CreateConversation([FromQuery]int otherUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var conversation = await _chatService.GetOrCreateConversationAsync(userId, otherUserId);
            return Ok(conversation);
        }
    }
}