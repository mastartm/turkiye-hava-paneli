using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Tests;

[Collection("postgres")]
public class EfStoresTests(PostgresFixture pg)
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly DateTimeOffset T0 = new(2026, 10, 10, 6, 0, 0, TimeSpan.Zero);

    private static DailyForecast Day(int d, double max, double? min = 1) =>
        new(new DateOnly(2026, 10, d), min, max, 10, 0.5, 3);

    [Fact]
    public async Task Tahmin_Ilk_Kez_Eklenir_Ve_Sehir_Olusur()
    {
        var factory = await pg.CreateDatabaseAsync();

        await new EfWeatherStore(factory).UpsertForecastsAsync(Ankara, [Day(10, 20), Day(11, 21)], T0);

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.Cities.CountAsync());
        Assert.Equal(2, await db.Forecasts.CountAsync());
        var city = await db.Cities.SingleAsync();
        Assert.Equal("Turkiye", city.Country);
    }

    [Fact]
    public async Task Ayni_Gun_Tekrar_Gelince_Uzerine_Yazilir_Cogalmaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);

        await store.UpsertForecastsAsync(Ankara, [Day(10, 20), Day(11, 21)], T0);
        await store.UpsertForecastsAsync(Ankara, [Day(10, 25), Day(12, 18)], T0.AddHours(6));

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.Cities.CountAsync());
        Assert.Equal(3, await db.Forecasts.CountAsync()); // 10, 11, 12
        var day10 = await db.Forecasts.SingleAsync(f => f.Date == new DateOnly(2026, 10, 10));
        Assert.Equal(25, day10.MaxTemperatureC);
        Assert.Equal(T0.AddHours(6), day10.FetchedAt);
    }

    [Fact]
    public async Task Null_Deger_Null_Olarak_Saklanir()
    {
        var factory = await pg.CreateDatabaseAsync();

        await new EfWeatherStore(factory).UpsertForecastsAsync(Ankara, [Day(10, 20, min: null)], T0);

        await using var db = factory.CreateDbContext();
        Assert.Null((await db.Forecasts.SingleAsync()).MinTemperatureC);
    }

    [Fact]
    public async Task Sehir_Koordinati_Degisince_Guncellenir()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);

        await store.UpsertForecastsAsync(Ankara, [Day(10, 20)], T0);
        await store.UpsertForecastsAsync(new City("Ankara", Country.Turkiye, 40.0, 33.0), [Day(10, 20)], T0);

        await using var db = factory.CreateDbContext();
        var city = await db.Cities.SingleAsync();
        Assert.Equal(40.0, city.Latitude);
    }

    [Fact]
    public async Task Iki_Farkli_Sehir_Ayri_Kayit_Olur()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfWeatherStore(factory);

        await store.UpsertForecastsAsync(Ankara, [Day(10, 20)], T0);
        await store.UpsertForecastsAsync(new City("Girne", Country.Kktc, 35.34, 33.32), [Day(10, 28)], T0);

        await using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.Cities.CountAsync());
        Assert.Equal(2, await db.Forecasts.CountAsync());
    }

    private static Earthquake Quake(string id, double mag) =>
        new(id, new DateTimeOffset(2026, 10, 9, 3, 0, 0, TimeSpan.Zero), mag, "Yer", 38.5, 27.1, 8.5);

    [Fact]
    public async Task Deprem_Ilk_Kez_Eklenir()
    {
        var factory = await pg.CreateDatabaseAsync();

        var count = await new EfEarthquakeStore(factory).UpsertAsync([Quake("a", 3.1), Quake("b", 4.0)]);

        Assert.Equal(2, count);
        await using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.Earthquakes.CountAsync());
    }

    [Fact]
    public async Task Ayni_Deprem_Tekrar_Gelince_Cogalmaz_Ve_Guncellenir()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfEarthquakeStore(factory);

        await store.UpsertAsync([Quake("a", 3.1)]);
        await store.UpsertAsync([Quake("a", 3.4), Quake("b", 2.8)]);

        await using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.Earthquakes.CountAsync());
        Assert.Equal(3.4, (await db.Earthquakes.SingleAsync(e => e.Id == "a")).Magnitude);
    }

    [Fact]
    public async Task Kaynak_Ayni_Depremi_Iki_Kez_Verirse_Tek_Kayit_Olur()
    {
        var factory = await pg.CreateDatabaseAsync();

        var count = await new EfEarthquakeStore(factory).UpsertAsync([Quake("a", 3.1), Quake("a", 3.1)]);

        Assert.Equal(1, count);
        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.Earthquakes.CountAsync());
    }

    [Fact]
    public async Task Bos_Liste_Sifir_Doner_Ve_Hata_Vermez()
    {
        var factory = await pg.CreateDatabaseAsync();

        Assert.Equal(0, await new EfEarthquakeStore(factory).UpsertAsync([]));
    }

    [Fact]
    public async Task Zaman_UTC_Olarak_Saklanip_Aynen_Okunur()
    {
        var factory = await pg.CreateDatabaseAsync();
        var local = new DateTimeOffset(2026, 10, 9, 6, 0, 0, TimeSpan.FromHours(3)); // 03:00 UTC

        await new EfEarthquakeStore(factory).UpsertAsync([Quake("a", 3.0) with { OccurredAt = local }]);

        await using var db = factory.CreateDbContext();
        var row = await db.Earthquakes.SingleAsync();
        Assert.Equal(local.ToUniversalTime(), row.OccurredAt);
        Assert.Equal(TimeSpan.Zero, row.OccurredAt.Offset);
    }
}
