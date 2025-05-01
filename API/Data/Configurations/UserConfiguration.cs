using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255).HasColumnName("email");
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255).HasColumnName("password_hash");
            builder.Property(u => u.FirstName).HasMaxLength(50).HasColumnName("first_name");
            builder.Property(u => u.LastName).HasMaxLength(50).HasColumnName("last_name");
            builder.Property(u => u.DateOfBirth).HasColumnType("date").HasColumnName("date_of_birth");
            builder.Property(u => u.ProfilePictureUrl).HasMaxLength(255).HasColumnName("profile_picture_url");
            builder.Property(u => u.PhoneNumber).HasMaxLength(20).HasColumnName("phone_number");
            builder.Property(u => u.AccountStatus).HasDefaultValue(Account_Status.Pending.ToString()).HasMaxLength(20).HasConversion<string>().HasColumnName("account_status");
            builder.Property(u => u.EmailVerified).HasDefaultValue(false).HasColumnName("email_verified");
            builder.Property(u => u.PhoneVerified).HasDefaultValue(false).HasColumnName("phone_verified");
            builder.Property(u => u.LastLogin).HasColumnType("datetime").HasColumnName("last_login");
            builder.Property(u => u.Role).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(UserRole.Guest.ToString()).HasColumnName("role");
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("SYSDATETIME()").HasColumnName("created_at").HasColumnType("datetime");
            builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime");
            builder.Property(u => u.PasswordResetToken)
                .HasMaxLength(255)
                .IsRequired(false);
            builder.HasCheckConstraint("CK_Users_AccountStatus", "[account_status] IN ('active', 'pending', 'blocked')");
            builder.HasCheckConstraint("CK_Users_Role", "[role] IN ('Guest', 'Host', 'Admin')");



        }
    }
}
