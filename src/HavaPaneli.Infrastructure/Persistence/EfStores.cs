using HavaPaneli.Application;
using HavaPaneli.Domain;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Persistence;

/// <summary>
/// Her çağrıda kısa ömürlü bir DbContext açılır (fabrikadan). Uzun süre yaşayan tek bir context,
/// yüzlerce kaydı bellekte izleyip yavaşlar ve bir hatadan sonra bozuk duruma düşebilir.
/// </summary>
public sealed class EfWeatherStore(IDbContextFactory<AppDbContext> factory) : IWeatherStore
{
    public async Task UpsertForecastsAsync(
        City city, IReadOnlyList<DailyForecast> forecasts, DateTimeOffset fetchedAt, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var cityRow = await CityRows.GetOrCreateAsync(db, city, ct);

        var dates = forecasts.Select(f => f.Date).ToList();
        var existing = await db.Forecasts
            .Where(f => f.CityId == cityRow.Id && dates.Contains(f.Date))
            .ToDictionaryAsync(f => f.Date, ct);

        foreach (var f in forecasts)
        {
            if (!existing.TryGetValue(f.Date, out var row))
            {
                row = new ForecastRow { CityId = cityRow.Id, Date = f.Date };
                db.Forecasts.Add(row);
            }
            row.MinTemperatureC = f.MinTemperatureC;
            row.MaxTemperatureC = f.MaxTemperatureC;
            row.MeanTemperatureC = f.MeanTemperatureC;
            row.PrecipitationMm = f.PrecipitationMm;
            row.WeatherCode = f.WeatherCode;
            row.FetchedAt = fetchedAt.ToUniversalTime();
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class EfCurrentWeatherStore(IDbContextFactory<AppDbContext> factory) : ICurrentWeatherStore
{
    public async Task UpsertCurrentAsync(City city, CurrentObservation current, DateTimeOffset fetchedAt, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var cityRow = await CityRows.GetOrCreateAsync(db, city, ct);
        var row = await db.CurrentWeather.SingleOrDefaultAsync(r => r.CityId == cityRow.Id, ct);
        if (row is null)
        {
            row = new CurrentWeatherRow { CityId = cityRow.Id };
            db.CurrentWeather.Add(row);
        }
        row.ObservedAt = current.ObservedAt.ToUniversalTime();
        row.TemperatureC = current.TemperatureC;
        row.WeatherCode = current.WeatherCode;
        row.PrecipitationMm = current.PrecipitationMm;
        row.FetchedAt = fetchedAt.ToUniversalTime();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class EfEarthquakeStore(IDbContextFactory<AppDbContext> factory) : IEarthquakeStore
{
    public async Task<int> UpsertAsync(IReadOnlyList<Earthquake> earthquakes, CancellationToken ct = default)
    {
        if (earthquakes.Count == 0) return 0;

        await using var db = await factory.CreateDbContextAsync(ct);

        // Kaynak aynı depremi birden fazla kez verebilir; kimliğe göre tekilleştir.
        var unique = earthquakes.GroupBy(e => e.Id).Select(g => g.First()).ToList();
        var ids = unique.Select(e => e.Id).ToList();
        var existing = await db.Earthquakes.Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);

        foreach (var q in unique)
        {
            if (!existing.TryGetValue(q.Id, out var row))
            {
                row = new EarthquakeRow { Id = q.Id };
                db.Earthquakes.Add(row);
            }
            // Kaynak sonradan büyüklüğü/yeri düzeltebilir (USGS "reviewed" olunca); bu yüzden güncelliyoruz.
            row.OccurredAt = q.OccurredAt.ToUniversalTime();
            row.Magnitude = q.Magnitude;
            row.Place = q.Place;
            row.Latitude = q.Latitude;
            row.Longitude = q.Longitude;
            row.DepthKm = q.DepthKm;
        }
        await db.SaveChangesAsync(ct);
        return unique.Count;
    }
}
