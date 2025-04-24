using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(c => c.PropertyId).HasColumnName("property_id");
            builder.Property(c => c.Subject).HasMaxLength(255).HasColumnName("subject");
            builder.Property(c => c.user1Id).IsRequired().HasColumnName("user1_id");
            builder.Property(c => c.user2Id).IsRequired().HasColumnName("user2_id");
            builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");
            builder.HasOne(c => c.Property)
                .WithMany(p => p.Conversations)
                .HasForeignKey(c => c.PropertyId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(c => c.User1)
                .WithMany(u => u.ConversationsAsUser1)
                .HasForeignKey(c => c.user1Id)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(c => c.User2)
                .WithMany(u => u.ConversationsAsUser2)
                .HasForeignKey(c => c.user2Id)
                .OnDelete(DeleteBehavior.NoAction);

        }
    }

}
