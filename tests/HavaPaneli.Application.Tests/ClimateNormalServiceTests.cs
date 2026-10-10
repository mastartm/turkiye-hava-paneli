using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Application.Tests;

public class ClimateNormalServiceTests
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Izmir = new("İzmir", Country.Turkiye, 38.42, 27.14);

    private static IReadOnlyList<DailyMean> FullHistory(double value)
    {
        var list = new List<DailyMean>();
        for (var d = ClimateNormalService.PeriodStart; d <= ClimateNormalService.PeriodEnd; d = d.AddDays(1))
            list.Add(new DailyMean(d, value));
        return list;
    }

    private sealed class FakeClimate(Func<City, IReadOnlyList<DailyMean>> onCall) : IClimateProvider
    {
        public List<string> Calls { get; } = [];
        public DateOnly? LastFrom { get; private set; }
        public DateOnly? LastTo { get; private set; }

        public Task<IReadOnlyList<DailyMean>> GetDailyMeansAsync(City city, DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            Calls.Add(city.Name);
            LastFrom = from; LastTo = to;
            return Task.FromResult(onCall(city));
        }
    }

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
    }

    private static ClimateNormalService Create(IReadOnlyList<City> cities, FakeClimate c, FakeStore s) =>
        new(cities, c, s, TimeSpan.Zero);

    [Fact]
    public async Task Her_Sehir_Icin_Normal_Hesaplanip_Saklanir()
    {
        var climate = new FakeClimate(_ => FullHistory(10));
        var store = new FakeStore();

        var result = await Create([Ankara, Izmir], climate, store).RunAsync();

        Assert.Equal(2, result.Computed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(2, store.Data.Count);
        Assert.Equal(10, store.Data["Ankara"].NormalFor(new DateOnly(2026, 10, 10)), 6);
    }

    [Fact]
    public async Task Standart_Donem_1991_2020_Istenir()
    {
        var climate = new FakeClimate(_ => FullHistory(10));

        await Create([Ankara], climate, new FakeStore()).RunAsync();

        Assert.Equal(new DateOnly(1991, 1, 1), climate.LastFrom);
        Assert.Equal(new DateOnly(2020, 12, 31), climate.LastTo);
    }

    [Fact]
    public async Task Zaten_Hesaplanmis_Sehir_Atlanir_Ve_Dis_API_Cagrilmaz()
    {
        var climate = new FakeClimate(_ => FullHistory(10));
        var store = new FakeStore();
        await Create([Ankara], climate, store).RunAsync();
        climate.Calls.Clear();

        var result = await Create([Ankara], climate, store).RunAsync();

        Assert.Equal(0, result.Computed);
        Assert.Equal(1, result.Skipped);
        Assert.Empty(climate.Calls);
    }

    [Fact]
    public async Task Force_Ile_Yeniden_Hesaplanir()
    {
        var climate = new FakeClimate(_ => FullHistory(10));
        var store = new FakeStore();
        await Create([Ankara], climate, store).RunAsync();

        var result = await Create([Ankara], new FakeClimate(_ => FullHistory(20)), store).RunAsync(force: true);

        Assert.Equal(1, result.Computed);
        Assert.Equal(20, store.Data["Ankara"].NormalFor(new DateOnly(2026, 10, 10)), 6);
    }

    [Fact]
    public async Task Bir_Sehir_Hata_Verirse_Digerleri_Devam_Eder()
    {
        var climate = new FakeClimate(c => c.Name == "İzmir" ? throw new HttpRequestException("429") : FullHistory(10));
        var store = new FakeStore();

        var result = await Create([Ankara, Izmir], climate, store).RunAsync();

        Assert.Equal(1, result.Computed);
        Assert.Single(result.Failed);
        Assert.Contains("İzmir", result.Failed[0]);
        Assert.False(store.Data.ContainsKey("İzmir"));
    }

    [Fact]
    public async Task Yetersiz_Veri_Hata_Sayilir_Ve_Saklanmaz()
    {
        var climate = new FakeClimate(_ => [new DailyMean(new DateOnly(2000, 1, 1), 5)]);
        var store = new FakeStore();

        var result = await Create([Ankara], climate, store).RunAsync();

        Assert.Equal(0, result.Computed);
        Assert.True(result.HasFailures);
        Assert.Empty(store.Data);
    }
}
