using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <summary>
/// Hava durumu katmanının "sözleşmesi". Uygulama bu arayüzü bilir, Open-Meteo'yu bilmez.
/// Kaynağı değiştirmek (ya da testte sahtesini vermek) böylece kolay olur.
/// </summary>
public interface IWeatherProvider
{
    /// <param name="days">1 ile 16 arası; Open-Meteo'nun ücretsiz tahmin sınırı.</param>
    Task<WeatherSnapshot> GetSnapshotAsync(City city, int days, CancellationToken cancellationToken = default);
}
