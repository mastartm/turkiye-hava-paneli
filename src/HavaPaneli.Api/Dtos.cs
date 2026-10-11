using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Api;

// API'nin dışarı verdiği şekiller (sözleşme). Domain sınıflarını doğrudan vermiyoruz:
// Domain değişince arayüzün beklediği JSON kazara bozulmasın.

internal static class Round
{
    /// <summary>Sıcaklık gösterimi için bir ondalık yeterli; "16.174888888888887" gibi gürültülü sayıları dışarı vermeyiz. Hesap tam hassasiyetle yapılır, yalnızca çıktı yuvarlanır.</summary>
    public static double? C(double? v) => v is { } x ? Math.Round(x, 1) : null;
}

public sealed record CityDto(string Id, string Name, string Country, double Latitude, double Longitude)
{
    public static CityDto From(City c) => new(c.Slug, c.Name, c.Country.ToString(), c.Latitude, c.Longitude);
}

public sealed record DayDto(
    DateOnly Date, double? MinC, double? MaxC, double? MeanC, double? PrecipitationMm, int? WeatherCode,
    double? NormalC, double? AnomalyC)
{
    public static DayDto From(DayView d) =>
        new(d.Date, Round.C(d.MinC), Round.C(d.MaxC), Round.C(d.MeanC), Round.C(d.PrecipitationMm), d.WeatherCode, Round.C(d.NormalC), Round.C(d.AnomalyC));
}

/// <param name="ObservedAt">Kaynağın ölçüm/model zamanı (Open-Meteo, ~15 dk'da bir).</param>
public sealed record CurrentDto(DateTimeOffset ObservedAt, double? TemperatureC, int? WeatherCode, double? PrecipitationMm)
{
    public static CurrentDto From(CurrentObservation c) =>
        new(c.ObservedAt, Round.C(c.TemperatureC), c.WeatherCode, Round.C(c.PrecipitationMm));
}

/// <param name="Current">Şu anki durum; hiç çekilmediyse null.</param>
/// <param name="UpdatedAt">Verinin bizim tarafımızdan en son çekildiği an: arayüz "veri ne kadar eski" gösterir.</param>
public sealed record CityForecastDto(CityDto City, IReadOnlyList<DayDto> Days, CurrentDto? Current, DateTimeOffset? UpdatedAt);

public sealed record MapPointDto(
    CityDto City, double? MinC, double? MaxC, double? MeanC, int? WeatherCode, double? NormalC, double? AnomalyC);

public sealed record MapDto(DateOnly Date, IReadOnlyList<MapPointDto> Points);

public sealed record EarthquakeDto(
    string Id, DateTimeOffset OccurredAt, double Magnitude, string Place, double Latitude, double Longitude, double? DepthKm)
{
    public static EarthquakeDto From(Earthquake e) =>
        new(e.Id, e.OccurredAt, e.Magnitude, e.Place, e.Latitude, e.Longitude, e.DepthKm);
}

public sealed record SourceDto(string Name, string Url, string License, string Note);
