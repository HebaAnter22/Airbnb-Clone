using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class HostConfiguration : IEntityTypeConfiguration<Models.Host>
    {
        public void Configure(EntityTypeBuilder<Models.Host> builder)
        {
            builder.ToTable("hosts");
            builder.HasKey(h => h.HostId);
            builder.Property(h => h.HostId).ValueGeneratedOnAdd().HasColumnName("host_id");
            builder.Property(h => h.StartDate).HasDefaultValueSql("GETDATE()").HasColumnName("start_date").HasColumnType("datetime");
            builder.Property(h => h.AboutMe).HasMaxLength(500).HasColumnName("about_me");
            builder.Property(h => h.Work).HasMaxLength(100).HasColumnName("work");
            builder.Property(h => h.Rating).HasDefaultValue(0).HasColumnName("rating").HasColumnType("decimal(3,2)");
            builder.Property(h => h.TotalReviews).HasDefaultValue(0).HasColumnName("total_reviews");
            builder.Property(h => h.Education).HasMaxLength(100).HasColumnName("education");
            builder.Property(h => h.Languages).HasMaxLength(100).HasColumnName("languages");
            builder.Property(h => h.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");

            builder.HasOne(h => h.User)
                .WithOne(u => u.Host)
                .HasForeignKey<Models.Host>(h => h.HostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
