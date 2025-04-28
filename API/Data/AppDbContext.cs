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
        //public DbSet<Models.Host> Hosts { get; set; }
        public DbSet<HostPayout> HostPayouts { get; set; }
        public DbSet<Violation> Violations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BookingPayout> BookingPayouts { get; set; }



        public DbSet<Models.Host> HostProfules { get; set; }
        public DbSet<VwPropertyDetails> VwPropertyDetails { get; set; }
        public DbSet<VwHostPerformance> VwHostPerformance { get; set; }
        public DbSet<VwActivePromotions> VwActivePromotions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<VwPropertyDetails>().HasNoKey().ToView("vw_property_details");
            modelBuilder.Entity<VwHostPerformance>().HasNoKey().ToView("vw_host_performance");
            modelBuilder.Entity<VwActivePromotions>().HasNoKey().ToView("vw_active_promotions");

            // Configure Host-HostPayout relationship
            modelBuilder.Entity<Models.Host>()
                .HasMany(h => h.Payouts)
                .WithOne(p => p.Host)
                .HasForeignKey(p => p.HostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
