using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(r => r.ReviewerId).IsRequired().HasColumnName("reviewer_id");
            builder.Property(r => r.BookingId).IsRequired().HasColumnName("booking_id");
            builder.Property(r => r.Rating).IsRequired().HasColumnName("rating");
            builder.Property(r => r.Comment).HasMaxLength(1000).HasColumnName("comment");
            builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSDATETIME()").HasColumnType("datetime").HasColumnName("created_at");
            builder.Property(r => r.UpdatedAt).HasColumnType("datetime").HasColumnName("updated_at");

            builder.HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Reviewer)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.NoAction);


        }
    }
}
