using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <summary>Tahminleri saklayan taraf. Aynı şehir+gün tekrar gelirse üzerine yazılır (upsert): tahmin her gün güncellenir.</summary>
public interface IWeatherStore
{
    Task UpsertForecastsAsync(
        City city, IReadOnlyList<DailyForecast> forecasts, DateTimeOffset fetchedAt, CancellationToken cancellationToken = default);
}

/// <summary>Depremleri saklayan taraf. Aynı kimlikli deprem ikinci kez eklenmez, değişmişse güncellenir.</summary>
public interface IEarthquakeStore
{
    /// <returns>İşlenen (eklenen veya güncellenen) kayıt sayısı.</returns>
    Task<int> UpsertAsync(IReadOnlyList<Earthquake> earthquakes, CancellationToken cancellationToken = default);
}
