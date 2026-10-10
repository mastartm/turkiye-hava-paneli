using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Persistence;

// Tablo sınıfları Domain'den ayrı: Domain saf kalır, EF'nin anahtar/ilişki ihtiyaçları buraya aittir.
public sealed class CityRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class ForecastRow
{
    public int CityId { get; set; }
    public DateOnly Date { get; set; }
    public double? MinTemperatureC { get; set; }
    public double? MaxTemperatureC { get; set; }
    public double? MeanTemperatureC { get; set; }
    public double? PrecipitationMm { get; set; }
    public int? WeatherCode { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}

public sealed class EarthquakeRow
{
    public string Id { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public double Magnitude { get; set; }
    public string Place { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DepthKm { get; set; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CityRow> Cities => Set<CityRow>();
    public DbSet<ForecastRow> Forecasts => Set<ForecastRow>();
    public DbSet<EarthquakeRow> Earthquakes => Set<EarthquakeRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CityRow>(e =>
        {
            e.ToTable("cities");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Country).HasMaxLength(20).IsRequired();
            // Ad benzersiz: toplayıcı şehri adından bulur, aynı şehir iki kez eklenmez.
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<ForecastRow>(e =>
        {
            e.ToTable("daily_forecasts");
            // Bileşik anahtar: bir şehrin bir günü için tek tahmin. Yenisi gelince üzerine yazılır.
            e.HasKey(x => new { x.CityId, x.Date });
            e.HasOne<CityRow>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EarthquakeRow>(e =>
        {
            e.ToTable("earthquakes");
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Place).HasMaxLength(300).IsRequired();
            // "Son depremler" sorgusu zamana göre sıralar.
            e.HasIndex(x => x.OccurredAt);
        });
    }
}
