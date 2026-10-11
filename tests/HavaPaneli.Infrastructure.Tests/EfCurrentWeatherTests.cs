using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Tests;

[Collection("postgres")]
public class EfCurrentWeatherTests(PostgresFixture pg)
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);
    private static readonly DateTimeOffset Fetched = new(2026, 10, 11, 10, 3, 0, TimeSpan.Zero);

    private static CurrentObservation Obs(double temp, int code = 2) =>
        new(new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.FromHours(3)), temp, code, 0.0);

    [Fact]
    public async Task Kaydedilen_Durum_Cekilme_Zamaniyla_Geri_Okunur()
    {
        var factory = await pg.CreateDatabaseAsync();
        await new EfCurrentWeatherStore(factory).UpsertCurrentAsync(Ankara, Obs(28.4), Fetched);

        var stored = await new EfForecastReader(factory).GetCurrentAsync("Ankara");

        Assert.NotNull(stored);
        Assert.Equal(28.4, stored!.Observation.TemperatureC);
        Assert.Equal(2, stored.Observation.WeatherCode);
        Assert.Equal(Fetched, stored.FetchedAt);
        Assert.Equal(TimeSpan.Zero, stored.FetchedAt.Offset);          // UTC saklanır
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero), stored.Observation.ObservedAt);
    }

    [Fact]
    public async Task Tekrar_Yazilinca_Uzerine_Yazilir_Cogalmaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfCurrentWeatherStore(factory);

        await store.UpsertCurrentAsync(Ankara, Obs(20), Fetched);
        await store.UpsertCurrentAsync(Ankara, Obs(28.4), Fetched.AddHours(1));

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.CurrentWeather.CountAsync());
        var stored = await new EfForecastReader(factory).GetCurrentAsync("Ankara");
        Assert.Equal(28.4, stored!.Observation.TemperatureC);
        Assert.Equal(Fetched.AddHours(1), stored.FetchedAt);
    }

    [Fact]
    public async Task Iki_Sehrin_Durumu_Birbirine_Karismaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfCurrentWeatherStore(factory);
        await store.UpsertCurrentAsync(Ankara, Obs(10), Fetched);
        await store.UpsertCurrentAsync(Girne, Obs(30), Fetched);

        var reader = new EfForecastReader(factory);

        Assert.Equal(10, (await reader.GetCurrentAsync("Ankara"))!.Observation.TemperatureC);
        Assert.Equal(30, (await reader.GetCurrentAsync("Girne"))!.Observation.TemperatureC);
    }

    [Fact]
    public async Task Hic_Cekilmemis_Ya_Da_Bilinmeyen_Sehir_Icin_Null()
    {
        var factory = await pg.CreateDatabaseAsync();
        await new EfWeatherStore(factory).UpsertForecastsAsync(
            Ankara, [new DailyForecast(new DateOnly(2026, 10, 11), 1, 2, 1.5, 0, 0)], Fetched);

        var reader = new EfForecastReader(factory);

        Assert.Null(await reader.GetCurrentAsync("Ankara")); // şehir ve tahmin var ama "şu an" hiç çekilmemiş
        Assert.Null(await reader.GetCurrentAsync("Yok"));
    }

    [Fact]
    public async Task Bos_Alanlar_Null_Olarak_Saklanir()
    {
        var factory = await pg.CreateDatabaseAsync();
        var obs = new CurrentObservation(Fetched, null, null, null);
        await new EfCurrentWeatherStore(factory).UpsertCurrentAsync(Ankara, obs, Fetched);

        var stored = await new EfForecastReader(factory).GetCurrentAsync("Ankara");

        Assert.Null(stored!.Observation.TemperatureC);
        Assert.Null(stored.Observation.WeatherCode);
    }
}
