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

    public async Task<WeatherSnapshot> GetSnapshotAsync(
        City city, int days, CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > MaxForecastDays)
            throw new ArgumentOutOfRangeException(nameof(days), $"Gün sayısı 1 ile {MaxForecastDays} arasında olmalı.");

        // InvariantCulture şart: Türkçe kültürde 39,93 yazılır ve API'yi bozar.
        const string dailyFields =
            "temperature_2m_max,temperature_2m_min,temperature_2m_mean,precipitation_sum,weather_code";
        // current: ŞU ANKİ durum (~15 dk'da bir güncellenir). Günlük ortalama ile karışmasın diye ayrı saklanır.
        const string currentFields = "temperature_2m,weather_code,precipitation";
        var url = FormattableString.Invariant(
            $"v1/forecast?latitude={city.Latitude}&longitude={city.Longitude}&current={currentFields}&daily={dailyFields}&timezone=auto&forecast_days={days}");

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return new WeatherSnapshot(Parse(doc.RootElement), ParseCurrent(doc.RootElement));
    }

    /// <summary>Yanıtta `current` yoksa (ya da zamanı okunamazsa) null: günlük tahmin yine de kullanılır.</summary>
    private static CurrentObservation? ParseCurrent(JsonElement root)
    {
        if (!root.TryGetProperty("current", out var cur) || cur.ValueKind != JsonValueKind.Object
            || !cur.TryGetProperty("time", out var time) || time.ValueKind != JsonValueKind.String)
            return null;

        // `time` şehrin yerel saatiyle ofsetsiz gelir ("2026-10-11T15:00"); yanıttaki utc_offset_seconds ile mutlak ana çeviririz.
        var offset = root.TryGetProperty("utc_offset_seconds", out var off) && off.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(off.GetInt32())
            : TimeSpan.Zero;
        if (!DateTime.TryParseExact(time.GetString(), "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return null;

        return new CurrentObservation(
            new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset),
            ReadNumber(cur, "temperature_2m"),
            (int?)ReadNumber(cur, "weather_code"),
            ReadNumber(cur, "precipitation"));
    }

    private static double? ReadNumber(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

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
