using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data.Configurations
{
    public class PropertyConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(p => p.HostId).IsRequired().HasColumnName("host_id");
            builder.Property(p => p.CategoryId).HasColumnName("category_id");
            builder.Property(p => p.Title).IsRequired().HasMaxLength(255).HasColumnName("title");
            builder.Property(p => p.Description).HasMaxLength(1000).HasColumnName("description");
            builder.Property(p => p.PropertyType).IsRequired().HasMaxLength(50).HasColumnName("property_type");
            builder.Property(p => p.Country).IsRequired().HasMaxLength(50).HasColumnName("country");
            builder.Property(p => p.Address).IsRequired().HasMaxLength(100).HasColumnName("address");
            builder.Property(p => p.City).IsRequired().HasMaxLength(50).HasColumnName("city");
            builder.Property(p => p.PostalCode).HasMaxLength(20).HasColumnName("postal_code");
            builder.Property(p => p.Latitude).HasColumnType("decimal(9,6)").HasColumnName("latitude");
            builder.Property(p => p.Longitude).HasColumnType("decimal(9,6)").HasColumnName("longitude");
            builder.Property(p => p.Currency).IsRequired().HasMaxLength(10).HasColumnName("currency");
            builder.Property(p => p.PricePerNight).HasColumnType("decimal(18,2)").HasColumnName("price_per_night");
            builder.Property(p => p.CleaningFee).HasDefaultValue(0).HasColumnType("decimal(18,2)").HasColumnName("cleaning_fee");
            builder.Property(p => p.ServiceFee).HasDefaultValue(0).HasColumnType("decimal(18,2)").HasColumnName("service_fee");
            builder.Property(p => p.MinNights).HasDefaultValue(1).HasColumnName("min_nights");
            builder.Property(p => p.MaxNights).HasColumnName("max_nights");
            builder.Property(p => p.Bedrooms).HasDefaultValue(1).HasColumnName("bedrooms");
            builder.Property(p => p.Bathrooms).HasDefaultValue(1).HasColumnName("bathrooms");
            builder.Property(p => p.MaxGuests).HasDefaultValue(1).HasColumnName("max_guests");
            builder.Property(p => p.CheckInTime).HasColumnType("time").HasColumnName("check_in_time");
            builder.Property(p => p.CheckOutTime).HasColumnType("time").HasColumnName("check_out_time");
            builder.Property(p => p.InstantBook).HasDefaultValue(false).HasColumnName("instant_book");
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");
            builder.Property(p => p.UpdatedAt).HasColumnType("datetime").HasColumnName("updated_at");
            builder.Property(p => p.CancellationPolicyId).HasColumnName("cancellation_policy_id");
            builder.Property(p => p.Status).HasMaxLength(20).HasColumnName("status").HasConversion<string>().HasDefaultValue(PropertyStatus.Pending.ToString());


            builder.HasCheckConstraint("CK_Properties_Status", "[status] IN ('active', 'pending', 'suspended')");

            builder.HasOne(p => p.Host)
                .WithMany(h => h.Properties)
                .HasForeignKey(p => p.HostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Properties)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
            
            builder.HasOne(p => p.CancellationPolicy)
                .WithMany(c => c.Properties)
                .HasForeignKey(p => p.CancellationPolicyId)
                .OnDelete(DeleteBehavior.Cascade);



        }
    }
}
