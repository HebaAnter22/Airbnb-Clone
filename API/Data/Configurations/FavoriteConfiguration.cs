using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations
{
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favourite>
    {
        public void Configure(EntityTypeBuilder<Favourite> builder)
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedOnAdd().HasColumnName("id");
            builder.Property(f => f.UserId).IsRequired().HasColumnName("user_id");
            builder.Property(f => f.PropertyId).IsRequired().HasColumnName("property_id");
            builder.Property(f => f.FavoritedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime").HasColumnName("favorited_at");
            
            builder.HasOne(f => f.Property)
                .WithMany(p => p.Favourites)
                .HasForeignKey(f => f.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.User)
                .WithMany(u => u.Favourites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
