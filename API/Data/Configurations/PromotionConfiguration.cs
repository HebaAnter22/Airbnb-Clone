using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(p => p.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
            builder.Property(p => p.DiscountType).IsRequired().HasMaxLength(20).HasColumnName("discount_type");
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").HasColumnName("amount");
            builder.Property(p => p.StartDate).HasColumnType("datetime").HasColumnName("start_date");
            builder.Property(p => p.EndDate).HasColumnType("datetime").HasColumnName("end_date");
            builder.Property(p => p.MaxUses).HasDefaultValue(1).HasColumnName("max_uses");
            builder.Property(p => p.UsedCount).HasDefaultValue(0).HasColumnName("used_count");
            builder.Property(p => p.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");

            builder.HasCheckConstraint("CK_Promotions_DiscountType", "[discount_type] IN ('percentage', 'fixed')");
        }
    }
}
