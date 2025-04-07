using Microsoft.EntityFrameworkCore;
using API.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace API.Data.Configurations
{
    public class UsedPromotionsConfiguration : IEntityTypeConfiguration<UserUsedPromotion>
    {
        public void Configure(EntityTypeBuilder<UserUsedPromotion> builder)
        {
            builder.HasKey(up => up.Id);
            builder.Property(up => up.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(up => up.UserId).IsRequired().HasColumnName("user_id");
            builder.Property(up => up.PromotionId).IsRequired().HasColumnName("promotion_id");
            builder.Property(up => up.BookingId).IsRequired().HasColumnName("booking_id");
            builder.Property(up => up.DiscountedAmount).HasColumnType("decimal(18,2)").HasColumnName("discounted_amount");
            builder.Property(up => up.UsedAt).HasColumnType("datetime").HasDefaultValueSql("SYSDATETIME()").HasColumnName("used_at");

            builder.HasOne(up => up.User)
                .WithMany(u => u.UsedPromotions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(up => up.Promotion)
                .WithMany(p => p.UserUsedPromotions)
                .HasForeignKey(up => up.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(up => up.Booking)
                .WithOne(b => b.UsedPromotion)
                .HasForeignKey<UserUsedPromotion>(up => up.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
