using HavaPaneli.Application;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;

namespace HavaPaneli.Infrastructure.Tests;

/// <summary>Uçtan uca: veritabanı A → dosya → boş veritabanı B. Gerçek PostgreSQL, gerçek dosya biçimi.</summary>
[Collection("postgres")]
public class ClimateNormalSeedIntegrationTests(PostgresFixture pg)
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);
    private static readonly IReadOnlyList<City> Cities = [Ankara, Girne];

    private static ClimateNormal Normal(double start) =>
        new(Enumerable.Range(0, 366).Select(i => start + i * 0.0731).ToArray());

    [Fact]
    public async Task Veritabani_Dosya_Baska_Veritabani_Degerler_Korunur()
    {
        var dbA = await pg.CreateDatabaseAsync();
        var storeA = new EfClimateNormalStore(dbA);
        await storeA.UpsertAsync(Ankara, Normal(-4));
        await storeA.UpsertAsync(Girne, Normal(9));

        // A → dosya
        var exported = await new ClimateNormalSeedService(Cities, storeA).ExportAsync();
        using var ms = new MemoryStream();
        ClimateNormalFile.Write(ms, exported);
        ms.Position = 0;
        var fromFile = ClimateNormalFile.Read(ms);

        // dosya → B (boş)
        var dbB = await pg.CreateDatabaseAsync();
        var storeB = new EfClimateNormalStore(dbB);
        var result = await new ClimateNormalSeedService(Cities, storeB).ImportAsync(fromFile);

        Assert.Equal(2, result.Imported);
        Assert.False(result.HasProblems);
        var ankaraB = await storeB.GetAsync("Ankara");
        var girneB = await storeB.GetAsync("Girne");
        for (var i = 0; i < 366; i++)
        {
            Assert.Equal(Normal(-4).DailyMeansC[i], ankaraB!.DailyMeansC[i], 2); // dosya 0,01 °C'ye yuvarlar
            Assert.Equal(Normal(9).DailyMeansC[i], girneB!.DailyMeansC[i], 2);
        }
    }

    [Fact]
    public async Task Import_Iki_Kez_Calisinca_Cogalmaz()
    {
        var db = await pg.CreateDatabaseAsync();
        var store = new EfClimateNormalStore(db);
        var data = new Dictionary<string, ClimateNormal> { ["ankara"] = Normal(0), ["girne"] = Normal(5) };
        var svc = new ClimateNormalSeedService(Cities, store);

        await svc.ImportAsync(data);
        await svc.ImportAsync(data);

        Assert.Equal(2, (await store.GetAllAsync()).Count);
    }
}
