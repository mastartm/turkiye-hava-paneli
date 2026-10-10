using System.Globalization;
using System.Text.Json;
using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// Open-Meteo geçmiş veri API'sinden (ERA5-Land yeniden analizi) günlük ortalama sıcaklık okur.
/// Model açıkça seçilir: varsayılan "en iyi eşleşme" zamanla değişebilir, normal ise tekrarlanabilir olmalı.
/// </summary>
public sealed class OpenMeteoClimateProvider : IClimateProvider
{
    private readonly HttpClient _http;

    public OpenMeteoClimateProvider(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<DailyMean>> GetDailyMeansAsync(
        City city, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to)
            throw new ArgumentException("Başlangıç tarihi bitişten sonra olamaz.", nameof(from));

        // InvariantCulture: Türkçe kültürde ondalık ayraç virgüldür ve tarih biçimi de farklı olabilir.
        var start = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = FormattableString.Invariant(
            $"v1/archive?latitude={city.Latitude}&longitude={city.Longitude}&start_date={start}&end_date={end}&daily=temperature_2m_mean&timezone=auto&models=era5_land");

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("daily", out var daily)
            || !daily.TryGetProperty("time", out var time)
            || !daily.TryGetProperty("temperature_2m_mean", out var values))
            throw new InvalidOperationException("Open-Meteo geçmiş yanıtında beklenen alanlar yok.");

        var result = new List<DailyMean>(time.GetArrayLength());
        for (var i = 0; i < time.GetArrayLength(); i++)
        {
            var v = values[i];
            result.Add(new DailyMean(
                DateOnly.ParseExact(time[i].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                v.ValueKind == JsonValueKind.Null ? null : v.GetDouble())); // null'u 0'a çevirmiyoruz
        }
        return result;
    }
}
