using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class AmenitiesConfiguration : IEntityTypeConfiguration<Amenity>
    {
        public void Configure(EntityTypeBuilder<Amenity> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(a => a.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(a => a.Category).IsRequired().HasMaxLength(50).HasColumnName("category");
            builder.Property(a => a.IconUrl).IsRequired().HasMaxLength(255).HasColumnName("icon_url");

            builder.HasMany(a => a.Properties)
                .WithMany(p => p.Amenities)
                .UsingEntity(j => j.ToTable("PropertyAmenities"));
        }
    }
}
