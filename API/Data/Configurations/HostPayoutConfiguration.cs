using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Models;

namespace API.Data.Configurations
{
    public class HostPayoutConfiguration : IEntityTypeConfiguration<HostPayout>
    {
        public void Configure(EntityTypeBuilder<HostPayout> builder)
        {
            builder.ToTable("HostPayout");
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseIdentityColumn();

            builder.Property(x => x.HostId)
                .HasColumnName("HostId")
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.PayoutMethod)
                .HasColumnName("PayoutMethod")
                .HasMaxLength(50);

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasMaxLength(50);

            builder.Property(x => x.PayoutAccountDetails)
                .HasColumnName("PayoutAccountDetails")
                .HasMaxLength(500);

            builder.Property(x => x.TransactionId)
                .HasColumnName("TransactionId")
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("ProcessedAt")
                .HasColumnType("datetime")
                .IsRequired(false);

            builder.Property(x => x.Notes)
                .HasColumnName("Notes")
                .HasMaxLength(500);

            builder.HasOne(x => x.Host)
                .WithMany(x => x.Payouts)
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 