using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <summary>Saklanmış tahminleri okuyan taraf (yazma tarafı <see cref="IWeatherStore"/>).</summary>
public interface IForecastReader
{
    /// <summary>Şehrin verilen günden itibaren en fazla <paramref name="days"/> günlük tahmini, tarihe göre sıralı.</summary>
    Task<IReadOnlyList<DailyForecast>> GetForCityAsync(string cityName, DateOnly from, int days, CancellationToken cancellationToken = default);

    /// <summary>Verilen gün için her şehrin tahmini (anahtar: şehir adı). Verisi olmayan şehir sözlükte yoktur.</summary>
    Task<IReadOnlyDictionary<string, DailyForecast>> GetForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}

public interface IEarthquakeReader
{
    /// <returns>En yeniden en eskiye sıralı, en fazla <paramref name="limit"/> deprem.</returns>
    Task<IReadOnlyList<Earthquake>> GetRecentAsync(DateTimeOffset since, double minMagnitude, int limit, CancellationToken cancellationToken = default);
}

/// <param name="NormalC">O günün iklim normali; şehrin normali henüz hesaplanmadıysa null.</param>
/// <param name="AnomalyC">Ortalama sıcaklık − normal. Pozitif = normalden sıcak. Normal ya da ortalama yoksa null.</param>
public sealed record DayView(
    DateOnly Date, double? MinC, double? MaxC, double? MeanC, double? PrecipitationMm, int? WeatherCode,
    double? NormalC, double? AnomalyC);

public sealed record CityForecastView(City City, IReadOnlyList<DayView> Days);

public sealed record MapPoint(
    City City, double? MinC, double? MaxC, double? MeanC, int? WeatherCode, double? NormalC, double? AnomalyC);

public sealed record MapSummary(DateOnly Date, IReadOnlyList<MapPoint> Points);

/// <summary>
/// Türkiye saatine göre "bugün". Türkiye 2016'dan beri yıl boyu UTC+3 kullanıyor (yaz saati yok),
/// bu yüzden sabit ofset yeterli ve işletim sistemine göre değişen saat dilimi adlarından bağımsız.
/// Bilinen sınır: KKTC yaz saati uyguladığı için yazın bir saatlik fark olabilir.
/// </summary>
public static class TurkeyTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(3);

    public static DateOnly Today(TimeProvider time) => DateOnly.FromDateTime(time.GetUtcNow().ToOffset(Offset).DateTime);
}

public sealed class ForecastQueryService
{
    public const int ForecastDays = 7;

    private readonly IReadOnlyList<City> _cities;
    private readonly IForecastReader _forecasts;
    private readonly IClimateNormalStore _normals;
    private readonly TimeProvider _time;

    public ForecastQueryService(
        IReadOnlyList<City> cities, IForecastReader forecasts, IClimateNormalStore normals, TimeProvider time)
    {
        _cities = cities;
        _forecasts = forecasts;
        _normals = normals;
        _time = time;
    }

    /// <summary>Şehri kimliğinden (slug) bulur; "Istanbul", "İSTANBUL", "istanbul" hepsi aynı şehre gider.</summary>
    public City? FindCity(string slug)
    {
        var key = City.ToSlug(slug);
        return _cities.FirstOrDefault(c => c.Slug == key);
    }

    /// <returns>Şehir yoksa null. Şehir var ama verisi yoksa günler listesi boş döner.</returns>
    public async Task<CityForecastView?> GetCityForecastAsync(string slug, CancellationToken ct = default)
    {
        var city = FindCity(slug);
        if (city is null) return null;

        var forecasts = await _forecasts.GetForCityAsync(city.Name, TurkeyTime.Today(_time), ForecastDays, ct);
        var normal = await _normals.GetAsync(city.Name, ct);

        var days = forecasts
            .OrderBy(f => f.Date)
            .Select(f => ToDayView(f, normal))
            .ToList();
        return new CityForecastView(city, days);
    }

    /// <summary>Haritadaki tüm şehirlerin bugünkü özeti. Verisi olmayan şehir de listede yer alır (boş değerlerle).</summary>
    public async Task<MapSummary> GetMapAsync(CancellationToken ct = default)
    {
        var today = TurkeyTime.Today(_time);
        var forecasts = await _forecasts.GetForDateAsync(today, ct);
        var normals = await _normals.GetAllAsync(ct);

        var points = _cities.Select(city =>
        {
            forecasts.TryGetValue(city.Name, out var f);
            normals.TryGetValue(city.Name, out var normal);
            var day = f is null ? null : ToDayView(f, normal);
            return new MapPoint(city, day?.MinC, day?.MaxC, day?.MeanC, day?.WeatherCode, day?.NormalC, day?.AnomalyC);
        }).ToList();

        return new MapSummary(today, points);
    }

    private static DayView ToDayView(DailyForecast f, ClimateNormal? normal)
    {
        double? normalC = normal?.NormalFor(f.Date);
        double? anomaly = normal is not null && f.MeanTemperatureC is { } mean ? normal.AnomalyFor(f.Date, mean) : null;
        return new DayView(f.Date, f.MinTemperatureC, f.MaxTemperatureC, f.MeanTemperatureC,
            f.PrecipitationMm, f.WeatherCode, normalC, anomaly);
    }
}

public sealed class EarthquakeQueryService
{
    public const int MaxDays = 30;
    public const int MaxResults = 500;

    private readonly IEarthquakeReader _reader;
    private readonly TimeProvider _time;

    public EarthquakeQueryService(IEarthquakeReader reader, TimeProvider time)
    {
        _reader = reader;
        _time = time;
    }

    /// <exception cref="ArgumentOutOfRangeException">Gün sayısı 1..30 ya da büyüklük 0..10 dışındaysa.</exception>
    public Task<IReadOnlyList<Earthquake>> GetRecentAsync(int days, double minMagnitude, CancellationToken ct = default)
    {
        if (days is < 1 or > MaxDays)
            throw new ArgumentOutOfRangeException(nameof(days), $"Gün sayısı 1 ile {MaxDays} arasında olmalı.");
        if (minMagnitude is < 0 or > 10 || double.IsNaN(minMagnitude))
            throw new ArgumentOutOfRangeException(nameof(minMagnitude), "Büyüklük 0 ile 10 arasında olmalı.");

        return _reader.GetRecentAsync(_time.GetUtcNow().AddDays(-days), minMagnitude, MaxResults, ct);
    }
}
