using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(500);
            builder.Property(n => n.IsRead)
                .HasDefaultValue(false);
            builder.Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
            builder.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(n => n.Sender)
                .WithMany(u => u.NotificationsSent)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
