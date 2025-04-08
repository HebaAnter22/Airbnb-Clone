using Microsoft.EntityFrameworkCore;
using API.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace API.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(m => m.ConversationId).IsRequired().HasColumnName("conversation_id");
            builder.Property(m => m.SenderId).IsRequired().HasColumnName("sender_id");
            builder.Property(m => m.Content).IsRequired().HasMaxLength(1000).HasColumnName("content");
            builder.Property(m => m.SentAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("sent_at");
            builder.Property(m => m.ReadAt).HasColumnType("datetime").HasColumnName("read_at");
            builder.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasIndex(m => m.ConversationId).HasDatabaseName("IX_ConversationId");
        }
    }
}
