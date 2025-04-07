using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class CancellationPoliciesConfigurationcs : IEntityTypeConfiguration<CancellationPolicy>
    {
        public void Configure(EntityTypeBuilder<CancellationPolicy> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(c => c.Name).IsRequired().HasMaxLength(50).HasColumnName("name");
            builder.Property(c => c.Description).HasMaxLength(255).HasColumnName("description");
            builder.Property(c => c.RefundPercentage).HasColumnType("decimal(5,2)").HasColumnName("refund_percentage");

            builder.HasCheckConstraint("CK_CancellationPolicies_RefundPercentage", "[refund_percentage] >= 0 AND [refund_percentage] <= 100");
            builder.HasCheckConstraint("CK_CancellationPolicies_RefundPercentage", "[name] IN ('flexible', 'moderate', 'strict', 'non_refundable')");



        }
    }
}
