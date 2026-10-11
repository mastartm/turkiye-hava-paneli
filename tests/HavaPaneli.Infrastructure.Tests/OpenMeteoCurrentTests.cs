using System.Net;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class OpenMeteoCurrentTests
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);

    private const string Daily =
        """
        "daily":{"time":["2026-10-11"],"temperature_2m_max":[24],"temperature_2m_min":[8],"temperature_2m_mean":[16],"precipitation_sum":[0.4],"weather_code":[80]}
        """;

    private const string Offset3 = "\"utc_offset_seconds\":10800,";

    private static (OpenMeteoWeatherProvider provider, StubHandler handler) Create(string body)
    {
        var handler = new StubHandler(HttpStatusCode.OK, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/") };
        return (new OpenMeteoWeatherProvider(http), handler);
    }

    private static string Body(string current, string offset = Offset3) => "{" + offset + current + Daily + "}";

    [Fact]
    public async Task Su_Anki_Durum_Okunur_Ve_Yerel_Saat_Mutlak_Ana_Cevrilir()
    {
        var (provider, _) = Create(Body("\"current\":{\"time\":\"2026-10-11T15:00\",\"interval\":900,\"temperature_2m\":28.4,\"weather_code\":2,\"precipitation\":0.0},"));

        var cur = (await provider.GetSnapshotAsync(Ankara, 1)).Current;

        Assert.NotNull(cur);
        Assert.Equal(28.4, cur!.TemperatureC);
        Assert.Equal(2, cur.WeatherCode);
        Assert.Equal(0.0, cur.PrecipitationMm);
        // 15:00 yerel (UTC+3) = 12:00 UTC: ofset uygulanmazsa ölçüm 3 saat kayık görünürdü.
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero), cur.ObservedAt);
    }

    [Fact]
    public async Task Gunluk_Ve_Su_Anki_Durum_Ayri_Degerlerdir()
    {
        // Gerçek denemedeki durum: gün içinde 11:00'de kısa sağanak (günlük kod 80), şu an parçalı bulutlu (kod 2).
        var (provider, _) = Create(Body("\"current\":{\"time\":\"2026-10-11T15:00\",\"temperature_2m\":28.4,\"weather_code\":2,\"precipitation\":0},"));

        var snap = await provider.GetSnapshotAsync(Ankara, 1);

        Assert.Equal(80, snap.Days[0].WeatherCode);
        Assert.Equal(2, snap.Current!.WeatherCode);
        Assert.Equal(16, snap.Days[0].MeanTemperatureC);
        Assert.Equal(28.4, snap.Current.TemperatureC);
    }

    [Fact]
    public async Task Istek_Current_Alanlarini_Ister()
    {
        var (provider, handler) = Create(Body("\"current\":{\"time\":\"2026-10-11T15:00\"},"));

        await provider.GetSnapshotAsync(Ankara, 1);

        Assert.Contains("current=temperature_2m,weather_code,precipitation", handler.LastRequest!.Query);
    }

    [Fact]
    public async Task Current_Yoksa_Null_Ve_Gunluk_Tahmin_Yine_Gelir()
    {
        var (provider, _) = Create(Body(""));

        var snap = await provider.GetSnapshotAsync(Ankara, 1);

        Assert.Null(snap.Current);
        Assert.Single(snap.Days);
    }

    [Fact]
    public async Task Okunamayan_Zaman_Null_Doner_Cokmez()
    {
        var (provider, _) = Create(Body("\"current\":{\"time\":\"dun aksam\",\"temperature_2m\":5},"));

        Assert.Null((await provider.GetSnapshotAsync(Ankara, 1)).Current);
    }

    [Fact]
    public async Task Alan_Null_Ise_Null_Kalir_Sifira_Cevrilmez()
    {
        var (provider, _) = Create(Body("\"current\":{\"time\":\"2026-10-11T15:00\",\"temperature_2m\":null,\"weather_code\":null,\"precipitation\":null},"));

        var cur = (await provider.GetSnapshotAsync(Ankara, 1)).Current!;

        Assert.Null(cur.TemperatureC);
        Assert.Null(cur.WeatherCode);
        Assert.Null(cur.PrecipitationMm);
    }

    [Fact]
    public async Task Ofset_Yoksa_UTC_Varsayilir()
    {
        var (provider, _) = Create(Body("\"current\":{\"time\":\"2026-10-11T15:00\",\"temperature_2m\":1},", offset: ""));

        Assert.Equal(TimeSpan.Zero, (await provider.GetSnapshotAsync(Ankara, 1)).Current!.ObservedAt.Offset);
    }
}
