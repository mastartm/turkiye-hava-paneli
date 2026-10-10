using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Tests;

[Collection("postgres")]
public class EfReadersTests(PostgresFixture pg)
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);
    private static readonly DateTimeOffset T0 = new(2026, 10, 10, 6, 0, 0, TimeSpan.Zero);

    private static DailyForecast Day(int d, double mean) => new(new DateOnly(2026, 10, d), 1, 20, mean, 0, 3);

    // ---- Tahmin okuyucu ----

    [Fact]
    public async Task Sehrin_Tahmini_Verilen_Gunden_Itibaren_Tarihe_Gore_Okunur()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);
        await store.UpsertForecastsAsync(Ankara, [Day(9, 1), Day(10, 2), Day(12, 4), Day(11, 3)], T0);

        var result = await new EfForecastReader(factory).GetForCityAsync("Ankara", new DateOnly(2026, 10, 10), 7);

        Assert.Equal([10, 11, 12], result.Select(f => f.Date.Day)); // 9'u atlar, sıralı
    }

    [Fact]
    public async Task Gun_Sayisi_Siniri_Uygulanir()
    {
        var factory = await pg.CreateDatabaseAsync();
        await new EfWeatherStore(factory).UpsertForecastsAsync(Ankara, [Day(10, 1), Day(11, 2), Day(12, 3)], T0);

        var result = await new EfForecastReader(factory).GetForCityAsync("Ankara", new DateOnly(2026, 10, 10), 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Baska_Sehrin_Verisi_Karismaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);
        await store.UpsertForecastsAsync(Ankara, [Day(10, 1)], T0);
        await store.UpsertForecastsAsync(Girne, [Day(10, 9)], T0);

        var result = await new EfForecastReader(factory).GetForCityAsync("Girne", new DateOnly(2026, 10, 10), 7);

        Assert.Equal(9, Assert.Single(result).MeanTemperatureC);
    }

    [Fact]
    public async Task Bilinmeyen_Sehir_Icin_Bos_Liste()
    {
        var factory = await pg.CreateDatabaseAsync();

        Assert.Empty(await new EfForecastReader(factory).GetForCityAsync("Yok", new DateOnly(2026, 10, 10), 7));
    }

    [Fact]
    public async Task Gunun_Tahmini_Sehir_Adina_Gore_Sozluk_Doner()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);
        await store.UpsertForecastsAsync(Ankara, [Day(10, 1), Day(11, 5)], T0);
        await store.UpsertForecastsAsync(Girne, [Day(10, 9)], T0);

        var result = await new EfForecastReader(factory).GetForDateAsync(new DateOnly(2026, 10, 10));

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["Ankara"].MeanTemperatureC);
        Assert.Equal(9, result["Girne"].MeanTemperatureC);
    }

    // ---- Deprem okuyucu ----

    private static Earthquake Quake(string id, double mag, DateTimeOffset at) => new(id, at, mag, "Yer", 38.5, 27.1, 8.5);

    [Fact]
    public async Task Depremler_Eskiyi_Ve_Kucugu_Eler_Yeniden_Eskiye_Siralar()
    {
        var factory = await pg.CreateDatabaseAsync();
        await new EfEarthquakeStore(factory).UpsertAsync(
        [
            Quake("old", 4.0, T0.AddDays(-10)),
            Quake("small", 1.5, T0.AddHours(-1)),
            Quake("a", 3.0, T0.AddHours(-5)),
            Quake("b", 5.0, T0.AddHours(-2)),
        ]);

        var result = await new EfEarthquakeReader(factory).GetRecentAsync(T0.AddDays(-7), 2.5, 100);

        Assert.Equal(["b", "a"], result.Select(e => e.Id)); // yeniden eskiye; "old" ve "small" elendi
    }

    [Fact]
    public async Task Deprem_Limiti_Uygulanir_En_Yeniler_Kalir()
    {
        var factory = await pg.CreateDatabaseAsync();
        await new EfEarthquakeStore(factory).UpsertAsync(
            Enumerable.Range(0, 5).Select(i => Quake($"q{i}", 3.0, T0.AddHours(-i))).ToList());

        var result = await new EfEarthquakeReader(factory).GetRecentAsync(T0.AddDays(-1), 0, 2);

        Assert.Equal(["q0", "q1"], result.Select(e => e.Id));
    }

    // ---- Normal: hepsini oku ----

    [Fact]
    public async Task Tum_Normaller_Sehir_Adina_Gore_Okunur()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfClimateNormalStore(factory);
        await store.UpsertAsync(Ankara, new ClimateNormal(Enumerable.Repeat(1.0, 366).ToArray()));
        await store.UpsertAsync(Girne, new ClimateNormal(Enumerable.Repeat(20.0, 366).ToArray()));

        var all = await store.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal(1.0, all["Ankara"].NormalFor(new DateOnly(2026, 1, 1)));
        Assert.Equal(20.0, all["Girne"].NormalFor(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task Hic_Normal_Yoksa_Bos_Sozluk()
    {
        var factory = await pg.CreateDatabaseAsync();

        Assert.Empty(await new EfClimateNormalStore(factory).GetAllAsync());
    }
}
