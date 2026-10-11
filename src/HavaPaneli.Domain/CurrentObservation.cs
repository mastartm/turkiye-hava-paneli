namespace HavaPaneli.Domain;

/// <summary>
/// Bir şehrin ŞU ANKİ durumu (Open-Meteo `current`, ~15 dakikada bir güncellenir). Günlük ortalamadan farklıdır:
/// günlük ortalama gecenin serinliğini de içerir, şu anki sıcaklık içermez.
/// </summary>
public sealed record CurrentObservation(
    DateTimeOffset ObservedAt,
    double? TemperatureC,
    int? WeatherCode,
    double? PrecipitationMm);

/// <summary>Tek istekle gelen tahmin: günlük günler ve (varsa) şu anki durum.</summary>
public sealed record WeatherSnapshot(IReadOnlyList<DailyForecast> Days, CurrentObservation? Current);
