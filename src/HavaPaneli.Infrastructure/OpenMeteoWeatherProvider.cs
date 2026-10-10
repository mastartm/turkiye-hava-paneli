using System.Globalization;
using System.Text.Json;
using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// Open-Meteo tahmin API'sinden günlük tahmin okur. Veri CC BY 4.0 lisanslıdır; atıf arayüzde gösterilir.
/// HttpClient'ın BaseAddress'i dışarıdan (DI) verilir, böylece testte sahte sunucuya yönlendirilebilir.
/// </summary>
public sealed class OpenMeteoWeatherProvider : IWeatherProvider
{
    private const int MaxForecastDays = 16;
    private readonly HttpClient _http;

    public OpenMeteoWeatherProvider(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<DailyForecast>> GetForecastAsync(
        City city, int days, CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > MaxForecastDays)
            throw new ArgumentOutOfRangeException(nameof(days), $"Gün sayısı 1 ile {MaxForecastDays} arasında olmalı.");

        // InvariantCulture şart: Türkçe kültürde 39,93 yazılır ve API'yi bozar.
        const string dailyFields =
            "temperature_2m_max,temperature_2m_min,temperature_2m_mean,precipitation_sum,weather_code";
        var url = FormattableString.Invariant(
            $"v1/forecast?latitude={city.Latitude}&longitude={city.Longitude}&daily={dailyFields}&timezone=auto&forecast_days={days}");

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return Parse(doc.RootElement);
    }

    private static IReadOnlyList<DailyForecast> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("daily", out var daily) || !daily.TryGetProperty("time", out var time))
            throw new InvalidOperationException("Open-Meteo yanıtında 'daily.time' alanı yok.");

        var result = new List<DailyForecast>(time.GetArrayLength());
        for (var i = 0; i < time.GetArrayLength(); i++)
        {
            result.Add(new DailyForecast(
                DateOnly.ParseExact(time[i].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ReadDouble(daily, "temperature_2m_min", i),
                ReadDouble(daily, "temperature_2m_max", i),
                ReadDouble(daily, "temperature_2m_mean", i),
                ReadDouble(daily, "precipitation_sum", i),
                (int?)ReadDouble(daily, "weather_code", i)));
        }
        return result;
    }

    // Kaynak bazı günler için null verebilir; null'u 0'a çevirmek yanlış veri üretirdi.
    private static double? ReadDouble(JsonElement daily, string name, int index)
    {
        if (!daily.TryGetProperty(name, out var array) || index >= array.GetArrayLength())
            return null;
        var item = array[index];
        return item.ValueKind == JsonValueKind.Null ? null : item.GetDouble();
    }
}
