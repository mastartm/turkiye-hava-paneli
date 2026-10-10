using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <summary>Deprem katmanının sözleşmesi; <see cref="IWeatherProvider"/> ile aynı mantık.</summary>
public interface IEarthquakeProvider
{
    /// <returns>En yeniden en eskiye sıralı depremler.</returns>
    Task<IReadOnlyList<Earthquake>> GetRecentAsync(
        GeoBounds bounds, DateTimeOffset since, double minMagnitude, CancellationToken cancellationToken = default);
}
