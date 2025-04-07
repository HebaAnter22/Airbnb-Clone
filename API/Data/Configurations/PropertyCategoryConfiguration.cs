using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class PropertyCategoryConfiguration : IEntityTypeConfiguration<PropertyCategory>
    {
        public void Configure(EntityTypeBuilder<PropertyCategory> builder)
        {
            builder.HasKey(c => c.CategoryId);
            builder.Property(c => c.CategoryId).ValueGeneratedOnAdd().HasColumnName("category_id");
            builder.Property(c => c.Name).IsRequired().HasMaxLength(50).HasColumnName("name");
            builder.Property(c => c.Description).HasMaxLength(255).HasColumnName("description");
            builder.Property(c => c.IconUrl).HasMaxLength(255).HasColumnName("icon_url");
            
        }
    }
}
