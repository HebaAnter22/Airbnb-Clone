using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(b => b.PropertyId).IsRequired().HasColumnName("property_id");
            builder.Property(b => b.GuestId).IsRequired().HasColumnName("guest_id");
            builder.Property(b => b.StartDate).IsRequired().HasColumnType("date").HasColumnName("start_date");
            builder.Property(b => b.EndDate).IsRequired().HasColumnType("date").HasColumnName("end_date");
            builder.Property(b => b.Status).IsRequired().HasMaxLength(20).HasColumnName("status").HasConversion<string>().HasDefaultValue(BookingStatus.Pending.ToString());
            builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");
            builder.Property(b => b.UpdatedAt).HasColumnType("datetime").HasColumnName("updated_at");
            builder.Property(b => b.CheckInStatus).HasMaxLength(20).HasColumnName("check_in_status");
            builder.Property(b => b.CheckOutStatus).HasMaxLength(20).HasColumnName("check_out_status");
            builder.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)").HasColumnName("total_amount");
            builder.Property(b => b.PromotionId).HasColumnName("promotion_id").HasDefaultValue(0);

            builder.HasCheckConstraint("CK_Bookings_Status", "[status] IN ('confirmed', 'denied', 'pending', 'cancelled', 'completed')");
            builder.HasCheckConstraint("CK_Bookings_CheckInStatus", "[check_in_status] IN ('pending', 'completed')");
            builder.HasCheckConstraint("CK_Bookings_CheckOutStatus", "[check_out_status] IN ('pending', 'completed')");

            builder.HasOne(b => b.Property)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Guest)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            
        }
    }
}
