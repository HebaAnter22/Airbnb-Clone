using API.Data.Configurations;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public AppDbContext() { }
        

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Host> HostProfules { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<CancellationPolicy> CancellationPolicies { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Favourite> Favourites { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<BookingPayment> BookingPayments { get; set; }
        public DbSet<HostVerification> HostVerifications { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<UserUsedPromotion> UserUsedPromotions { get; set; }
        public DbSet<PropertyCategory> PropertyCategories { get; set; }
        public DbSet<PropertyAvailability> PropertyAvailabilities { get; set; }

        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            
        }

    }
}
