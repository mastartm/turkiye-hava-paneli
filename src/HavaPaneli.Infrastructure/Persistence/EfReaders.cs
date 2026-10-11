using HavaPaneli.Application;
using HavaPaneli.Domain;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Persistence;

// Okuma sınıfları AsNoTracking kullanır: veri sadece gösterilecek, değiştirilmeyecek.
// Değişiklik izleme (change tracking) gereksiz bellek ve zaman harcar.

public sealed class EfForecastReader(IDbContextFactory<AppDbContext> factory) : IForecastReader
{
    public async Task<IReadOnlyList<DailyForecast>> GetForCityAsync(
        string cityName, DateOnly from, int days, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db.Forecasts.AsNoTracking()
            .Where(f => f.Date >= from && db.Cities.Any(c => c.Id == f.CityId && c.Name == cityName))
            .OrderBy(f => f.Date)
            .Take(days)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<string, DailyForecast>> GetForDateAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db.Forecasts.AsNoTracking()
            .Where(f => f.Date == date)
            .Join(db.Cities, f => f.CityId, c => c.Id, (f, c) => new { c.Name, Forecast = f })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Name, x => ToDomain(x.Forecast));
    }

    public async Task<StoredCurrent?> GetCurrentAsync(string cityName, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var row = await db.CurrentWeather.AsNoTracking()
            .Where(r => db.Cities.Any(c => c.Id == r.CityId && c.Name == cityName))
            .SingleOrDefaultAsync(ct);

        return row is null
            ? null
            : new StoredCurrent(new CurrentObservation(row.ObservedAt, row.TemperatureC, row.WeatherCode, row.PrecipitationMm), row.FetchedAt);
    }

    private static DailyForecast ToDomain(ForecastRow r) =>
        new(r.Date, r.MinTemperatureC, r.MaxTemperatureC, r.MeanTemperatureC, r.PrecipitationMm, r.WeatherCode);
}

public sealed class EfEarthquakeReader(IDbContextFactory<AppDbContext> factory) : IEarthquakeReader
{
    public async Task<IReadOnlyList<Earthquake>> GetRecentAsync(
        DateTimeOffset since, double minMagnitude, int limit, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var sinceUtc = since.ToUniversalTime();
        var rows = await db.Earthquakes.AsNoTracking()
            .Where(e => e.OccurredAt >= sinceUtc && e.Magnitude >= minMagnitude)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => new Earthquake(r.Id, r.OccurredAt, r.Magnitude, r.Place, r.Latitude, r.Longitude, r.DepthKm)).ToList();
    }
}
