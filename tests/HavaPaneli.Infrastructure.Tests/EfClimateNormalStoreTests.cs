using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Tests;

[Collection("postgres")]
public class EfClimateNormalStoreTests(PostgresFixture pg)
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);

    private static ClimateNormal Normal(double start) =>
        new(Enumerable.Range(0, 366).Select(i => start + i / 10.0).ToArray());

    [Fact]
    public async Task Kaydedilen_Normal_Aynen_Geri_Okunur()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfClimateNormalStore(factory);

        await store.UpsertAsync(Ankara, Normal(-3));

        var read = await store.GetAsync("Ankara");
        Assert.NotNull(read);
        Assert.Equal(Normal(-3).DailyMeansC, read!.DailyMeansC);
    }

    [Fact]
    public async Task Olmayan_Sehir_Icin_Null_Doner()
    {
        var factory = await pg.CreateDatabaseAsync();

        Assert.Null(await new EfClimateNormalStore(factory).GetAsync("Yok"));
    }

    [Fact]
    public async Task Tekrar_Yazilinca_Uzerine_Yazilir_Cogalmaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfClimateNormalStore(factory);

        await store.UpsertAsync(Ankara, Normal(0));
        await store.UpsertAsync(Ankara, Normal(10));

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.ClimateNormals.CountAsync());
        Assert.Equal(10, (await store.GetAsync("Ankara"))!.NormalFor(new DateOnly(2021, 1, 1)), 6);
    }

    [Fact]
    public async Task Iki_Sehrin_Normali_Birbirine_Karismaz()
    {
        var factory = await pg.CreateDatabaseAsync();
        var store = new EfClimateNormalStore(factory);

        await store.UpsertAsync(Ankara, Normal(0));
        await store.UpsertAsync(new City("Girne", Country.Kktc, 35.34, 33.32), Normal(20));

        Assert.Equal(0, (await store.GetAsync("Ankara"))!.NormalFor(new DateOnly(2021, 1, 1)), 6);
        Assert.Equal(20, (await store.GetAsync("Girne"))!.NormalFor(new DateOnly(2021, 1, 1)), 6);
    }
}
