﻿using API.Models;

namespace API.DTOs.Chat
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public User User1 { get; set; }
        public User User2 { get; set; }
        public List<Message> Messages { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
