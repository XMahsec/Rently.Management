using Microsoft.EntityFrameworkCore;
using Rently.Management.Domain.Entities;
using Rently.Management.Domain.Common;
namespace Rently.Management.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {

        // ───────────────────────────────────────────────
        // DbSets لكل الجداول الموجودة في الـ ERD
        // ───────────────────────────────────────────────

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CarImage> CarImages { get; set; }
        public DbSet<CarUnavailableDate> CarUnavailableDates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Otp> Otps { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----------------------------------------------------------------
            // 1. تطبيق كل الـ fluent configurations من مجلد Configurations
            // ----------------------------------------------------------------

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
          
            modelBuilder.ApplyDecimalPrecision();
            modelBuilder.Entity<Car>()
                .HasOne(c => c.Owner)
                .WithMany(u => u.OwnedCars)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Car + Renter
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Car)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Renter)
                .WithMany(u => u.BookingsAsRenter)
                .HasForeignKey(b => b.RenterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Favorite → junction table (User ↔ Car)
            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.CarId });   // composite key

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Car)
                .WithMany(c => c.Favorites)
                .HasForeignKey(f => f.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message → sender & receiver (self-referencing)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // --------------------------------------------------------------------
        // Override SaveChanges لإضافة auditing أو soft-delete أو timestamps
        // (اختياري – شائع جداً في مشاريع حقيقية)
        // --------------------------------------------------------------------
        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is IAuditableEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // نفس المنطق السابق
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is IAuditableEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}