using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class PropertyAvailabilityConfiguration : IEntityTypeConfiguration<PropertyAvailability>
    {
        public void Configure(EntityTypeBuilder<PropertyAvailability> builder)
        {
            builder.HasKey(pa => pa.Id);
            builder.Property(pa => pa.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(pa => pa.PropertyId).IsRequired().HasColumnName("property_id");
            builder.Property(pa => pa.Date).IsRequired().HasColumnType("date").HasColumnName("date");
            builder.Property(pa => pa.IsAvailable).HasDefaultValue(true).HasColumnName("is_available");
            builder.Property(pa => pa.BlockedReason).HasMaxLength(255).HasColumnName("blocked_reason");
            builder.Property(pa => pa.Price).HasColumnType("decimal(18,2)").HasColumnName("price");
            builder.Property(pa => pa.MinNights).IsRequired().HasDefaultValue(1).HasColumnName("min_nights");

            builder.HasOne(pa => pa.Property)
                .WithMany(p => p.Availabilities)
                .HasForeignKey(pa => pa.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
