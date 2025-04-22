using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class BookingPaymentConfiguration : IEntityTypeConfiguration<BookingPayment>
    {
        public void Configure(EntityTypeBuilder<BookingPayment> builder)
        {
            builder.HasKey(bp => bp.Id);
            builder.Property(bp => bp.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(bp => bp.BookingId).IsRequired().HasColumnName("booking_id");
            builder.Property(bp => bp.Amount).IsRequired().HasColumnType("decimal(18,2)").HasColumnName("amount");
            builder.Property(bp => bp.PaymentMethodType).IsRequired().HasMaxLength(50).HasColumnName("payment_method_type");
            builder.Property(bp => bp.Status).IsRequired().HasMaxLength(20).HasColumnName("status").HasConversion<string>();
            builder.Property(bp => bp.TransactionId).HasMaxLength(255).HasColumnName("transaction_id");
            builder.Property(bp => bp.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("created_at");
            builder.Property(bp => bp.UpdatedAt).HasColumnType("datetime").HasColumnName("updated_at");
            builder.Property(bp => bp.RefundedAmount).HasDefaultValue(0).HasColumnType("decimal(18,2)").HasColumnName("refunded_amount");
            builder.Property(bp => bp.PayementGateWayResponse).HasColumnName("payment_gateway_response").HasColumnType("NVARCHAR");

            // Check Constraints
            builder.HasCheckConstraint("CK_BookingPayments_RefundedAmount", "[refunded_amount] >= 0");
            builder.HasCheckConstraint("CK_BookingPayments_Amount", "[amount] > 0");
            builder.HasCheckConstraint("CK_BookingPayments_RefundedAmount_Amount", "[refunded_amount] <= [amount]");

            // Navigation Property
            builder.HasOne(bp => bp.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(bp => bp.BookingId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
