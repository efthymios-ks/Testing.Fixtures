using Microsoft.EntityFrameworkCore;

namespace Testing.Fixtures.Host.Features.Persistence;

public class QuotesDbContext(DbContextOptions<QuotesDbContext> options) : DbContext(options)
{
    public virtual DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();

    public virtual DbSet<Quote> Quotes => Set<Quote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ShippingRate>(rate =>
        {
            rate.ToTable("ShippingRates");
            rate.HasKey(entity => entity.Id);
            rate.Property(entity => entity.Destination).HasMaxLength(64).IsRequired();
            rate.Property(entity => entity.PricePerKg).HasPrecision(18, 2).IsRequired();
            rate.HasIndex(entity => entity.Destination).IsUnique();

            // Seeded by the migration, so a restored test database already carries them.
            rate.HasData(
                new ShippingRate { Id = 1, Destination = "athens", PricePerKg = 2.50m },
                new ShippingRate { Id = 2, Destination = "berlin", PricePerKg = 4.75m },
                new ShippingRate { Id = 3, Destination = "london", PricePerKg = 5.20m }
            );
        });

        modelBuilder.Entity<Quote>(quote =>
        {
            quote.ToTable("Quotes");
            quote.HasKey(entity => entity.Id);
            quote.Property(entity => entity.Reference).HasMaxLength(32).IsRequired();
            quote.Property(entity => entity.Destination).HasMaxLength(64).IsRequired();
            quote.Property(entity => entity.WeightKg).HasPrecision(18, 3).IsRequired();
            quote.Property(entity => entity.PriceEuros).HasPrecision(18, 2).IsRequired();
            quote.Property(entity => entity.CreatedAt).IsRequired();
            quote.HasIndex(entity => entity.Reference).IsUnique();
        });
    }
}
