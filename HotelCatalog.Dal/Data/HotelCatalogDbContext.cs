using Microsoft.EntityFrameworkCore;
using HotelCatalog.Domain.Entities;
using HotelCatalog.Dal.Configurations;
using System.Linq.Expressions;

namespace HotelCatalog.Dal.Data
{
    public class HotelCatalogDbContext : DbContext
    {
        public HotelCatalogDbContext(DbContextOptions<HotelCatalogDbContext> options)
            : base(options)
        {
        }

        public DbSet<DiscountCategory> DiscountCategories { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<RoomCategoryDetails> RoomCategoryDetails { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<RoomService> RoomServices { get; set; }
        public DbSet<ClientDiscount> ClientDiscounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new DiscountCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new RoomCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new RoomCategoryDetailsConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceConfiguration());
            modelBuilder.ApplyConfiguration(new RoomServiceConfiguration());
            modelBuilder.ApplyConfiguration(new ClientDiscountConfiguration());
            SeedData(modelBuilder);
            ApplyAuditMetadata(modelBuilder);
        }

        private static void ApplyAuditMetadata(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = entityType.HasSharedClrType
                    ? modelBuilder.Entity(entityType.Name)
                    : modelBuilder.Entity(entityType.ClrType);
                entity.Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
                entity.Property<string>("CreatedBy").HasMaxLength(100);
                entity.Property<DateTime?>("UpdatedAt");
                entity.Property<string>("UpdatedBy").HasMaxLength(100);
                entity.Property<bool>("IsDeleted").HasDefaultValue(false).IsRequired();
                entity.Property<int>("Version").HasDefaultValue(1).IsConcurrencyToken();

                if (!entityType.HasSharedClrType)
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "entity");
                    var isDeleted = Expression.Call(
                        typeof(EF),
                        nameof(EF.Property),
                        [typeof(bool)],
                        parameter,
                        Expression.Constant("IsDeleted"));
                    entity.HasQueryFilter(Expression.Lambda(Expression.Equal(isDeleted, Expression.Constant(false)), parameter));
                }
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyAuditChanges();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyAuditChanges();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyAuditChanges()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                    entry.Property("IsDeleted").CurrentValue = false;
                    entry.Property("Version").CurrentValue = 1;
                }
                else
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                    entry.Property("Version").CurrentValue = Convert.ToInt32(entry.Property("Version").CurrentValue) + 1;
                }
            }
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscountCategory>().HasData(
                new DiscountCategory { DiscountId = 1, Name = "Student", DiscountPercent = 10.00m },
                new DiscountCategory { DiscountId = 2, Name = "Senior", DiscountPercent = 15.00m },
                new DiscountCategory { DiscountId = 3, Name = "VIP", DiscountPercent = 20.00m }
            );

            modelBuilder.Entity<RoomCategory>().HasData(
                new RoomCategory { CategoryId = 1, Name = "Standard", Description = "Standard room with basic amenities" },
                new RoomCategory { CategoryId = 2, Name = "Deluxe", Description = "Deluxe room with premium amenities" },
                new RoomCategory { CategoryId = 3, Name = "Suite", Description = "Luxury suite with extra space" }
            );

            modelBuilder.Entity<Service>().HasData(
                new Service { ServiceId = 1, Name = "WiFi", Description = "High-speed internet", Price = 0.00m },
                new Service { ServiceId = 2, Name = "Breakfast", Description = "Continental breakfast", Price = 150.00m },
                new Service { ServiceId = 3, Name = "Parking", Description = "Underground parking", Price = 100.00m },
                new Service { ServiceId = 4, Name = "Spa", Description = "Spa and wellness center", Price = 500.00m },
                new Service { ServiceId = 5, Name = "Pool", Description = "Swimming pool access", Price = 200.00m }
            );

            modelBuilder.Entity<RoomService>().HasData(
                new RoomService { CategoryId = 1, ServiceId = 1, AdditionalPrice = 0m }, // WiFi
                new RoomService { CategoryId = 1, ServiceId = 3, AdditionalPrice = 15m }, // Parking
                new RoomService { CategoryId = 2, ServiceId = 1, AdditionalPrice = 0m }, // WiFi
                new RoomService { CategoryId = 2, ServiceId = 2, AdditionalPrice = 20m }, // Breakfast
                new RoomService { CategoryId = 2, ServiceId = 3, AdditionalPrice = 10m }, // Parking
                new RoomService { CategoryId = 2, ServiceId = 5, AdditionalPrice = 0m }, // Pool
                new RoomService { CategoryId = 3, ServiceId = 1, AdditionalPrice = 0m }, // WiFi
                new RoomService { CategoryId = 3, ServiceId = 2, AdditionalPrice = 15m }, // Breakfast
                new RoomService { CategoryId = 3, ServiceId = 3, AdditionalPrice = 0m }, // Parking
                new RoomService { CategoryId = 3, ServiceId = 4, AdditionalPrice = 50m }, // Spa
                new RoomService { CategoryId = 3, ServiceId = 5, AdditionalPrice = 0m }  // Pool
            );
        }
    }
}
