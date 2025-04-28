using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
    {
        public void Configure(EntityTypeBuilder<Violation> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id).ValueGeneratedOnAdd().HasColumnName("id");
            
            builder.Property(v => v.ReportedById).IsRequired().HasColumnName("reported_by_id");
            builder.Property(v => v.ReportedPropertyId).HasColumnName("reported_property_id");
            builder.Property(v => v.ReportedHostId).HasColumnName("reported_host_id");
            builder.Property(v => v.ViolationType).IsRequired().HasMaxLength(50).HasColumnName("violation_type");
            builder.Property(v => v.Description).IsRequired().HasColumnName("description");
            builder.Property(v => v.Status).IsRequired().HasDefaultValue(ViolationStatus.Pending.ToString()).HasMaxLength(20).HasColumnName("status");
            builder.Property(v => v.CreatedAt).HasDefaultValueSql("SYSDATETIME()").HasColumnName("created_at");
            builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            builder.Property(v => v.AdminNotes).HasColumnName("admin_notes");
            builder.Property(v => v.ResolvedAt).HasColumnName("resolved_at");

            // Configure relationships
            builder.HasOne(v => v.ReportedBy)
                .WithMany()
                .HasForeignKey(v => v.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.ReportedProperty)
                .WithMany()
                .HasForeignKey(v => v.ReportedPropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.ReportedHost)
                .WithMany()
                .HasForeignKey(v => v.ReportedHostId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 