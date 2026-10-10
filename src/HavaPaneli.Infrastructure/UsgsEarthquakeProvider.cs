using System.Globalization;
using System.Text.Json;
using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// USGS FDSN deprem servisinden (GeoJSON) deprem okur. Resmi, belgeli bir API'dir.
/// Servis tek sorguda en fazla 20.000 sonuç döndürür; bu bölge ve süre için yeterlidir.
/// </summary>
public sealed class UsgsEarthquakeProvider : IEarthquakeProvider
{
    private const int MaxResults = 20000;
    private readonly HttpClient _http;

    public UsgsEarthquakeProvider(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Earthquake>> GetRecentAsync(
        GeoBounds bounds, DateTimeOffset since, double minMagnitude, CancellationToken cancellationToken = default)
    {
        if (minMagnitude is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(minMagnitude), "Büyüklük 0 ile 10 arasında olmalı.");

        // InvariantCulture: Türkçe kültürde ondalık ayraç virgüldür, API'yi bozar.
        var start = since.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        var url = FormattableString.Invariant(
            $"fdsnws/event/1/query?format=geojson&orderby=time&limit={MaxResults}&minmagnitude={minMagnitude}&minlatitude={bounds.MinLatitude}&maxlatitude={bounds.MaxLatitude}&minlongitude={bounds.MinLongitude}&maxlongitude={bounds.MaxLongitude}&starttime={start}");

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return Parse(doc.RootElement);
    }

    private static IReadOnlyList<Earthquake> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("features", out var features))
            throw new InvalidOperationException("USGS yanıtında 'features' alanı yok.");

        var result = new List<Earthquake>(features.GetArrayLength());
        foreach (var feature in features.EnumerateArray())
        {
            var props = feature.GetProperty("properties");

            // Büyüklüğü olmayan kayıt anlamsız; sıfır sayıp göstermek yerine atlıyoruz.
            if (props.GetProperty("mag").ValueKind == JsonValueKind.Null)
                continue;

            // GeoJSON sırası [boylam, enlem, derinlik]; enlem ile boylamın yer değiştirmesi klasik bir hatadır.
            var coords = feature.GetProperty("geometry").GetProperty("coordinates");
            var depth = coords.GetArrayLength() > 2 && coords[2].ValueKind != JsonValueKind.Null
                ? coords[2].GetDouble()
                : (double?)null;

            result.Add(new Earthquake(
                feature.GetProperty("id").GetString()!,
                DateTimeOffset.FromUnixTimeMilliseconds(props.GetProperty("time").GetInt64()),
                props.GetProperty("mag").GetDouble(),
                props.TryGetProperty("place", out var place) && place.ValueKind == JsonValueKind.String
                    ? place.GetString()!
                    : "Bilinmeyen yer",
                Latitude: coords[1].GetDouble(),
                Longitude: coords[0].GetDouble(),
                DepthKm: depth));
        }
        return result;
    }
}
