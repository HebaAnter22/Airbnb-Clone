using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
    {
        public void Configure(EntityTypeBuilder<PropertyImage> builder)
        {
            builder.HasKey(pi => pi.Id);
            builder.Property(pi => pi.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(pi => pi.PropertyId).IsRequired().HasColumnName("property_id");
            builder.Property(pi => pi.ImageUrl).IsRequired().HasMaxLength(255).HasColumnName("image_url");
            builder.Property(pi => pi.Description).HasMaxLength(255).HasColumnName("description");
            builder.Property(pi => pi.IsPrimary).HasDefaultValue(false).HasColumnName("is_primary");
            builder.Property(pi => pi.Category).HasMaxLength(50).HasColumnName("category");
            builder.Property(pi => pi.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");

            builder.HasCheckConstraint("CK_PropertyImages_Category", "[category] IN ('Bedroom', 'Bathroom', 'Living Area', 'Kitchen', 'Exterior', 'Additional')");

            builder.HasOne(pi => pi.Property)
                .WithMany(p => p.PropertyImages)
                .HasForeignKey(pi => pi.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pi => pi.PropertyId);
        }
    }

}
