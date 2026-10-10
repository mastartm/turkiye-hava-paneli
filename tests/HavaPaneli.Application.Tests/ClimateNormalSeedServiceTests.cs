using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Application.Tests;

public class ClimateNormalSeedServiceTests
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Istanbul = new("İstanbul", Country.Turkiye, 41.01, 28.98);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);

    private static ClimateNormal Constant(double v) => new(Enumerable.Repeat(v, 366).ToArray());

    private sealed class FakeStore : IClimateNormalStore
    {
        public Dictionary<string, ClimateNormal> Data { get; } = [];

        public Task UpsertAsync(City city, ClimateNormal normal, CancellationToken ct = default)
        {
            Data[city.Name] = normal;
            return Task.CompletedTask;
        }

        public Task<ClimateNormal?> GetAsync(string cityName, CancellationToken ct = default) =>
            Task.FromResult(Data.GetValueOrDefault(cityName));

        public Task<IReadOnlyDictionary<string, ClimateNormal>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, ClimateNormal>>(Data);
    }

    // ---- Dışa aktarma ----

    [Fact]
    public async Task Disa_Aktarma_Slug_Anahtarli_Verir()
    {
        var store = new FakeStore();
        store.Data["İstanbul"] = Constant(10);

        var result = await new ClimateNormalSeedService([Ankara, Istanbul], store).ExportAsync();

        Assert.Equal(["istanbul"], result.Keys);   // "İstanbul" değil, ASCII kimlik
        Assert.Equal(10, result["istanbul"].NormalFor(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task Disa_Aktarma_Normali_Olmayan_Sehri_Atlar()
    {
        var store = new FakeStore();
        store.Data["Ankara"] = Constant(1);

        var result = await new ClimateNormalSeedService([Ankara, Istanbul, Girne], store).ExportAsync();

        Assert.Equal(["ankara"], result.Keys);
    }

    [Fact]
    public async Task Disa_Aktarma_Katalogda_Olmayan_Sehri_Almaz()
    {
        var store = new FakeStore();
        store.Data["Atlantis"] = Constant(1);

        Assert.Empty(await new ClimateNormalSeedService([Ankara], store).ExportAsync());
    }

    // ---- İçe aktarma ----

    [Fact]
    public async Task Ice_Aktarma_Tum_Sehirleri_Yukler()
    {
        var store = new FakeStore();
        var data = new Dictionary<string, ClimateNormal> { ["ankara"] = Constant(1), ["istanbul"] = Constant(2) };

        var result = await new ClimateNormalSeedService([Ankara, Istanbul], store).ImportAsync(data);

        Assert.Equal(2, result.Imported);
        Assert.False(result.HasProblems);
        Assert.Equal(2, store.Data["İstanbul"].NormalFor(new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public async Task Dosyada_Olmayan_Sehir_Hata_Sayilir_Digerleri_Yuklenir()
    {
        var store = new FakeStore();
        var data = new Dictionary<string, ClimateNormal> { ["ankara"] = Constant(1) };

        var result = await new ClimateNormalSeedService([Ankara, Girne], store).ImportAsync(data);

        Assert.Equal(1, result.Imported);
        Assert.Equal(["Girne"], result.MissingCities);
        Assert.True(result.HasProblems);
        Assert.True(store.Data.ContainsKey("Ankara"));
        Assert.False(store.Data.ContainsKey("Girne"));
    }

    [Fact]
    public async Task Katalogda_Olmayan_Kimlik_Yok_Sayilir_Ve_Raporlanir()
    {
        var store = new FakeStore();
        var data = new Dictionary<string, ClimateNormal> { ["ankara"] = Constant(1), ["atlantis"] = Constant(9) };

        var result = await new ClimateNormalSeedService([Ankara], store).ImportAsync(data);

        Assert.Equal(["atlantis"], result.UnknownIds);
        Assert.False(result.HasProblems);          // bilinmeyen kimlik hata değil, uyarı
        Assert.Single(store.Data);
    }

    [Fact]
    public async Task Ice_Aktarma_Mevcut_Normalin_Uzerine_Yazar_Tekrar_Calistirmak_Zararsiz()
    {
        var store = new FakeStore();
        var svc = new ClimateNormalSeedService([Ankara], store);
        await svc.ImportAsync(new Dictionary<string, ClimateNormal> { ["ankara"] = Constant(1) });

        await svc.ImportAsync(new Dictionary<string, ClimateNormal> { ["ankara"] = Constant(5) });

        Assert.Single(store.Data);
        Assert.Equal(5, store.Data["Ankara"].NormalFor(new DateOnly(2026, 1, 1)));
    }
}
